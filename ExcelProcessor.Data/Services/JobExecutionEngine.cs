using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Core.Interfaces;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;
using ExcelProcessor.Data.Repositories;
using System.IO; // Added for File.Exists

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// 作业执行引擎
    /// </summary>
    public class JobExecutionEngine : IJobExecutionEngine
    {
        private readonly IExcelService _excelService;
        private readonly ISqlService _sqlService;
        private readonly IDataImportService _dataImportService;
        private readonly IExcelConfigService _excelConfigService;
        private readonly IJobExecutionRepository _jobExecutionRepository;
        private readonly IJobStepRepository _jobStepRepository; // Added job step repository
        private readonly ILogger<JobExecutionEngine> _logger;
        private readonly ConcurrentDictionary<string, JobExecutionContext> _executionContexts = new();
        private readonly ConcurrentDictionary<string, ExecutionProgress> _executionProgress = new();
        private readonly Dictionary<string, Action<ExecutionEvent>> _executionEventCallbacks = new();
        private readonly object _eventLock = new object();

        public JobExecutionEngine(
            IExcelService excelService,
            ISqlService sqlService,
            IDataImportService dataImportService,
            IExcelConfigService excelConfigService,
            IJobExecutionRepository jobExecutionRepository,
            IJobStepRepository jobStepRepository, // Added job step repository
            ILogger<JobExecutionEngine> logger)
        {
            _excelService = excelService;
            _sqlService = sqlService;
            _dataImportService = dataImportService;
            _excelConfigService = excelConfigService;
            _jobExecutionRepository = jobExecutionRepository;
            _jobStepRepository = jobStepRepository; // Initialize job step repository
            _logger = logger;
        }

        #region 作业执行

        public async Task<JobExecutionResult> ExecuteJobAsync(JobConfig jobConfig, Dictionary<string, object>? parameters = null, string executedBy = "", CancellationToken cancellationToken = default)
        {
            var executionId = Guid.NewGuid().ToString();
            var result = new JobExecutionResult { ExecutionId = executionId };
            var jobExecution = new JobExecution
            {
                Id = executionId,
                JobId = jobConfig.Id,
                JobName = jobConfig.Name,
                Status = JobStatus.Running,
                StartTime = DateTime.Now,
                ExecutedBy = executedBy,
                Parameters = parameters ?? new Dictionary<string, object>(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            try
            {
                _logger.LogInformation("开始执行作业: {JobName} ({JobId}), 执行ID: {ExecutionId}", jobConfig.Name, jobConfig.Id, executionId);
                
                // 保存执行记录
                await _jobExecutionRepository.CreateAsync(jobExecution);
                
                // 创建执行上下文
                var context = new JobExecutionContext
                {
                    JobConfig = jobConfig,
                    ExecutionId = executionId,
                    Parameters = parameters ?? new Dictionary<string, object>(),
                    ExecutedBy = executedBy,
                    StartTime = DateTime.Now,
                    CancellationToken = cancellationToken
                };
                _executionContexts[executionId] = context;
                
                // 检查作业步骤配置
                await ValidateJobStepsConfiguration(jobConfig, context);
                
                context.AddLog($"作业开始执行: {jobConfig.Name}");
                context.AddLog($"执行参数: {(parameters != null ? JsonSerializer.Serialize(parameters) : "无")}");
                
                // 获取作业步骤 - 从数据库获取最新的步骤配置
                var steps = await _jobStepRepository.GetByJobIdAsync(jobConfig.Id);
                if (steps == null || !steps.Any())
                {
                    throw new InvalidOperationException("作业没有配置任何步骤");
                }

                context.AddLog($"找到 {steps.Count} 个作业步骤");
                
                // 按顺序执行步骤
                foreach (var step in steps.OrderBy(s => s.OrderIndex))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        context.AddLog("作业执行被取消");
                        break;
                    }

                    if (!step.IsEnabled)
                    {
                        context.AddLog($"跳过禁用的步骤: {step.Name}");
                        continue;
                    }

                    context.AddLog($"开始执行步骤 {step.OrderIndex}: {step.Name}");
                    context.AddLog($"步骤类型: {step.Type}");
                    context.AddLog($"步骤配置: ExcelConfigId={step.ExcelConfigId}, SqlConfigId={step.SqlConfigId}");
                    
                    var stepResult = await ExecuteStepAsync(step, context, cancellationToken);
                    
                    if (!stepResult.IsSuccess)
                    {
                        context.AddLog($"步骤执行失败: {step.Name}, 错误: {stepResult.ErrorMessage}");
                        
                        if (!step.ContinueOnFailure)
                        {
                            context.AddLog("步骤配置为失败时停止，终止作业执行");
                            break;
                        }
                    }
                    else
                    {
                        context.AddLog($"步骤执行成功: {step.Name}");
                    }
                }

                // 作业执行完成
                result.IsSuccess = true;
                result.ExecutionId = executionId;
                result.StartTime = jobExecution.StartTime;
                result.EndTime = DateTime.Now;
                result.DurationSeconds = (result.EndTime.Value - result.StartTime).TotalSeconds;
                result.ExecutionLogs = context.Logs.ToList();
                
                jobExecution.Status = JobStatus.Completed;
                jobExecution.EndTime = result.EndTime;
                jobExecution.Duration = result.EndTime.Value - jobExecution.StartTime;
                jobExecution.UpdatedAt = DateTime.Now;
                
                context.AddLog($"作业执行完成: {jobConfig.Name}");
                _logger.LogInformation("作业执行成功: {JobName} ({JobId}), 执行ID: {ExecutionId}", jobConfig.Name, jobConfig.Id, executionId);
                
                TriggerExecutionEvent(executionId, ExecutionEventType.JobCompleted, "作业执行完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "作业执行失败: {JobName} ({JobId}), 执行ID: {ExecutionId}", jobConfig.Name, jobConfig.Id, executionId);
                
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.ErrorDetails = ex.ToString();
                result.StartTime = jobExecution.StartTime;
                result.EndTime = DateTime.Now;
                result.DurationSeconds = (result.EndTime.Value - result.StartTime).TotalSeconds;
                
                // 更新执行记录状态为失败
                jobExecution.Status = JobStatus.Failed;
                jobExecution.EndTime = result.EndTime;
                jobExecution.Duration = result.EndTime.Value - jobExecution.StartTime;
                jobExecution.ErrorMessage = ex.Message;
                jobExecution.ErrorDetails = ex.ToString();
                jobExecution.UpdatedAt = DateTime.Now;
                await _jobExecutionRepository.UpdateAsync(jobExecution);
                
                TriggerExecutionEvent(executionId, ExecutionEventType.JobFailed, $"作业执行异常: {ex.Message}");
            }
            finally
            {
                // 清理执行上下文
                _executionContexts.TryRemove(executionId, out _);
                _executionProgress.TryRemove(executionId, out _);
            }

            return result;
        }

        public async Task<StepExecutionResult> ExecuteStepAsync(JobStep step, JobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var result = new StepExecutionResult
            {
                StepId = step.Id,
                StepName = step.Name,
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInformation("执行步骤: {StepName} ({StepId})", step.Name, step.Id);
                context.AddLog($"开始执行步骤: {step.Name}");
                context.AddLog($"步骤类型: {step.Type}, 步骤顺序: {step.OrderIndex}");

                // 根据步骤类型查找对应的配置并执行
                switch (step.Type)
                {
                    case StepType.ExcelImport:
                        // 通过ExcelConfigId查找Excel配置，然后执行导入
                        await ExecuteStepWithExcelConfig(step, context, result);
                        break;
                    case StepType.SqlExecution:
                        // 通过SqlConfigId查找SQL配置，然后执行SQL
                        await ExecuteStepWithSqlConfig(step, context, result);
                        break;
                    case StepType.DataExport:
                        await ExecuteDataExportStep(step, context, result);
                        break;
                    case StepType.Wait:
                        await ExecuteWaitStep(step, context, result);
                        break;
                    case StepType.Condition:
                        await ExecuteConditionStep(step, context, result);
                        break;
                    default:
                        throw new NotSupportedException($"不支持的步骤类型: {step.Type}");
                }

                result.IsSuccess = true;
                context.AddLog($"步骤执行成功: {step.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "步骤执行失败: {StepName} ({StepId})", step.Name, step.Id);
                context.AddLog($"步骤执行失败: {step.Name} - {ex.Message}");
                
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.ErrorDetails = ex.ToString();
            }
            finally
            {
                result.EndTime = DateTime.Now;
                result.DurationSeconds = (result.EndTime.Value - result.StartTime).TotalSeconds;
                result.ExecutionLogs = context.Logs.ToList();
            }

            return result;
        }

        public async Task<ExcelProcessor.Core.Services.ValidationResult> ValidateJobConfigAsync(JobConfig jobConfig)
        {
            var result = new ExcelProcessor.Core.Services.ValidationResult { IsValid = true };

            try
            {
                if (string.IsNullOrWhiteSpace(jobConfig.Name))
                {
                    result.IsValid = false;
                    result.Errors.Add("作业名称不能为空");
                }

                if (jobConfig.Steps == null || jobConfig.Steps.Count == 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("作业必须包含至少一个步骤");
                }

                if (jobConfig.Steps != null)
                {
                    foreach (var step in jobConfig.Steps)
                    {
                        var stepValidation = await ValidateStepConfigAsync(step);
                        if (!stepValidation.IsValid)
                        {
                            result.IsValid = false;
                            result.Errors.AddRange(stepValidation.Errors.Select(e => $"步骤 '{step.Name}': {e}"));
                        }
                        if (stepValidation.Warnings.Any())
                        {
                            result.Warnings.AddRange(stepValidation.Warnings.Select(w => $"步骤 '{step.Name}': {w}"));
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证作业配置时发生错误: {JobName}", jobConfig.Name);
                result.IsValid = false;
                result.Errors.Add($"验证作业配置时发生错误: {ex.Message}");
                return result;
            }
        }

        public async Task<ExcelProcessor.Core.Services.ValidationResult> ValidateStepConfigAsync(JobStep step)
        {
            var result = new ExcelProcessor.Core.Services.ValidationResult { IsValid = true };

            try
            {
                if (string.IsNullOrWhiteSpace(step.Name))
                {
                    result.IsValid = false;
                    result.Errors.Add("步骤名称不能为空");
                }

                if (step.OrderIndex < 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("步骤顺序不能为负数");
                }

                if (step.TimeoutSeconds <= 0)
                {
                    result.Warnings.Add("步骤超时时间应该大于0");
                }

                // 验证步骤配置ID
                switch (step.Type)
                {
                    case StepType.ExcelImport:
                        if (string.IsNullOrWhiteSpace(step.ExcelConfigId))
                {
                            result.IsValid = false;
                            result.Errors.Add("Excel导入步骤必须指定Excel配置ID");
                        }
                        break;
                    case StepType.SqlExecution:
                        if (string.IsNullOrWhiteSpace(step.SqlConfigId))
                        {
                            result.IsValid = false;
                            result.Errors.Add("SQL执行步骤必须指定SQL配置ID");
                    }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "验证步骤配置时发生错误: {StepName}", step.Name);
                result.Warnings.Add($"验证步骤配置时发生错误: {ex.Message}");
            }

                return result;
        }

        #endregion

        #region 执行上下文

        /// <summary>
        /// 创建执行上下文
        /// </summary>
        public JobExecutionContext CreateExecutionContext(JobConfig jobConfig, Dictionary<string, object>? parameters = null, string executedBy = "")
        {
            var context = new JobExecutionContext
            {
                JobConfig = jobConfig,
                ExecutionId = Guid.NewGuid().ToString(),
                Parameters = parameters ?? new Dictionary<string, object>(),
                ExecutedBy = executedBy,
                StartTime = DateTime.Now
            };
            
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    context.SetVariable(param.Key, param.Value);
                }
            }
            
            return context;
        }

        /// <summary>
        /// 获取执行上下文
        /// </summary>
        public JobExecutionContext? GetExecutionContext(string executionId)
        {
            _executionContexts.TryGetValue(executionId, out var context);
            return context;
        }

        /// <summary>
        /// 清理执行上下文
        /// </summary>
        public void CleanupExecutionContext(string executionId)
        {
            _executionContexts.TryRemove(executionId, out _);
            _executionProgress.TryRemove(executionId, out _);
        }

        #endregion

        #region 执行监控

        /// <summary>
        /// 获取执行进度
        /// </summary>
        public async Task<ExecutionProgress> GetExecutionProgressAsync(string executionId)
        {
            if (_executionProgress.TryGetValue(executionId, out var progress))
            {
                return await Task.FromResult(progress);
            }
            return await Task.FromResult(new ExecutionProgress());
        }

        /// <summary>
        /// 更新执行进度
        /// </summary>
        public async Task UpdateExecutionProgressAsync(string executionId, ExecutionProgress progress)
        {
            _executionProgress[executionId] = progress;
            await Task.CompletedTask;
        }

        /// <summary>
        /// 订阅执行事件
        /// </summary>
        public void SubscribeToExecutionEvents(Action<ExecutionEvent> callback)
        {
            lock (_eventLock)
            {
                var key = callback.GetHashCode().ToString();
                _executionEventCallbacks[key] = callback;
            }
        }

        /// <summary>
        /// 取消订阅执行事件
        /// </summary>
        public void UnsubscribeFromExecutionEvents(Action<ExecutionEvent> callback)
        {
            lock (_eventLock)
            {
                var key = callback.GetHashCode().ToString();
                _executionEventCallbacks.Remove(key);
            }
        }

        #endregion

        #region 错误处理

        public async Task<ErrorHandlingResult> HandleExecutionErrorAsync(JobExecutionContext context, Exception error, JobStep? step = null)
        {
            var result = new ErrorHandlingResult
            {
                IsHandled = true,
                ShouldContinue = false,
                Message = $"处理执行错误: {error.Message}"
            };

            try
            {
                context.AddLog($"处理执行错误: {error.Message}");

                if (step != null)
                {
                    if (step.ContinueOnFailure)
                    {
                        result.ShouldContinue = true;
                        result.Message = $"步骤 '{step.Name}' 配置为失败时继续执行";
                        context.AddLog(result.Message);
                    }
                    else
                    {
                        result.ShouldContinue = false;
                        result.Message = $"步骤 '{step.Name}' 执行失败，停止后续步骤";
                        context.AddLog(result.Message);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理执行错误时发生异常");
                result.IsHandled = false;
                result.ShouldContinue = false;
                result.Message = $"处理执行错误时发生异常: {ex.Message}";
                return result;
            }
        }

        public async Task<StepExecutionResult> RetryStepExecutionAsync(JobStep step, JobExecutionContext context, int retryCount, CancellationToken cancellationToken = default)
        {
            try
            {
                context.AddLog($"重试步骤执行: {step.Name}, 重试次数: {retryCount}");

                if (retryCount >= step.RetryCount)
                {
                    var result = new StepExecutionResult
                    {
                        StepId = step.Id,
                        StepName = step.Name,
                        IsSuccess = false,
                        ErrorMessage = $"重试次数已达上限: {step.RetryCount}",
                        RetryCount = retryCount
                    };
                    context.AddLog($"重试次数已达上限，步骤执行失败: {step.Name}");
                    return result;
                }

                // 等待重试间隔
                if (step.RetryIntervalSeconds > 0)
                {
                    context.AddLog($"等待重试间隔: {step.RetryIntervalSeconds} 秒");
                    await Task.Delay(step.RetryIntervalSeconds * 1000, cancellationToken);
                }

                // 重新执行步骤
            return await ExecuteStepAsync(step, context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重试步骤执行失败: {StepName}", step.Name);
                var result = new StepExecutionResult
                {
                    StepId = step.Id,
                    StepName = step.Name,
                    IsSuccess = false,
                    ErrorMessage = $"重试步骤执行失败: {ex.Message}",
                    RetryCount = retryCount
                };
                return result;
            }
        }

        #endregion

        #region 私有方法

        private void UpdateExecutionProgress(string executionId, ExecutionProgress progress)
        {
            _executionProgress[executionId] = progress;
            TriggerExecutionEvent(executionId, ExecutionEventType.ProgressUpdated, $"进度更新: {progress.Progress}%");
        }

        private void TriggerExecutionEvent(string executionId, ExecutionEventType eventType, string message)
        {
            var executionEvent = new ExecutionEvent
            {
                ExecutionId = executionId,
                EventType = eventType,
                EventTime = DateTime.Now,
                Message = message
            };

            lock (_eventLock)
            {
                foreach (var callback in _executionEventCallbacks.Values)
                {
                    try
                    {
                        callback(executionEvent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "执行事件回调失败");
                    }
                }
            }
        }

        /// <summary>
        /// 验证作业步骤配置
        /// </summary>
        private async Task ValidateJobStepsConfiguration(JobConfig jobConfig, JobExecutionContext context)
        {
            try
            {
                context.AddLog("开始验证作业步骤配置...");
                
                var steps = await _jobStepRepository.GetByJobIdAsync(jobConfig.Id);
                if (steps == null || !steps.Any())
                {
                    context.AddLog("警告: 作业没有配置任何步骤");
                    return;
                }

                context.AddLog($"验证 {steps.Count} 个作业步骤的配置...");
                
                foreach (var step in steps.OrderBy(s => s.OrderIndex))
                {
                    context.AddLog($"验证步骤 {step.OrderIndex}: {step.Name} (类型: {step.Type})");
                    context.AddLog($"步骤详细信息: ID={step.Id}, ExcelConfigId={step.ExcelConfigId}, SqlConfigId={step.SqlConfigId}");
                    
                    switch (step.Type)
                    {
                        case StepType.ExcelImport:
                            if (string.IsNullOrWhiteSpace(step.ExcelConfigId))
                            {
                                context.AddLog($"警告: Excel导入步骤 '{step.Name}' 的ExcelConfigId为空");
                            }
                            else
                            {
                                context.AddLog($"Excel导入步骤 '{step.Name}' 的ExcelConfigId: {step.ExcelConfigId}");
                                // 尝试获取Excel配置进行验证
                                var excelConfig = await GetExcelConfigById(step.ExcelConfigId, context);
                                if (excelConfig != null)
                                {
                                    context.AddLog($"✓ 找到Excel配置: {excelConfig.ConfigName}");
                                }
                                else
                                {
                                    context.AddLog($"✗ 未找到Excel配置: {step.ExcelConfigId}");
                                }
                            }
                            break;
                            
                        case StepType.SqlExecution:
                            if (string.IsNullOrWhiteSpace(step.SqlConfigId))
                            {
                                context.AddLog($"警告: SQL执行步骤 '{step.Name}' 的SqlConfigId为空");
                            }
                            else
                            {
                                context.AddLog($"SQL执行步骤 '{step.Name}' 的SqlConfigId: {step.SqlConfigId}");
                                // 尝试获取SQL配置进行验证
                                var sqlConfig = await GetSqlConfigById(step.SqlConfigId, context);
                                if (sqlConfig != null)
                                {
                                    context.AddLog($"✓ 找到SQL配置: {sqlConfig.Name}");
                                }
                                else
                                {
                                    context.AddLog($"✗ 未找到SQL配置: {step.SqlConfigId}");
                                }
                            }
                            break;
                            
                        default:
                            context.AddLog($"步骤类型 {step.Type} 暂不支持验证");
                            break;
                    }
                }
                
                context.AddLog("作业步骤配置验证完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证作业步骤配置时发生错误");
                context.AddLog($"验证作业步骤配置时发生错误: {ex.Message}");
            }
        }

        #endregion

        #region 步骤执行方法

        /// <summary>
        /// 通过Excel配置执行步骤
        /// </summary>
        private async Task ExecuteStepWithExcelConfig(JobStep step, JobExecutionContext context, StepExecutionResult result)
        {
            try
            {
                context.AddLog($"执行Excel导入步骤: {step.Name}");
                context.AddLog($"步骤配置信息: StepId={step.Id}, StepName={step.Name}, ExcelConfigId={step.ExcelConfigId}");

                // 检查ExcelConfigId
                if (string.IsNullOrWhiteSpace(step.ExcelConfigId))
                {
                    var errorMessage = $"Excel配置ID不能为空。请检查：\n" +
                                     $"1. 步骤配置是否正确\n" +
                                     $"2. Excel配置是否已创建\n" +
                                     $"3. 数据库是否已正确初始化";
                    context.AddLog(errorMessage);
                    throw new ArgumentException(errorMessage);
                }

                // 根据ExcelConfigId查找Excel配置
                var excelConfig = await GetExcelConfigById(step.ExcelConfigId, context);
                if (excelConfig == null)
                {
                    var errorMessage = $"无法找到Excel配置: {step.ExcelConfigId}";
                    context.AddLog(errorMessage);
                    throw new ArgumentException(errorMessage);
                }

                context.AddLog($"找到Excel配置: {excelConfig.ConfigName}");
                context.AddLog($"Excel文件路径: {excelConfig.FilePath}");
                context.AddLog($"目标表名: {excelConfig.TargetTableName}");

                // 验证Excel文件是否存在
                if (!File.Exists(excelConfig.FilePath))
                {
                    var errorMessage = $"Excel文件不存在: {excelConfig.FilePath}";
                    context.AddLog(errorMessage);
                    throw new FileNotFoundException(errorMessage);
                }

                // 验证目标表名
                if (string.IsNullOrWhiteSpace(excelConfig.TargetTableName))
                {
                    var errorMessage = "目标表名不能为空";
                    context.AddLog(errorMessage);
                    throw new ArgumentException(errorMessage);
                }

                // 获取字段映射
                var fieldMappings = await _excelService.GetFieldMappingsAsync(excelConfig.Id);
                if (fieldMappings == null || !fieldMappings.Any())
                {
                    var errorMessage = $"Excel配置 {excelConfig.ConfigName} 没有配置字段映射";
                    context.AddLog(errorMessage);
                    throw new ArgumentException(errorMessage);
                }

                // 将ExcelFieldMapping转换为FieldMapping
                var convertedFieldMappings = fieldMappings.Select(fm => new FieldMapping
                {
                    ExcelColumn = fm.ExcelColumnName,
                    ExcelOriginalColumn = GetColumnLetter(fm.ExcelColumnIndex),
                    DatabaseField = fm.TargetFieldName,
                    DataType = fm.TargetFieldType,
                    IsRequired = fm.IsRequired
                }).ToList();

                context.AddLog($"字段映射数量: {convertedFieldMappings.Count}");
                foreach (var mapping in convertedFieldMappings)
                {
                    context.AddLog($"字段映射: {mapping.ExcelColumn} -> {mapping.DatabaseField} ({mapping.DataType})");
                }

                // 调用现有的DataImportService执行导入
                context.AddLog("开始调用DataImportService执行Excel导入...");
                
                var importResult = await _dataImportService.ImportExcelDataAsync(
                    excelConfig,
                    convertedFieldMappings,
                    excelConfig.TargetTableName,
                    new JobProgressCallback(context)
                );

                if (importResult.IsSuccess)
                {
                    result.IsSuccess = true;
                    result.OutputData["ImportedRows"] = importResult.SuccessRows;
                    result.OutputData["TotalRows"] = importResult.TotalRows;
                    result.OutputData["TableName"] = excelConfig.TargetTableName;
                    result.OutputData["ExcelConfigName"] = excelConfig.ConfigName;
                    
                    context.SetVariable("ImportResult", result.OutputData);
                    context.AddLog($"Excel导入成功: 导入 {importResult.SuccessRows} 行数据到表 {excelConfig.TargetTableName}");
                    
                    _logger.LogInformation("Excel导入步骤执行成功: {StepName}, 导入行数: {ImportedRows}", 
                        step.Name, importResult.SuccessRows);
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = importResult.ErrorMessage ?? "Excel导入失败";
                    result.ErrorDetails = importResult.ErrorMessage;
                    
                    context.AddLog($"Excel导入失败: {result.ErrorMessage}");
                    _logger.LogError("Excel导入步骤执行失败: {StepName}, 错误: {Error}", 
                        step.Name, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行Excel导入步骤时发生错误: {StepName}", step.Name);
                context.AddLog($"执行Excel导入步骤时发生错误: {ex.Message}");
                
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.ErrorDetails = ex.ToString();
            }
        }

        /// <summary>
        /// 通过SQL配置执行步骤
        /// </summary>
        private async Task ExecuteStepWithSqlConfig(JobStep step, JobExecutionContext context, StepExecutionResult result)
        {
            try
            {
                context.AddLog($"执行SQL执行步骤: {step.Name}");
                context.AddLog($"步骤配置信息: StepId={step.Id}, StepName={step.Name}, SqlConfigId={step.SqlConfigId}");
                
                // 检查SqlConfigId
                if (string.IsNullOrWhiteSpace(step.SqlConfigId))
                {
                    var errorMessage = $"SQL配置ID不能为空。请检查：\n" +
                                     $"1. 步骤配置是否正确\n" +
                                     $"2. SQL配置是否已创建\n" +
                                     $"3. 数据库是否已正确初始化";
                    context.AddLog(errorMessage);
                    throw new ArgumentException(errorMessage);
                }

                // 根据SqlConfigId查找SQL配置
                var sqlConfig = await GetSqlConfigById(step.SqlConfigId, context);
                if (sqlConfig == null)
                {
                    var errorMessage = $"未找到ID为 {step.SqlConfigId} 的SQL配置。请检查：\n" +
                                     $"1. SQL配置是否已创建\n" +
                                     $"2. SQL配置ID是否正确\n" +
                                     $"3. 数据库是否已正确初始化";
                    context.AddLog(errorMessage);
                    throw new ArgumentException(errorMessage);
                }

                _logger.LogInformation("找到SQL配置: {ConfigName}", sqlConfig.Name);
                context.AddLog($"使用SQL配置: {sqlConfig.Name}");
                context.AddLog($"SQL语句: {sqlConfig.SqlStatement}");
                context.AddLog($"数据源ID: {sqlConfig.DataSourceId}");

                // 验证SQL语句
                if (string.IsNullOrWhiteSpace(sqlConfig.SqlStatement))
                {
                    var errorMessage = "SQL语句不能为空";
                    context.AddLog(errorMessage);
                    throw new ArgumentException(errorMessage);
                }

                // 验证数据源ID
                if (string.IsNullOrWhiteSpace(sqlConfig.DataSourceId))
                {
                    var errorMessage = "数据源ID不能为空";
                    context.AddLog(errorMessage);
                    throw new ArgumentException(errorMessage);
                }
                
                // 使用找到的配置执行SQL
                await ExecuteSqlWithConfig(sqlConfig, step, context, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行SQL执行步骤时发生错误: {StepName}", step.Name);
                context.AddLog($"执行SQL执行步骤时发生错误: {ex.Message}");
                
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.ErrorDetails = ex.ToString();
            }
        }



        /// <summary>
        /// 根据ExcelConfigId查找Excel配置
        /// </summary>
        private async Task<ExcelConfig?> GetExcelConfigById(string excelConfigId, JobExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(excelConfigId))
            {
                context.AddLog("Excel配置ID为空");
                return null;
            }

            try
            {
                context.AddLog($"开始查找Excel配置，ID: {excelConfigId}");
                
                // 首先尝试直接使用原始ID查找
                var excelConfig = await _excelService.GetExcelConfigAsync(excelConfigId);
                if (excelConfig != null)
                {
                    context.AddLog($"根据ID {excelConfigId} 找到Excel配置: {excelConfig.ConfigName}");
                    return excelConfig;
                }
                context.AddLog($"根据ID {excelConfigId} 未找到Excel配置");

                // 尝试作为配置名称查找
                try
                {
                    var config = await _excelConfigService.GetConfigByNameAsync(excelConfigId);
                    if (config != null)
                    {
                        context.AddLog($"根据名称 {excelConfigId} 找到Excel配置: {config.ConfigName}");
                        return config;
                    }
                    context.AddLog($"根据名称 {excelConfigId} 未找到Excel配置");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "根据名称查找Excel配置失败: {ConfigId}", excelConfigId);
                    context.AddLog($"根据名称查找Excel配置失败: {ex.Message}");
                }

                // 尝试从所有配置中查找匹配的
                try
                {
                    var allConfigs = await _excelConfigService.GetAllConfigsAsync();
                    context.AddLog($"数据库中总共有 {allConfigs.Count} 个Excel配置");
                    
                    var config = allConfigs.FirstOrDefault(c => 
                        c.Id.ToString().Equals(excelConfigId, StringComparison.OrdinalIgnoreCase) ||
                        c.ConfigName.Equals(excelConfigId, StringComparison.OrdinalIgnoreCase));
                    
                    if (config != null)
                    {
                        context.AddLog($"从所有配置中找到匹配的Excel配置: {config.ConfigName} (ID: {config.Id})");
                        return config;
                    }
                    
                    // 列出所有可用的配置，帮助调试
                    var availableConfigs = string.Join(", ", allConfigs.Select(c => $"{c.ConfigName}({c.Id})"));
                    context.AddLog($"可用的Excel配置: {availableConfigs}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "从所有配置中查找Excel配置失败: {ConfigId}", excelConfigId);
                    context.AddLog($"从所有配置中查找Excel配置失败: {ex.Message}");
                }

                context.AddLog($"未找到Excel配置: {excelConfigId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "查找Excel配置失败: {ConfigId}", excelConfigId);
                context.AddLog($"查找Excel配置失败: {ex.Message}");
                return null;
            }
        }



        /// <summary>
        /// 使用Excel配置执行导入
        /// </summary>
        private async Task ExecuteExcelImportWithConfig(ExcelConfig excelConfig, JobStep step, JobExecutionContext context, StepExecutionResult result)
        {
            try
            {
                context.AddLog($"验证Excel文件: {excelConfig.FilePath}");
                context.AddLog($"目标工作表: {excelConfig.SheetName}");
                context.AddLog($"目标表: {excelConfig.TargetTableName}");

                // 获取字段映射
                List<FieldMapping> fieldMappings = new List<FieldMapping>();
                if (!string.IsNullOrEmpty(excelConfig.Id))
                {
                    try
                    {
                        // 从字段映射服务获取字段映射
                        var fieldMappingsResult = await _excelService.GetFieldMappingsAsync(excelConfig.Id);
                        if (fieldMappingsResult != null)
                        {
                            // 转换为FieldMapping类型
                            fieldMappings = fieldMappingsResult.Select(fm => new FieldMapping
                            {
                                ExcelColumn = fm.ExcelColumnName,
                                ExcelOriginalColumn = GetColumnLetter(fm.ExcelColumnIndex),
                                DatabaseField = fm.TargetFieldName,
                                DataType = fm.TargetFieldType,
                                IsRequired = fm.IsRequired
                            }).ToList();
                            _logger.LogInformation("获取到字段映射: {Count} 个", fieldMappings.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "获取字段映射失败，将使用默认字段映射");
                    }
                }

                // 执行实际的Excel导入
                context.AddLog("开始执行Excel数据导入...");
                _logger.LogInformation("开始执行Excel导入: {FilePath} -> {TargetTable}", excelConfig.FilePath, excelConfig.TargetTableName);

                var importResult = await _dataImportService.ImportExcelDataAsync(excelConfig, fieldMappings, excelConfig.TargetTableName, 
                    new JobExecutionProgressCallback(context));

                if (importResult.IsSuccess)
                {
                    result.IsSuccess = true;
                    result.OutputData["ImportedRows"] = importResult.SuccessRows;
                    result.OutputData["FailedRows"] = importResult.FailedRows;
                    result.OutputData["SkippedRows"] = importResult.SkippedRows;
                    result.OutputData["TargetTable"] = excelConfig.TargetTableName;
                    result.OutputData["FilePath"] = excelConfig.FilePath;
                    result.OutputData["ProcessingTime"] = importResult.Duration.TotalSeconds;

                    context.AddLog($"Excel导入完成: 成功 {importResult.SuccessRows} 行，失败 {importResult.FailedRows} 行，跳过 {importResult.SkippedRows} 行");
                    context.AddLog($"数据已导入到表: {excelConfig.TargetTableName}");
                    
                    // 设置上下文变量供后续步骤使用
                    context.SetVariable("ImportedData", result.OutputData);
                    context.SetVariable("LastImportResult", importResult);
                    
                    _logger.LogInformation("Excel导入步骤执行成功: {StepName}, 导入行数: {ImportedRows}", 
                        step.Name, importResult.SuccessRows);
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = importResult.ErrorMessage ?? "Excel导入失败";
                    result.ErrorDetails = importResult.ErrorMessage;
                    
                    context.AddLog($"Excel导入失败: {result.ErrorMessage}");
                    _logger.LogError("Excel导入步骤执行失败: {StepName}, 错误: {Error}", 
                        step.Name, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行Excel导入时发生错误: {StepName}", step.Name);
                context.AddLog($"执行Excel导入时发生错误: {ex.Message}");
                
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.ErrorDetails = ex.ToString();
            }
        }



        /// <summary>
        /// 根据SqlConfigId查找SQL配置
        /// </summary>
        private async Task<SqlConfig?> GetSqlConfigById(string sqlConfigId, JobExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(sqlConfigId))
            {
                context.AddLog("SQL配置ID为空");
                return null;
            }

            try
            {
                context.AddLog($"开始查找SQL配置，ID: {sqlConfigId}");
                
                // 首先直接使用原始ID查找
                var sqlConfig = await _sqlService.GetSqlConfigByIdAsync(sqlConfigId);
                if (sqlConfig != null)
                {
                    context.AddLog($"根据ID {sqlConfigId} 找到SQL配置: {sqlConfig.Name}");
                    return sqlConfig;
                }

                context.AddLog($"根据ID {sqlConfigId} 未找到SQL配置，尝试根据名称查找");

                // 如果直接查找失败，尝试作为配置名称查找
                try
                {
                    var sqlConfigs = await _sqlService.GetAllSqlConfigsAsync();
                    context.AddLog($"数据库中总共有 {sqlConfigs.Count} 个SQL配置");
                    
                    var config = sqlConfigs.FirstOrDefault(c => c.Name.Equals(sqlConfigId, StringComparison.OrdinalIgnoreCase));
                    if (config != null)
                    {
                        context.AddLog($"根据名称 {sqlConfigId} 找到SQL配置: {config.Name}");
                        return config;
                    }
                    
                    // 尝试从所有配置中查找匹配的
                    config = sqlConfigs.FirstOrDefault(c => 
                        c.Id.ToString().Equals(sqlConfigId, StringComparison.OrdinalIgnoreCase) ||
                        c.Name.Equals(sqlConfigId, StringComparison.OrdinalIgnoreCase));
                    
                    if (config != null)
                    {
                        context.AddLog($"从所有配置中找到匹配的SQL配置: {config.Name} (ID: {config.Id})");
                        return config;
                    }
                    
                    // 列出所有可用的配置名称，帮助调试
                    var availableConfigs = string.Join(", ", sqlConfigs.Select(c => $"{c.Name}({c.Id})"));
                    context.AddLog($"可用的SQL配置: {availableConfigs}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "根据名称查找SQL配置失败: {ConfigId}", sqlConfigId);
                    context.AddLog($"根据名称查找SQL配置失败: {ex.Message}");
                }

                context.AddLog($"未找到SQL配置: {sqlConfigId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "查找SQL配置失败: {ConfigId}", sqlConfigId);
                context.AddLog($"查找SQL配置失败: {ex.Message}");
                return null;
            }
        }



        /// <summary>
        /// 使用SQL配置执行SQL
        /// </summary>
        private async Task ExecuteSqlWithConfig(SqlConfig sqlConfig, JobStep step, JobExecutionContext context, StepExecutionResult result)
        {
            try
            {
                context.AddLog($"执行SQL配置: {sqlConfig.Name}");
                context.AddLog($"SQL语句: {sqlConfig.SqlStatement}");
                context.AddLog($"数据源ID: {sqlConfig.DataSourceId}");

                // 获取数据源配置
                var dataSource = await _sqlService.GetDataSourceByIdAsync(sqlConfig.DataSourceId);
                if (dataSource == null)
                {
                    var errorMessage = $"未找到数据源: {sqlConfig.DataSourceId}";
                    context.AddLog(errorMessage);
                    throw new ArgumentException(errorMessage);
                }

                context.AddLog($"连接到数据源: {dataSource.Name} ({dataSource.ConnectionString})");

                // 执行SQL语句
                context.AddLog("开始执行SQL语句...");
                var sqlResult = await _sqlService.ExecuteSqlAsync(sqlConfig.SqlStatement, dataSource.ConnectionString);
                
                if (sqlResult.Status == "成功")
                {
                    result.IsSuccess = true;
                    result.OutputData["AffectedRows"] = sqlResult.AffectedRows;
                    result.OutputData["OperationType"] = "SQL_EXECUTION";
                    result.OutputData["SqlConfigName"] = sqlConfig.Name;
                    result.OutputData["DataSourceId"] = sqlConfig.DataSourceId;
                    result.OutputData["ExecutionTime"] = sqlResult.Duration / 1000.0; // 转换为秒
                    
                    context.SetVariable("DatabaseResult", result.OutputData);
                    context.AddLog($"SQL执行完成: 影响行数 {sqlResult.AffectedRows}, 执行时间 {sqlResult.Duration / 1000.0:F2}秒");
                    
                    _logger.LogInformation("SQL执行步骤执行成功: {StepName}, 影响行数: {AffectedRows}", 
                        step.Name, sqlResult.AffectedRows);
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = sqlResult.ErrorMessage ?? "SQL执行失败";
                    result.ErrorDetails = sqlResult.ErrorMessage;
                    
                    context.AddLog($"SQL执行失败: {result.ErrorMessage}");
                    _logger.LogError("SQL执行步骤执行失败: {StepName}, 错误: {Error}", 
                        step.Name, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行SQL时发生错误: {StepName}", step.Name);
                context.AddLog($"执行SQL时发生错误: {ex.Message}");
                
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.ErrorDetails = ex.ToString();
            }
        }

        private async Task ExecuteDataExportStep(JobStep step, JobExecutionContext context, StepExecutionResult result)
        {
            // 模拟数据导出步骤执行
            await Task.Delay(1000); // 模拟处理时间
            
            try
            {
                context.AddLog($"执行数据导出步骤: {step.Name}");

                // 模拟导出结果
                result.OutputData["ExportedRows"] = 85;
                result.OutputData["ExportFormat"] = "Excel";
                result.OutputData["ExportPath"] = "C:\\temp\\export.xlsx";
                
                context.SetVariable("ExportResult", result.OutputData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行数据导出步骤时发生错误: {StepName}", step.Name);
                context.AddLog($"执行步骤时发生错误: {ex.Message}");
                result.OutputData["Error"] = ex.Message;
                result.OutputData["ExportedRows"] = 0;
            }
        }

        private async Task ExecuteWaitStep(JobStep step, JobExecutionContext context, StepExecutionResult result)
        {
            try
            {
                var waitSeconds = 60; // 默认等待60秒
                
                context.AddLog($"执行等待步骤: {step.Name}, 等待时间: {waitSeconds} 秒");

                // 执行等待
                await Task.Delay(waitSeconds * 1000);

                result.OutputData["WaitSeconds"] = waitSeconds;
                result.OutputData["WaitCompleted"] = true;
                
                context.SetVariable("WaitResult", result.OutputData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行等待步骤时发生错误: {StepName}", step.Name);
                context.AddLog($"执行步骤时发生错误: {ex.Message}");
                result.OutputData["Error"] = ex.Message;
            }
        }

        private async Task ExecuteConditionStep(JobStep step, JobExecutionContext context, StepExecutionResult result)
        {
            try
            {
                var conditionExpression = step.ConditionExpression ?? "";
                
                context.AddLog($"执行条件步骤: {step.Name}, 条件表达式: {conditionExpression}");

                // 执行条件判断
                // TODO: 实现条件表达式解析和执行逻辑
                var conditionResult = true; // 临时实现

                result.OutputData["ConditionExpression"] = conditionExpression;
                result.OutputData["ConditionResult"] = conditionResult;
                
                context.SetVariable("ConditionResult", result.OutputData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行条件步骤时发生错误: {StepName}", step.Name);
                context.AddLog($"执行步骤时发生错误: {ex.Message}");
                result.OutputData["Error"] = ex.Message;
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 将StepType转换为JobStepType
        /// </summary>
        /// <param name="stepType">原始步骤类型</param>
        /// <returns>转换后的步骤类型</returns>
        private JobStepType ConvertStepType(StepType stepType)
        {
            return stepType switch
            {
                StepType.ExcelImport => JobStepType.ExcelImport,
                StepType.SqlExecution => JobStepType.SqlExecution,
                StepType.FileOperation => JobStepType.FileOperation,
                StepType.DataValidation => JobStepType.DataValidation,
                StepType.EmailSend => JobStepType.EmailSend,
                StepType.Condition => JobStepType.ConditionCheck,
                StepType.Loop => JobStepType.LoopProcess,
                StepType.Wait => JobStepType.Wait,
                StepType.CustomScript => JobStepType.CustomScript,
                StepType.DataExport => JobStepType.DataTransformation,
                _ => JobStepType.CustomScript // 默认值
            };
        }

        /// <summary>
        /// 清理JSON字符串
        /// </summary>
        /// <param name="jsonString">原始JSON字符串</param>
        /// <returns>清理后的JSON字符串</returns>
        private string CleanJsonString(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return jsonString;

            // 移除可能的BOM标记
            if (jsonString.StartsWith("\uFEFF"))
                jsonString = jsonString.Substring(1);

            // 移除可能的UTF-8编码问题
            jsonString = jsonString.Replace("\u0000", "");

            // 尝试修复常见的编码问题
            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
                jsonString = System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                // 如果编码转换失败，保持原样
            }

            return jsonString;
        }

        /// <summary>
        /// 将列索引转换为Excel列字母（A, B, C, ..., Z, AA, AB, ...）
        /// </summary>
        /// <param name="columnIndex">列索引（从0开始）</param>
        /// <returns>Excel列字母</returns>
        private string GetColumnLetter(int columnIndex)
        {
            if (columnIndex < 0) return "A";
            
            var result = "";
            while (columnIndex >= 0)
            {
                result = (char)('A' + (columnIndex % 26)) + result;
                columnIndex = (columnIndex / 26) - 1;
            }
            return result;
        }

        #endregion
    }

        /// <summary>
    /// 作业执行进度回调实现
        /// </summary>
    public class JobExecutionProgressCallback : IImportProgressCallback
    {
        private readonly JobExecutionContext _context;

        public JobExecutionProgressCallback(JobExecutionContext context)
        {
            _context = context;
        }

        public void SetStatus(string status)
        {
            _context.AddLog($"导入状态: {status}");
        }

        public void UpdateProgress(double progress, string message)
        {
            _context.AddLog($"导入进度: {progress:F1}% - {message}");
        }

        public void UpdateStatistics(int totalRows, int processedRows, int successRows, int failedRows)
        {
            _context.AddLog($"导入统计: 总计 {totalRows} 行，已处理 {processedRows} 行，成功 {successRows} 行，失败 {failedRows} 行");
        }

        public void UpdateCurrentRow(int currentRow, int totalRows)
        {
            _context.AddLog($"当前处理: 第 {currentRow} 行，共 {totalRows} 行");
        }

        public void UpdateBatchInfo(int currentBatch, int batchSize, int totalBatches)
        {
            _context.AddLog($"批次信息: 第 {currentBatch} 批，批次大小 {batchSize}，共 {totalBatches} 批");
        }
    }

        /// <summary>
        /// 作业进度回调实现
        /// </summary>
        public class JobProgressCallback : IImportProgressCallback
        {
            private readonly JobExecutionContext _context;

            public JobProgressCallback(JobExecutionContext context)
            {
                _context = context;
            }

            public void ReportProgress(int currentRow, int totalRows, string message)
            {
                var percentage = totalRows > 0 ? (currentRow * 100.0 / totalRows) : 0;
                _context.AddLog($"导入进度: {currentRow}/{totalRows} ({percentage:F1}%) - {message}");
            }

            public void ReportError(int rowNumber, string error)
            {
                _context.AddLog($"第 {rowNumber} 行导入错误: {error}");
            }

            public void SetStatus(string status)
            {
                _context.AddLog($"状态: {status}");
            }

            public void UpdateProgress(int percentage, string message)
            {
                _context.AddLog($"进度: {percentage}% - {message}");
            }

            public void UpdateProgress(double progress, string message)
            {
                _context.AddLog($"进度: {progress:F1}% - {message}");
            }

            public void UpdateStatistics(int totalRows, int processedRows, int successRows, int failedRows)
            {
                _context.AddLog($"统计: 总计{totalRows}行, 已处理{processedRows}行, 成功{successRows}行, 失败{failedRows}行");
            }

            public void UpdateCurrentRow(int currentRow, int totalRows)
            {
                _context.AddLog($"当前处理: 第 {currentRow} 行，共 {totalRows} 行");
            }

            public void UpdateBatchInfo(int currentBatch, int batchSize, int totalBatches)
            {
                _context.AddLog($"批次信息: 第 {currentBatch} 批，批次大小 {batchSize}，共 {totalBatches} 批");
            }
        }
} 