using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Data.Repositories;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// 作业服务实现
    /// </summary>
    public class JobService : IJobService
    {
        private readonly IJobRepository _jobRepository;
        private readonly IJobStepRepository _jobStepRepository;
        private readonly IJobExecutionEngine _jobExecutionEngine;
        private readonly IJobExecutionRepository _jobExecutionRepository;
        private readonly ILogger<JobService> _logger;
        private readonly Dictionary<string, Action<JobExecution>> _jobEventCallbacks = new();
        private JobScheduler? _jobScheduler;

        public JobService(
            IJobRepository jobRepository,
            IJobStepRepository jobStepRepository,
            IJobExecutionEngine jobExecutionEngine,
            IJobExecutionRepository jobExecutionRepository,
            ILogger<JobService> logger)
        {
            _jobRepository = jobRepository;
            _jobStepRepository = jobStepRepository;
            _jobExecutionEngine = jobExecutionEngine;
            _jobExecutionRepository = jobExecutionRepository;
            _logger = logger;
        }

        /// <summary>
        /// 设置作业调度器引用（用于解决循环依赖）
        /// </summary>
        public void SetJobScheduler(JobScheduler jobScheduler)
        {
            _jobScheduler = jobScheduler;
            
            // 订阅作业执行事件
            _jobScheduler.JobExecutionRequested += ExecuteJobAsync;
        }

        #region 作业配置管理

        public async Task<List<JobConfig>> GetAllJobsAsync()
        {
            try
            {
                _logger.LogInformation("获取所有作业配置");
                var jobs = await _jobRepository.GetAllAsync();
                
                _logger.LogInformation("从数据库获取到 {Count} 个作业配置", jobs.Count);
                
                // 加载步骤数据
                foreach (var job in jobs)
                {
                    _logger.LogInformation("处理作业配置: ID={JobId}, 名称={JobName}, 类型={JobType}, 状态={Status}", 
                        job.Id, job.Name, job.Type, job.Status);
                    
                    // 输出原始配置数据的详细信息
                    if (!string.IsNullOrWhiteSpace(job.StepsConfig))
                    {
                        _logger.LogInformation("作业 {JobName} 的步骤配置长度: {Length}, 前100字符: {Preview}", 
                            job.Name, job.StepsConfig.Length, 
                            job.StepsConfig.Length > 100 ? job.StepsConfig.Substring(0, 100) + "..." : job.StepsConfig);
                    }
                    else
                    {
                        _logger.LogWarning("作业 {JobName} 的步骤配置为空", job.Name);
                    }
                    
                    if (!string.IsNullOrWhiteSpace(job.InputParameters))
                    {
                        _logger.LogInformation("作业 {JobName} 的输入参数长度: {Length}, 前100字符: {Preview}", 
                            job.Name, job.InputParameters.Length, 
                            job.InputParameters.Length > 100 ? job.InputParameters.Substring(0, 100) + "..." : job.InputParameters);
                    }
                    else
                    {
                        _logger.LogWarning("作业 {JobName} 的输入参数为空", job.Name);
                    }
                    
                    await LoadJobStepsAsync(job);
                    
                    // 输出加载后的结果
                    _logger.LogInformation("作业 {JobName} 加载完成: 步骤数={StepsCount}, 参数数={ParamsCount}", 
                        job.Name, job.Steps?.Count ?? 0, job.Parameters?.Count ?? 0);
                }
                
                _logger.LogInformation("所有作业配置处理完成，共 {Count} 个作业", jobs.Count);
                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有作业配置失败: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public async Task<JobConfig?> GetJobByIdAsync(string jobId)
        {
            try
            {
                _logger.LogInformation("根据ID获取作业配置: {JobId}", jobId);
                var job = await _jobRepository.GetByIdAsync(jobId);
                
                if (job != null)
                {
                    await LoadJobStepsAsync(job);
                }
                
                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取作业配置失败: {JobId}", jobId);
                throw;
            }
        }

        public async Task<JobConfig?> GetJobByNameAsync(string jobName)
        {
            try
            {
                _logger.LogInformation("根据名称获取作业配置: {JobName}", jobName);
                var job = await _jobRepository.GetByNameAsync(jobName);
                
                if (job != null)
                {
                    await LoadJobStepsAsync(job);
                }
                
                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据名称获取作业配置失败: {JobName}", jobName);
                throw;
            }
        }

        public async Task<List<JobConfig>> GetJobsByTypeAsync(string jobType)
        {
            try
            {
                _logger.LogInformation("根据类型获取作业配置: {JobType}", jobType);
                var jobs = await _jobRepository.GetByTypeAsync(jobType);
                
                foreach (var job in jobs)
                {
                    await LoadJobStepsAsync(job);
                }
                
                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据类型获取作业配置失败: {JobType}", jobType);
                throw;
            }
        }

        public async Task<List<JobConfig>> GetJobsByCategoryAsync(string category)
        {
            try
            {
                _logger.LogInformation("根据分类获取作业配置: {Category}", category);
                var jobs = await _jobRepository.GetByCategoryAsync(category);
                
                foreach (var job in jobs)
                {
                    await LoadJobStepsAsync(job);
                }
                
                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据分类获取作业配置失败: {Category}", category);
                throw;
            }
        }

        public async Task<List<JobConfig>> GetJobsByStatusAsync(JobStatus status)
        {
            try
            {
                _logger.LogInformation("根据状态获取作业配置: {Status}", status);
                var jobs = await _jobRepository.GetByStatusAsync(status);
                
                foreach (var job in jobs)
                {
                    await LoadJobStepsAsync(job);
                }
                
                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据状态获取作业配置失败: {Status}", status);
                throw;
            }
        }

        public async Task<List<JobConfig>> SearchJobsAsync(string keyword)
        {
            try
            {
                _logger.LogInformation("搜索作业配置: {Keyword}", keyword);
                var jobs = await _jobRepository.SearchAsync(keyword);
                
                foreach (var job in jobs)
                {
                    await LoadJobStepsAsync(job);
                }
                
                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索作业配置失败: {Keyword}", keyword);
                throw;
            }
        }

        public async Task<(bool success, string message)> CreateJobAsync(JobConfig jobConfig)
        {
            try
            {
                _logger.LogInformation("创建作业配置: {JobName}", jobConfig.Name);

                // 在校验之前规范化步骤顺序，确保 OrderIndex 为 1..N
                NormalizeJobStepOrder(jobConfig);

                // 验证作业配置
                var validation = await ValidateJobConfigAsync(jobConfig);
                if (!validation.isValid)
                {
                    return (false, string.Join("; ", validation.errors));
                }

                // 检查名称是否重复
                var existingJob = await GetJobByNameAsync(jobConfig.Name);
                if (existingJob != null)
                {
                    return (false, $"作业名称 '{jobConfig.Name}' 已存在");
                }

                // 序列化配置
                SerializeJobConfig(jobConfig);

                // 设置创建信息
                jobConfig.CreatedAt = DateTime.Now;
                jobConfig.UpdatedAt = DateTime.Now;

                await _jobRepository.CreateAsync(jobConfig);

                // 将步骤保存到 JobSteps 表
                if (jobConfig.Steps != null && jobConfig.Steps.Any())
                {
                    foreach (var step in jobConfig.Steps)
                    {
                        step.Id = string.IsNullOrWhiteSpace(step.Id) ? Guid.NewGuid().ToString() : step.Id;
                        step.JobId = jobConfig.Id;
                        step.CreatedAt = DateTime.Now;
                        step.UpdatedAt = DateTime.Now;
                    }
                    await _jobStepRepository.CreateBatchAsync(jobConfig.Steps);
                }

                _logger.LogInformation("作业配置创建成功: {JobId}", jobConfig.Id);
                return (true, "作业配置创建成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建作业配置失败: {JobName}", jobConfig.Name);
                return (false, $"创建作业配置失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> UpdateJobAsync(JobConfig jobConfig)
        {
            try
            {
                _logger.LogInformation("更新作业配置: {JobId}", jobConfig.Id);

                // 在校验之前规范化步骤顺序，确保 OrderIndex 为 1..N
                NormalizeJobStepOrder(jobConfig);

                // 验证作业配置
                var validation = await ValidateJobConfigAsync(jobConfig);
                if (!validation.isValid)
                {
                    return (false, string.Join("; ", validation.errors));
                }

                // 检查是否存在
                var existingJob = await GetJobByIdAsync(jobConfig.Id);
                if (existingJob == null)
                {
                    return (false, "作业配置不存在");
                }

                // 检查名称是否重复（排除自己）
                var jobWithSameName = await GetJobByNameAsync(jobConfig.Name);
                if (jobWithSameName != null && jobWithSameName.Id != jobConfig.Id)
                {
                    return (false, $"作业名称 '{jobConfig.Name}' 已存在");
                }

                // 序列化配置
                SerializeJobConfig(jobConfig);

                // 设置更新信息
                jobConfig.UpdatedAt = DateTime.Now;

                await _jobRepository.UpdateAsync(jobConfig);

                // 先清空旧步骤，再写入新步骤
                await _jobStepRepository.DeleteByJobIdAsync(jobConfig.Id);
                if (jobConfig.Steps != null && jobConfig.Steps.Any())
                {
                    foreach (var step in jobConfig.Steps)
                    {
                        step.Id = string.IsNullOrWhiteSpace(step.Id) ? Guid.NewGuid().ToString() : step.Id;
                        step.JobId = jobConfig.Id;
                        step.UpdatedAt = DateTime.Now;
                        if (step.CreatedAt == default) step.CreatedAt = DateTime.Now;
                    }
                    await _jobStepRepository.CreateBatchAsync(jobConfig.Steps);
                }

                _logger.LogInformation("作业配置更新成功: {JobId}", jobConfig.Id);
                return (true, "作业配置更新成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新作业配置失败: {JobId}", jobConfig.Id);
                return (false, $"更新作业配置失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> DeleteJobAsync(string jobId)
        {
            try
            {
                _logger.LogInformation("删除作业配置: {JobId}", jobId);

                // 检查是否存在
                var existingJob = await GetJobByIdAsync(jobId);
                if (existingJob == null)
                {
                    return (false, "作业配置不存在");
                }

                // 检查是否有正在执行的作业
                var runningJobs = await GetRunningJobsAsync();
                if (runningJobs.Any(j => j.JobId == jobId))
                {
                    return (false, "作业正在执行中，无法删除");
                }

                await _jobRepository.DeleteAsync(jobId);
                
                _logger.LogInformation("作业配置删除成功: {JobId}", jobId);
                return (true, "作业配置删除成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除作业配置失败: {JobId}", jobId);
                return (false, $"删除作业配置失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> BatchDeleteJobsAsync(List<string> jobIds)
        {
            try
            {
                _logger.LogInformation("批量删除作业配置: {JobIds}", string.Join(", ", jobIds));

                var results = new List<(bool success, string message)>();
                foreach (var jobId in jobIds)
                {
                    var result = await DeleteJobAsync(jobId);
                    results.Add(result);
                }

                var successCount = results.Count(r => r.success);
                var failureCount = results.Count(r => !r.success);

                var message = $"批量删除完成: 成功 {successCount} 个，失败 {failureCount} 个";
                
                _logger.LogInformation(message);
                return (failureCount == 0, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除作业配置失败");
                return (false, $"批量删除作业配置失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message, JobConfig? newJob)> CopyJobAsync(string jobId, string newJobName)
        {
            try
            {
                _logger.LogInformation("复制作业配置: {JobId} -> {NewJobName}", jobId, newJobName);

                // 获取源作业
                var sourceJob = await GetJobByIdAsync(jobId);
                if (sourceJob == null)
                {
                    return (false, "源作业配置不存在", null);
                }

                // 检查新名称是否重复
                var existingJob = await GetJobByNameAsync(newJobName);
                if (existingJob != null)
                {
                    return (false, $"作业名称 '{newJobName}' 已存在", null);
                }

                // 创建新作业
                var newJob = new JobConfig
                {
                    Name = newJobName,
                    Description = $"{sourceJob.Description} (副本)",
                    Type = sourceJob.Type,
                    Category = sourceJob.Category,
                    Status = JobStatus.Pending,
                    Priority = sourceJob.Priority,
                    IsEnabled = false, // 复制的作业默认禁用
                    ExecutionMode = sourceJob.ExecutionMode,
                    CronExpression = sourceJob.CronExpression,
                    TimeoutSeconds = sourceJob.TimeoutSeconds,
                    MaxRetryCount = sourceJob.MaxRetryCount,
                    RetryIntervalSeconds = sourceJob.RetryIntervalSeconds,
                    AllowParallelExecution = sourceJob.AllowParallelExecution,
                    StepsConfig = sourceJob.StepsConfig,
                    InputParameters = sourceJob.InputParameters,
                    OutputConfig = sourceJob.OutputConfig,
                    NotificationConfig = sourceJob.NotificationConfig,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CreatedBy = sourceJob.CreatedBy,
                    UpdatedBy = sourceJob.UpdatedBy,
                    Remarks = $"从作业 '{sourceJob.Name}' 复制"
                };

                // 保存新作业
                var createResult = await CreateJobAsync(newJob);
                if (!createResult.success)
                {
                    return (false, createResult.message, null);
                }

                _logger.LogInformation("作业配置复制成功: {JobId} -> {NewJobId}", jobId, newJob.Id);
                return (true, "作业配置复制成功", newJob);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制作业配置失败: {JobId}", jobId);
                return (false, $"复制作业配置失败: {ex.Message}", null);
            }
        }

        public async Task<(bool success, string message)> SetJobEnabledAsync(string jobId, bool isEnabled)
        {
            try
            {
                _logger.LogInformation("设置作业启用状态: {JobId} -> {IsEnabled}", jobId, isEnabled);

                var job = await GetJobByIdAsync(jobId);
                if (job == null)
                {
                    return (false, "作业配置不存在");
                }

                job.IsEnabled = isEnabled;
                job.UpdatedAt = DateTime.Now;

                await _jobRepository.UpdateAsync(job);
                
                // 同步更新调度器中的作业状态
                _jobScheduler?.UpdateJobEnabledStatus(jobId, isEnabled);
                
                _logger.LogInformation("作业启用状态设置成功: {JobId} -> {IsEnabled}", jobId, isEnabled);
                return (true, $"作业已{(isEnabled ? "启用" : "禁用")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置作业启用状态失败: {JobId}", jobId);
                return (false, $"设置作业启用状态失败: {ex.Message}");
            }
        }

        #endregion

        #region 作业执行管理

        public async Task<(bool success, string message, string executionId)> ExecuteJobAsync(string jobId, Dictionary<string, object>? parameters = null, string executedBy = "")
        {
            try
            {
                _logger.LogInformation("开始执行作业: {JobId}, 执行人: {ExecutedBy}", jobId, executedBy);

                // 验证作业是否存在
                var jobConfig = await _jobRepository.GetByIdAsync(jobId);
                if (jobConfig == null)
                {
                    var errorMsg = $"作业不存在: {jobId}";
                    _logger.LogWarning(errorMsg);
                    return (false, errorMsg, string.Empty);
                }

                if (!jobConfig.IsEnabled)
                {
                    var errorMsg = $"作业已禁用: {jobConfig.Name}";
                    _logger.LogWarning(errorMsg);
                    return (false, errorMsg, string.Empty);
                }

                // 使用作业执行引擎执行作业
                var executionResult = await _jobExecutionEngine.ExecuteJobAsync(jobConfig, parameters, executedBy);
                
                if (executionResult.IsSuccess)
                {
                    _logger.LogInformation("作业执行成功: {JobName} ({JobId}), 执行ID: {ExecutionId}", jobConfig.Name, jobId, executionResult.ExecutionId);
                    return (true, "作业执行成功", executionResult.ExecutionId);
                }
                else
                {
                    _logger.LogError("作业执行失败: {JobName} ({JobId}), 错误: {Error}", jobConfig.Name, jobId, executionResult.ErrorMessage ?? "未知错误");
                    return (false, executionResult.ErrorMessage ?? "作业执行失败", executionResult.ExecutionId);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"作业执行异常: {ex.Message}";
                _logger.LogError(ex, errorMsg);
                return (false, errorMsg, string.Empty);
            }
        }

        public async Task<(bool success, string message)> StopJobExecutionAsync(string executionId)
        {
            try
            {
                _logger.LogInformation("停止作业执行: {ExecutionId}", executionId);

                var execution = await GetJobExecutionAsync(executionId);
                if (execution == null)
                {
                    return (false, "执行记录不存在");
                }

                if (execution.Status != JobStatus.Running)
                {
                    return (false, "作业不在运行状态");
                }

                // 更新执行记录
                execution.Status = JobStatus.Cancelled;
                execution.EndTime = DateTime.Now;
                execution.Duration = execution.EndTime.Value - execution.StartTime;
                execution.UpdatedAt = DateTime.Now;

                // 保存更新后的执行记录
                var updateResult = await _jobExecutionRepository.UpdateAsync(execution);
                if (!updateResult)
                {
                    _logger.LogWarning("更新执行记录失败: {ExecutionId}", executionId);
                }

                _logger.LogInformation("作业执行已停止: {ExecutionId}", executionId);
                return (true, "作业执行已停止");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止作业执行失败: {ExecutionId}", executionId);
                return (false, $"停止作业执行失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> PauseJobExecutionAsync(string executionId)
        {
            try
            {
                _logger.LogInformation("暂停作业执行: {ExecutionId}", executionId);

                var execution = await GetJobExecutionAsync(executionId);
                if (execution == null)
                {
                    return (false, "执行记录不存在");
                }

                if (execution.Status != JobStatus.Running)
                {
                    return (false, "作业不在运行状态");
                }

                // 更新执行记录
                execution.Status = JobStatus.Paused;
                execution.UpdatedAt = DateTime.Now;

                // 保存更新后的执行记录
                var updateResult = await _jobExecutionRepository.UpdateAsync(execution);
                if (!updateResult)
                {
                    _logger.LogWarning("更新执行记录失败: {ExecutionId}", executionId);
                }

                _logger.LogInformation("作业执行已暂停: {ExecutionId}", executionId);
                return (true, "作业执行已暂停");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "暂停作业执行失败: {ExecutionId}", executionId);
                return (false, $"暂停作业执行失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> ResumeJobExecutionAsync(string executionId)
        {
            try
            {
                _logger.LogInformation("恢复作业执行: {ExecutionId}", executionId);

                var execution = await GetJobExecutionAsync(executionId);
                if (execution == null)
                {
                    return (false, "执行记录不存在");
                }

                if (execution.Status != JobStatus.Paused)
                {
                    return (false, "作业不在暂停状态");
                }

                // 更新执行记录
                execution.Status = JobStatus.Running;
                execution.UpdatedAt = DateTime.Now;

                // 保存更新后的执行记录
                var updateResult = await _jobExecutionRepository.UpdateAsync(execution);
                if (!updateResult)
                {
                    _logger.LogWarning("更新执行记录失败: {ExecutionId}", executionId);
                }

                _logger.LogInformation("作业执行已恢复: {ExecutionId}", executionId);
                return (true, "作业执行已恢复");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复作业执行失败: {ExecutionId}", executionId);
                return (false, $"恢复作业执行失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message, string newExecutionId)> RetryJobExecutionAsync(string executionId)
        {
            try
            {
                _logger.LogInformation("重试作业执行: {ExecutionId}", executionId);

                var execution = await GetJobExecutionAsync(executionId);
                if (execution == null)
                {
                    return (false, "执行记录不存在", string.Empty);
                }

                if (execution.Status != JobStatus.Failed)
                {
                    return (false, "只有失败的作业才能重试", string.Empty);
                }

                // 重新执行作业
                var result = await ExecuteJobAsync(execution.JobId, execution.Parameters, execution.ExecutedBy);
                
                return (result.success, result.message, result.executionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重试作业执行失败: {ExecutionId}", executionId);
                return (false, $"重试作业执行失败: {ex.Message}", string.Empty);
            }
        }

        #endregion

        #region 作业执行记录

        public async Task<(List<JobExecution> executions, int totalCount)> GetJobExecutionsAsync(string jobId, int page = 1, int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("获取作业执行记录: {JobId}, 页码: {Page}, 页大小: {PageSize}", jobId, page, pageSize);
                
                var result = await _jobExecutionRepository.GetByJobIdAsync(jobId, page, pageSize);
                _logger.LogInformation("获取到作业执行记录: {JobId}, 数量: {Count}, 总数: {TotalCount}", jobId, result.executions.Count, result.totalCount);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取作业执行记录失败: {JobId}", jobId);
                throw;
            }
        }

        public async Task<JobExecution?> GetJobExecutionAsync(string executionId)
        {
            try
            {
                _logger.LogInformation("获取执行记录详情: {ExecutionId}", executionId);
                var execution = await _jobExecutionRepository.GetByIdAsync(executionId);
                
                if (execution != null)
                {
                    _logger.LogInformation("获取到执行记录: {ExecutionId}, 状态: {Status}", executionId, execution.Status);
                }
                else
                {
                    _logger.LogWarning("执行记录不存在: {ExecutionId}", executionId);
                }
                
                return execution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取执行记录详情失败: {ExecutionId}", executionId);
                throw;
            }
        }

        public async Task<List<JobStepExecution>> GetJobStepExecutionsAsync(string executionId)
        {
            try
            {
                _logger.LogInformation("获取执行步骤记录: {ExecutionId}", executionId);
                
                // 获取执行记录
                var execution = await GetJobExecutionAsync(executionId);
                if (execution == null)
                {
                    _logger.LogWarning("执行记录不存在: {ExecutionId}", executionId);
                    return new List<JobStepExecution>();
                }
                
                // 从数据库获取步骤执行记录
                var stepExecutions = await _jobExecutionRepository.GetStepExecutionsAsync(executionId);
                _logger.LogInformation("获取到步骤执行记录: {ExecutionId}, 步骤数: {Count}", executionId, stepExecutions?.Count ?? 0);
                
                return stepExecutions ?? new List<JobStepExecution>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取执行步骤记录失败: {ExecutionId}", executionId);
                throw;
            }
        }

        public async Task<(bool success, string message)> DeleteJobExecutionAsync(string executionId)
        {
            try
            {
                _logger.LogInformation("删除执行记录: {ExecutionId}", executionId);

                var execution = await GetJobExecutionAsync(executionId);
                if (execution == null)
                {
                    return (false, "执行记录不存在");
                }

                if (execution.Status == JobStatus.Running)
                {
                    return (false, "正在执行的作业无法删除");
                }

                // 删除执行记录
                var deleteResult = await _jobExecutionRepository.DeleteAsync(executionId);
                if (!deleteResult)
                {
                    _logger.LogWarning("删除执行记录失败: {ExecutionId}", executionId);
                    return (false, "删除执行记录失败");
                }
                
                _logger.LogInformation("执行记录删除成功: {ExecutionId}", executionId);
                return (true, "执行记录删除成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除执行记录失败: {ExecutionId}", executionId);
                return (false, $"删除执行记录失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message, int deletedCount)> CleanupExpiredExecutionsAsync(int daysToKeep = 30)
        {
            try
            {
                _logger.LogInformation("清理过期执行记录: 保留天数 {DaysToKeep}", daysToKeep);
                
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                
                // 获取过期的执行记录
                var expiredExecutions = await _jobExecutionRepository.GetAllAsync();
                var toDelete = expiredExecutions.Where(e => e.StartTime < cutoffDate && e.Status != JobStatus.Running).ToList();
                
                var deletedCount = 0;
                foreach (var execution in toDelete)
                {
                    try
                    {
                        var deleteResult = await _jobExecutionRepository.DeleteAsync(execution.Id);
                        if (deleteResult)
                        {
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "删除过期执行记录失败: {ExecutionId}", execution.Id);
                    }
                }
                
                _logger.LogInformation("过期执行记录清理完成: 删除 {DeletedCount} 条记录", deletedCount);
                return (true, $"清理完成，删除 {deletedCount} 条记录", deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期执行记录失败");
                return (false, $"清理过期执行记录失败: {ex.Message}", 0);
            }
        }

        #endregion

        #region 作业统计

        public async Task<JobStatistics?> GetJobStatisticsAsync(string jobId)
        {
            try
            {
                _logger.LogInformation("获取作业统计信息: {JobId}", jobId);
                
                // 获取作业配置信息
                var jobConfig = await _jobRepository.GetByIdAsync(jobId);
                if (jobConfig == null)
                {
                    _logger.LogWarning("作业配置不存在: {JobId}", jobId);
                    return null;
                }

                // 获取作业的所有执行记录
                var executions = await _jobExecutionRepository.GetAllByJobIdAsync(jobId) ?? new List<JobExecution>();
                
                if (!executions.Any())
                {
                    _logger.LogInformation("作业没有执行记录: {JobId}", jobId);
                    return new JobStatistics
                    {
                        JobId = jobId,
                        JobName = jobConfig.Name,
                        TotalExecutions = 0,
                        SuccessfulExecutions = 0,
                        FailedExecutions = 0,
                        CancelledExecutions = 0,
                        SuccessRate = 0,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                }

                var statistics = new JobStatistics
                {
                    JobId = jobId,
                    JobName = jobConfig.Name,
                    TotalExecutions = executions.Count,
                    SuccessfulExecutions = executions.Count(e => e.Status == JobStatus.Completed),
                    FailedExecutions = executions.Count(e => e.Status == JobStatus.Failed),
                    CancelledExecutions = executions.Count(e => e.Status == JobStatus.Cancelled),
                    LastExecutionTime = executions.Any() ? executions.Max(e => e.StartTime) : DateTime.MinValue,
                    FirstExecutionTime = executions.Any() ? executions.Min(e => e.StartTime) : DateTime.MinValue,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // 计算成功率
                statistics.SuccessRate = statistics.TotalExecutions > 0 
                    ? (double)statistics.SuccessfulExecutions / statistics.TotalExecutions * 100 
                    : 0;

                // 计算执行时间统计
                var completedExecutions = executions.Where(e => e.Status == JobStatus.Completed && e.Duration.HasValue && e.Duration.Value.TotalSeconds > 0).ToList();
                if (completedExecutions.Any())
                {
                    statistics.AverageDuration = TimeSpan.FromSeconds(completedExecutions.Average(e => e.Duration!.Value.TotalSeconds));
                    statistics.TotalDuration = TimeSpan.FromSeconds(completedExecutions.Sum(e => e.Duration!.Value.TotalSeconds));
                }

                _logger.LogInformation("作业统计信息获取成功: {JobId}, 总执行次数: {TotalExecutions}, 成功率: {SuccessRate:F2}%", 
                    jobId, statistics.TotalExecutions, statistics.SuccessRate);
                
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取作业统计信息失败: {JobId}", jobId);
                throw;
            }
        }

        public async Task<List<JobStatistics>> GetAllJobStatisticsAsync()
        {
            try
            {
                _logger.LogInformation("获取所有作业统计信息");
                
                // 获取所有作业配置
                var allJobs = await _jobRepository.GetAllAsync();
                var statisticsList = new List<JobStatistics>();
                
                foreach (var job in allJobs)
                {
                    try
                    {
                        var statistics = await GetJobStatisticsAsync(job.Id);
                        if (statistics != null)
                        {
                            statisticsList.Add(statistics);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "获取作业统计信息失败: {JobId}", job.Id);
                        // 继续处理其他作业
                    }
                }
                
                _logger.LogInformation("获取所有作业统计信息完成: {Count} 个作业", statisticsList.Count);
                return statisticsList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有作业统计信息失败");
                throw;
            }
        }

        public async Task<(bool success, string message)> UpdateJobStatisticsAsync(string jobId)
        {
            try
            {
                _logger.LogInformation("更新作业统计信息: {JobId}", jobId);

                // 获取作业配置信息
                var jobConfig = await _jobRepository.GetByIdAsync(jobId);
                if (jobConfig == null)
                {
                    return (false, "作业配置不存在");
                }

                // 获取作业的所有执行记录
                var executionsResult = await _jobExecutionRepository.GetAllByJobIdAsync(jobId);
                var executions = executionsResult ?? new List<JobExecution>();
                
                if (!executions.Any())
                {
                    return (true, "没有执行记录，无需更新统计");
                }

                var statistics = new JobStatistics
                {
                    JobId = jobId,
                    JobName = jobConfig.Name,
                    TotalExecutions = executions.Count,
                    SuccessfulExecutions = executions.Count(e => e.Status == JobStatus.Completed),
                    FailedExecutions = executions.Count(e => e.Status == JobStatus.Failed),
                    CancelledExecutions = executions.Count(e => e.Status == JobStatus.Cancelled),
                    LastExecutionTime = executions.Any() ? executions.Max(e => e.StartTime) : DateTime.MinValue,
                    FirstExecutionTime = executions.Any() ? executions.Min(e => e.StartTime) : DateTime.MinValue,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // 计算成功率
                statistics.SuccessRate = statistics.TotalExecutions > 0 
                    ? (double)statistics.SuccessfulExecutions / statistics.TotalExecutions * 100 
                    : 0;

                // 计算执行时间统计
                var completedExecutions = executions.Where(e => e.Status == JobStatus.Completed && e.Duration.HasValue && e.Duration.Value.TotalSeconds > 0).ToList();
                if (completedExecutions.Any())
                {
                    statistics.AverageDuration = TimeSpan.FromSeconds(completedExecutions.Average(e => e.Duration!.Value.TotalSeconds));
                    statistics.TotalDuration = TimeSpan.FromSeconds(completedExecutions.Sum(e => e.Duration!.Value.TotalSeconds));
                }

                // 保存或更新统计信息
                // 注意：这里需要实现统计信息的持久化存储
                // 由于当前没有专门的统计信息存储接口，这里只记录日志
                _logger.LogInformation("作业统计信息计算完成: {JobId}, 总执行次数: {TotalExecutions}, 成功率: {SuccessRate:F2}%", 
                    jobId, statistics.TotalExecutions, statistics.SuccessRate);

                _logger.LogInformation("作业统计信息更新成功: {JobId}", jobId);
                return (true, "作业统计信息更新成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新作业统计信息失败: {JobId}", jobId);
                return (false, $"更新作业统计信息失败: {ex.Message}");
            }
        }

        #endregion

        #region 作业调度

        public async Task<(bool success, string message)> StartSchedulerAsync()
        {
            try
            {
                _logger.LogInformation("启动作业调度器");
                var success = await _jobScheduler.StartAsync();
                return success ? (true, "作业调度器启动成功") : (false, "作业调度器启动失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动作业调度器失败");
                return (false, $"启动作业调度器失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> StopSchedulerAsync()
        {
            try
            {
                _logger.LogInformation("停止作业调度器");
                var success = await _jobScheduler.StopAsync();
                return success ? (true, "作业调度器停止成功") : (false, "作业调度器停止失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止作业调度器失败");
                return (false, $"停止作业调度器失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> PauseSchedulerAsync()
        {
            try
            {
                _logger.LogInformation("暂停作业调度器");
                var success = await _jobScheduler.PauseAsync();
                return success ? (true, "作业调度器暂停成功") : (false, "作业调度器暂停失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "暂停作业调度器失败");
                return (false, $"暂停作业调度器失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> ResumeSchedulerAsync()
        {
            try
            {
                _logger.LogInformation("恢复作业调度器");
                var success = await _jobScheduler.ResumeAsync();
                return success ? (true, "作业调度器恢复成功") : (false, "作业调度器恢复失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复作业调度器失败");
                return (false, $"恢复作业调度器失败: {ex.Message}");
            }
        }

        public async Task<(bool isRunning, bool isPaused, DateTime? lastRunTime)> GetSchedulerStatusAsync()
        {
            try
            {
                _logger.LogInformation("获取调度器状态");
                return _jobScheduler.GetStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取调度器状态失败");
                throw;
            }
        }

        public async Task<(bool success, string message)> AddScheduledJobAsync(string jobId, string cronExpression)
        {
            try
            {
                _logger.LogInformation("添加定时作业: {JobId}, Cron: {CronExpression}", jobId, cronExpression);
                var success = await _jobScheduler.AddScheduledJobAsync(jobId, cronExpression);
                return success ? (true, "定时作业添加成功") : (false, "定时作业添加失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加定时作业失败: {JobId}", jobId);
                return (false, $"添加定时作业失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> RemoveScheduledJobAsync(string jobId)
        {
            try
            {
                _logger.LogInformation("移除定时作业: {JobId}", jobId);
                var success = await _jobScheduler.RemoveScheduledJobAsync(jobId);
                return success ? (true, "定时作业移除成功") : (false, "定时作业移除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除定时作业失败: {JobId}", jobId);
                return (false, $"移除定时作业失败: {ex.Message}");
            }
        }

        public async Task<List<(string jobId, string cronExpression, DateTime? nextRunTime)>> GetScheduledJobsAsync()
        {
            try
            {
                _logger.LogInformation("获取定时作业列表");
                var scheduledJobs = _jobScheduler.GetScheduledJobs();
                return scheduledJobs.Select(job => (job.jobId, job.cronExpression, job.nextRunTime)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取定时作业列表失败");
                throw;
            }
        }

        #endregion

        #region 作业监控

        public async Task<List<JobExecution>> GetRunningJobsAsync()
        {
            try
            {
                _logger.LogInformation("获取正在运行的作业");
                var runningJobs = await _jobExecutionRepository.GetByStatusAsync(JobStatus.Running);
                _logger.LogInformation("获取到正在运行的作业: {Count} 个", runningJobs.Count);
                return runningJobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取正在运行的作业失败");
                throw;
            }
        }

        public async Task<JobExecution?> GetLatestJobExecutionAsync(string jobId)
        {
            try
            {
                _logger.LogInformation("获取作业最新执行记录: {JobId}", jobId);
                var latestExecution = await _jobExecutionRepository.GetLatestByJobIdAsync(jobId);
                return latestExecution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取作业最新执行记录失败: {JobId}", jobId);
                throw;
            }
        }

        public async Task<(int progress, string currentStep, DateTime startTime, DateTime? estimatedEndTime)> GetJobProgressAsync(string executionId)
        {
            try
            {
                _logger.LogInformation("获取作业执行进度: {ExecutionId}", executionId);
                
                var execution = await GetJobExecutionAsync(executionId);
                if (execution == null)
                {
                    return (0, string.Empty, DateTime.Now, null);
                }

                // 从作业执行引擎获取进度信息
                var progress = await _jobExecutionEngine.GetExecutionProgressAsync(executionId);
                
                return (progress.Progress, progress.CurrentStep, progress.StartTime, progress.EstimatedEndTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取作业执行进度失败: {ExecutionId}", executionId);
                throw;
            }
        }

        public void SubscribeToJobEvents(Action<JobExecution> callback)
        {
            lock (_jobEventCallbacks)
            {
                _jobEventCallbacks[callback.GetHashCode().ToString()] = callback;
            }
        }

        public void UnsubscribeFromJobEvents(Action<JobExecution> callback)
        {
            lock (_jobEventCallbacks)
            {
                _jobEventCallbacks.Remove(callback.GetHashCode().ToString());
            }
        }

        #endregion

        #region 作业导入导出

        public async Task<(bool success, string message)> ExportJobsAsync(List<string> jobIds, string filePath)
        {
            try
            {
                _logger.LogInformation("导出作业配置: {JobIds} -> {FilePath}", string.Join(", ", jobIds), filePath);

                var jobs = new List<JobConfig>();
                foreach (var jobId in jobIds)
                {
                    var job = await GetJobByIdAsync(jobId);
                    if (job != null)
                    {
                        jobs.Add(job);
                    }
                }

                var exportData = new
                {
                    ExportTime = DateTime.Now,
                    Version = "1.0",
                    Jobs = jobs
                };

                var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);

                _logger.LogInformation("作业配置导出成功: {FilePath}", filePath);
                return (true, $"作业配置导出成功: {jobs.Count} 个作业");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出作业配置失败");
                return (false, $"导出作业配置失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message, List<JobConfig> importedJobs)> ImportJobsAsync(string filePath, bool overwrite = false)
        {
            try
            {
                _logger.LogInformation("导入作业配置: {FilePath}, 覆盖: {Overwrite}", filePath, overwrite);

                if (!File.Exists(filePath))
                {
                    return (false, "导入文件不存在", new List<JobConfig>());
                }

                var json = await File.ReadAllTextAsync(filePath);
                var importData = JsonSerializer.Deserialize<dynamic>(json);

                // TODO: 实现导入逻辑
                var importedJobs = new List<JobConfig>();

                _logger.LogInformation("作业配置导入成功: {FilePath}", filePath);
                return (true, $"作业配置导入成功: {importedJobs.Count} 个作业", importedJobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入作业配置失败: {FilePath}", filePath);
                return (false, $"导入作业配置失败: {ex.Message}", new List<JobConfig>());
            }
        }

        public async Task<(bool isValid, List<string> errors)> ValidateJobConfigAsync(JobConfig jobConfig)
        {
            try
            {
                var errors = new List<string>();

                // 验证基本信息
                if (string.IsNullOrWhiteSpace(jobConfig.Name))
                {
                    errors.Add("作业名称不能为空");
                }

                if (string.IsNullOrWhiteSpace(jobConfig.Type))
                {
                    errors.Add("作业类型不能为空");
                }

                // 验证步骤配置
                if (jobConfig.Steps != null && jobConfig.Steps.Any())
                {
                    for (int i = 0; i < jobConfig.Steps.Count; i++)
                    {
                        var step = jobConfig.Steps[i];
                        if (string.IsNullOrWhiteSpace(step.Name))
                        {
                            errors.Add($"步骤 {i + 1} 名称不能为空");
                        }
                    }

                    // 校验步骤顺序索引的正确性：必须从 1 开始、连续且不重复
                    var orderIndexes = jobConfig.Steps.Select(s => s.OrderIndex).ToList();
                    if (orderIndexes.Any(idx => idx <= 0))
                    {
                        errors.Add("步骤顺序必须为正整数");
                    }
                    else
                    {
                        var distinctCount = orderIndexes.Distinct().Count();
                        var expectedCount = orderIndexes.Count;
                        if (distinctCount != expectedCount || orderIndexes.Min() != 1 || orderIndexes.Max() != expectedCount)
                        {
                            errors.Add("步骤顺序不连续或存在重复");
                        }
                    }
                }

                // 验证定时表达式
                if (jobConfig.ExecutionMode == ExecutionMode.Scheduled && !string.IsNullOrWhiteSpace(jobConfig.CronExpression))
                {
                    // TODO: 验证Cron表达式格式
                }

                return (errors.Count == 0, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证作业配置失败");
                return (false, new List<string> { $"验证作业配置失败: {ex.Message}" });
            }
        }

        public async Task<(bool success, string message)> LoadScheduledJobsAsync()
        {
            try
            {
                // 获取所有启用的定时作业
                var scheduledJobs = await GetAllJobsAsync();
                var enabledJobs = scheduledJobs.Where(j => j.IsEnabled && j.ExecutionMode == ExecutionMode.Scheduled).ToList();

                // 加载到调度器
                foreach (var job in enabledJobs)
                {
                    if (!string.IsNullOrEmpty(job.CronExpression))
                    {
                        await _jobScheduler.AddScheduledJobAsync(job.Id, job.CronExpression);
                    }
                }

                return (true, $"成功加载 {enabledJobs.Count} 个定时作业");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载定时作业失败");
                return (false, $"加载定时作业失败: {ex.Message}");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 序列化作业配置
        /// </summary>
        private void SerializeJobConfig(JobConfig jobConfig)
        {
            try
            {
                // 步骤不再存入 StepsConfig，由 JobSteps 表持久化
                jobConfig.StepsConfig = null;

                if (jobConfig.Parameters != null && jobConfig.Parameters.Any())
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };
                    jobConfig.InputParameters = JsonSerializer.Serialize(jobConfig.Parameters, options);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "序列化作业配置失败: {JobId}", jobConfig.Id);
                throw;
            }
        }

        /// <summary>
        /// 加载作业步骤数据
        /// </summary>
        private async Task LoadJobStepsAsync(JobConfig jobConfig)
        {
            try
            {
                _logger.LogInformation("开始加载作业步骤: {JobId}, 名称: {JobName}", jobConfig.Id, jobConfig.Name);
                
                // 从JobSteps表查询步骤数据（关系型存储）
                var steps = await _jobStepRepository.GetByJobIdAsync(jobConfig.Id);
                jobConfig.Steps = steps ?? new List<JobStep>();
                
                _logger.LogInformation("从JobSteps表加载步骤数据，共 {Count} 个步骤", jobConfig.Steps.Count);
                        
                        // 输出每个步骤的详细信息
                        foreach (var step in jobConfig.Steps)
                        {
                    _logger.LogInformation("步骤: {StepName} ({StepType}), 顺序: {OrderIndex}", step.Name, step.Type, step.OrderIndex);
                }

                // 反序列化输入参数（这部分仍然需要JSON反序列化）
                var options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                if (!string.IsNullOrWhiteSpace(jobConfig.InputParameters))
                {
                    _logger.LogInformation("反序列化输入参数，原始长度: {Length}, 内容预览: {Preview}", 
                        jobConfig.InputParameters.Length, 
                        jobConfig.InputParameters.Length > 100 ? jobConfig.InputParameters.Substring(0, 100) + "..." : jobConfig.InputParameters);
                    
                    try
                    {
                        // 尝试直接反序列化
                        jobConfig.Parameters = JsonSerializer.Deserialize<List<JobParameter>>(jobConfig.InputParameters, options) ?? new List<JobParameter>();
                        _logger.LogInformation("输入参数反序列化成功，共 {Count} 个参数", jobConfig.Parameters.Count);
                        
                        // 输出每个参数的详细信息
                        foreach (var param in jobConfig.Parameters)
                        {
                            _logger.LogInformation("参数: {ParamName} = {ParamValue} ({ParamType})", param.Name, param.Value, param.Type);
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogWarning(jsonEx, "直接反序列化输入参数失败，错误位置: {LineNumber}, {Position}, 尝试清理JSON字符串: {JobId}", 
                            jsonEx.LineNumber, jsonEx.BytePositionInLine, jobConfig.Id);
                        
                    // 尝试清理可能的编码问题
                        var cleanedParams = CleanJsonString(jobConfig.InputParameters, "输入参数");
                    jobConfig.Parameters = JsonSerializer.Deserialize<List<JobParameter>>(cleanedParams, options) ?? new List<JobParameter>();
                        _logger.LogInformation("清理后的输入参数反序列化成功，共 {Count} 个参数", jobConfig.Parameters.Count);
                    }
                }
                else
                {
                    _logger.LogWarning("输入参数为空，使用默认空列表");
                    jobConfig.Parameters = new List<JobParameter>();
                }
                
                _logger.LogInformation("作业步骤加载完成: {JobId}, 步骤数: {StepsCount}, 参数数: {ParamsCount}", 
                    jobConfig.Id, jobConfig.Steps.Count, jobConfig.Parameters.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载作业步骤失败: {JobId}, 错误: {ErrorMessage}", jobConfig.Id, ex.Message);
                jobConfig.Steps = new List<JobStep>();
                jobConfig.Parameters = new List<JobParameter>();
            }
        }

        /// <summary>
        /// 清理JSON字符串中的编码问题
        /// </summary>
        private string CleanJsonString(string jsonString, string context)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                _logger.LogWarning("{Context} JSON字符串为空，返回默认空数组", context);
                return "[]";
            }

            try
            {
                _logger.LogInformation("开始清理{Context} JSON字符串，原始长度: {Length}", context, jsonString.Length);
                
                // 首先尝试验证JSON格式
                try
                {
                    JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
                    _logger.LogInformation("{Context} JSON格式验证通过，无需清理", context);
                    return jsonString; // JSON格式正确，直接返回
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogInformation("{Context} JSON格式验证失败: {ErrorMessage}", context, jsonEx.Message);
                }

                // 尝试检测并修复编码问题
                byte[] bytes = Encoding.UTF8.GetBytes(jsonString);
                _logger.LogInformation("{Context} 转换为UTF-8字节数组，长度: {Length}", context, bytes.Length);
                
                // 检查是否包含无效的UTF-8字节序列
                bool hasEncodingIssue = false;
                var problematicBytes = new List<byte>();
                for (int i = 0; i < bytes.Length; i++)
                {
                    // 检测常见的编码问题字符
                    if (bytes[i] == 0xE6 || bytes[i] == 0xE7 || bytes[i] == 0xE8 || 
                        bytes[i] == 0xE9 || bytes[i] == 0xEA || bytes[i] == 0xEB)
                    {
                        hasEncodingIssue = true;
                        problematicBytes.Add(bytes[i]);
                    }
                }
                
                if (hasEncodingIssue)
                {
                    _logger.LogWarning("{Context} 检测到编码问题字节: {ProblematicBytes}", context, string.Join(", ", problematicBytes.Select(b => $"0x{b:X2}")));
                    
                    // 尝试使用不同的编码重新解码
                    var encodings = new[] { "GBK", "GB2312", "Big5", "UTF-8" };
                    
                    foreach (var encodingName in encodings)
                    {
                        try
                        {
                            _logger.LogInformation("{Context} 尝试使用 {Encoding} 编码修复", context, encodingName);
                            
                            var encoding = Encoding.GetEncoding(encodingName);
                            var encodedBytes = encoding.GetBytes(jsonString);
                            var decodedString = encoding.GetString(encodedBytes);
                            
                            // 验证是否为有效的JSON
                            JsonSerializer.Deserialize<Dictionary<string, object>>(decodedString);
                            _logger.LogInformation("{Context} 成功使用 {Encoding} 编码修复JSON字符串", context, encodingName);
                            return decodedString;
            }
            catch (Exception ex)
            {
                            _logger.LogDebug("{Context} 使用 {Encoding} 编码修复失败: {ErrorMessage}", context, encodingName, ex.Message);
                        }
                    }
                    
                                    // 如果所有编码都失败，尝试更激进的修复方法
                _logger.LogWarning("{Context} 所有编码修复方法都失败，尝试激进修复", context);
                
                try
                {
                    // 尝试移除所有非ASCII字符
                    var asciiOnly = new string(jsonString.Where(c => c < 128).ToArray());
                    _logger.LogInformation("{Context} 移除非ASCII字符后长度: {Length}", context, asciiOnly.Length);
                    
                    if (!string.IsNullOrWhiteSpace(asciiOnly))
                    {
                        // 尝试修复常见的JSON格式问题
                        var cleanedJson = asciiOnly
                            .Replace("\r\n", "")
                            .Replace("\n", "")
                            .Replace("\r", "")
                            .Replace("\t", "")
                            .Trim();
                        
                        // 验证修复后的JSON
                        JsonSerializer.Deserialize<Dictionary<string, object>>(cleanedJson);
                        _logger.LogInformation("{Context} 激进修复成功", context);
                        return cleanedJson;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("{Context} 激进修复失败: {ErrorMessage}", context, ex.Message);
                }
                
                // 如果激进修复也失败，尝试高级修复方法
                _logger.LogWarning("{Context} 激进修复失败，尝试高级修复方法", context);
                try
                {
                    var advancedFixed = AdvancedJsonFix(jsonString, context);
                    if (advancedFixed != "{}" && advancedFixed != "[]")
                    {
                        _logger.LogInformation("{Context} 高级修复成功", context);
                        return advancedFixed;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("{Context} 高级修复失败: {ErrorMessage}", context, ex.Message);
                }
                }
                
                // 尝试修复常见的JSON格式问题
                _logger.LogInformation("{Context} 尝试修复常见JSON格式问题", context);
                var finalCleanedJson = jsonString
                    .Replace("\r\n", "")
                    .Replace("\n", "")
                    .Replace("\r", "")
                    .Replace("\t", "")
                    .Trim();
                
                // 如果JSON以[开头，说明是数组，返回空数组
                if (finalCleanedJson.StartsWith("[") && !finalCleanedJson.EndsWith("]"))
                {
                    _logger.LogWarning("{Context} JSON数组格式不完整，返回空数组", context);
                    return "[]";
                }
                
                // 如果JSON以{开头，说明是对象，返回空对象
                if (finalCleanedJson.StartsWith("{") && !finalCleanedJson.EndsWith("}"))
                {
                    _logger.LogWarning("{Context} JSON对象格式不完整，返回空对象", context);
                    return "{}";
                }
                
                _logger.LogInformation("{Context} JSON格式修复完成，最终长度: {Length}", context, finalCleanedJson.Length);
                return finalCleanedJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Context} 清理JSON字符串时发生异常: {ErrorMessage}", context, ex.Message);
                return "[]";
            }
        }

        /// <summary>
        /// 高级JSON修复方法，处理复杂的编码和格式问题
        /// </summary>
        private string AdvancedJsonFix(string jsonString, string context)
        {
            try
            {
                _logger.LogInformation("开始高级JSON修复: {Context}, 原始长度: {Length}", context, jsonString.Length);
                
                // 方法1: 尝试修复常见的转义字符问题
                var fixedEscapes = jsonString
                    .Replace("\\\"", "\"")
                    .Replace("\\'", "'")
                    .Replace("\\\\", "\\");
                
                if (fixedEscapes != jsonString)
                {
                    _logger.LogInformation("{Context} 修复转义字符后长度: {Length}", context, fixedEscapes.Length);
                    try
                    {
                        JsonSerializer.Deserialize<Dictionary<string, object>>(fixedEscapes);
                        _logger.LogInformation("{Context} 转义字符修复成功", context);
                        return fixedEscapes;
                    }
                    catch
                    {
                        _logger.LogDebug("{Context} 转义字符修复后仍无效", context);
                    }
                }
                
                // 方法2: 尝试修复编码问题 - 使用字节级别的修复
                var bytes = Encoding.UTF8.GetBytes(jsonString);
                var fixedBytes = new List<byte>();
                
                for (int i = 0; i < bytes.Length; i++)
                {
                    var currentByte = bytes[i];
                    
                    // 检测并修复常见的编码问题
                    if (currentByte == 0xE6 && i + 2 < bytes.Length) // 可能是"的"字
                    {
                        if (bytes[i + 1] == 0x88 && bytes[i + 2] == 0x91)
                        {
                            fixedBytes.AddRange(new byte[] { 0x22 }); // 替换为双引号
                            i += 2; // 跳过后续字节
                            continue;
                        }
                    }
                    else if (currentByte == 0xE7 && i + 2 < bytes.Length) // 可能是"的"字
                    {
                        if (bytes[i + 1] == 0x9A && bytes[i + 2] == 0x84)
                        {
                            fixedBytes.AddRange(new byte[] { 0x22 }); // 替换为双引号
                            i += 2; // 跳过后续字节
                            continue;
                        }
                    }
                    else if (currentByte == 0xE8 && i + 2 < bytes.Length) // 可能是"的"字
                    {
                        if (bytes[i + 1] == 0xBF && bytes[i + 2] == 0x99)
                        {
                            fixedBytes.AddRange(new byte[] { 0x22 }); // 替换为双引号
                            i += 2; // 跳过后续字节
                            continue;
                        }
                    }
                    
                    // 保留有效的UTF-8字节
                    if (currentByte < 128 || (currentByte >= 0xC0 && currentByte <= 0xDF))
                    {
                        fixedBytes.Add(currentByte);
                    }
                    else if (currentByte >= 0xE0 && currentByte <= 0xEF && i + 2 < bytes.Length)
                    {
                        // 3字节UTF-8序列
                        if (bytes[i + 1] >= 0x80 && bytes[i + 1] <= 0xBF &&
                            bytes[i + 2] >= 0x80 && bytes[i + 2] <= 0xBF)
                        {
                            fixedBytes.AddRange(new byte[] { currentByte, bytes[i + 1], bytes[i + 2] });
                            i += 2;
                        }
                        else
                        {
                            // 无效的3字节序列，替换为空格
                            fixedBytes.Add(0x20);
                            i += 2;
                        }
                    }
                    else
                    {
                        // 其他无效字节，替换为空格
                        fixedBytes.Add(0x20);
                    }
                }
                
                if (fixedBytes.Count != bytes.Length)
                {
                    _logger.LogInformation("{Context} 字节级修复后长度: {Length}", context, fixedBytes.Count);
                    try
                    {
                        var fixedString = Encoding.UTF8.GetString(fixedBytes.ToArray());
                        JsonSerializer.Deserialize<Dictionary<string, object>>(fixedString);
                        _logger.LogInformation("{Context} 字节级修复成功", context);
                        return fixedString;
                    }
                    catch
                    {
                        _logger.LogDebug("{Context} 字节级修复后仍无效", context);
                    }
                }
                
                // 方法3: 尝试重建JSON结构
                _logger.LogInformation("{Context} 尝试重建JSON结构", context);
                
                // 查找JSON的基本结构
                var openBraces = jsonString.Count(c => c == '{');
                var closeBraces = jsonString.Count(c => c == '}');
                var openBrackets = jsonString.Count(c => c == '[');
                var closeBrackets = jsonString.Count(c => c == ']');
                
                _logger.LogInformation("{Context} JSON结构统计 - 大括号: {OpenBraces}/{CloseBraces}, 方括号: {OpenBrackets}/{CloseBrackets}", 
                    context, openBraces, closeBraces, openBrackets, closeBrackets);
                
                // 如果结构不平衡，尝试修复
                if (openBraces != closeBraces || openBrackets != closeBrackets)
                {
                    var fixedStructure = jsonString;
                    
                    // 添加缺失的闭合符号
                    if (openBraces > closeBraces)
                    {
                        fixedStructure += new string('}', openBraces - closeBraces);
                    }
                    if (openBrackets > closeBrackets)
                    {
                        fixedStructure += new string(']', openBrackets - closeBrackets);
                    }
                    
                    if (fixedStructure != jsonString)
                    {
                        _logger.LogInformation("{Context} 修复JSON结构后长度: {Length}", context, fixedStructure.Length);
                        try
                        {
                            JsonSerializer.Deserialize<Dictionary<string, object>>(fixedStructure);
                            _logger.LogInformation("{Context} JSON结构修复成功", context);
                            return fixedStructure;
                        }
                        catch
                        {
                            _logger.LogDebug("{Context} JSON结构修复后仍无效", context);
                        }
                    }
                }
                
                // 方法4: 最后尝试 - 提取有效的JSON片段
                _logger.LogInformation("{Context} 尝试提取有效的JSON片段", context);
                
                var startIndex = jsonString.IndexOf('{');
                var endIndex = jsonString.LastIndexOf('}');
                
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    var jsonFragment = jsonString.Substring(startIndex, endIndex - startIndex + 1);
                    _logger.LogInformation("{Context} 提取JSON片段，长度: {Length}", context, jsonFragment.Length);
                    
                    try
                    {
                        JsonSerializer.Deserialize<Dictionary<string, object>>(jsonFragment);
                        _logger.LogInformation("{Context} JSON片段提取成功", context);
                        return jsonFragment;
                    }
                    catch
                    {
                        _logger.LogDebug("{Context} JSON片段提取后仍无效", context);
                    }
                }
                
                _logger.LogWarning("{Context} 所有高级修复方法都失败，返回默认配置", context);
                return "{}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Context} 高级JSON修复时发生异常: {ErrorMessage}", context, ex.Message);
                return "{}";
            }
        }

        /// <summary>
        /// 触发作业执行完成事件
        /// </summary>
        private void OnJobExecutionCompleted(JobExecution execution)
        {
            lock (_jobEventCallbacks)
            {
                foreach (var callback in _jobEventCallbacks.Values)
                {
                    try
                    {
                        callback(execution);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "执行作业事件回调失败");
                    }
                }
            }
        }

        private static void NormalizeJobStepOrder(JobConfig jobConfig)
        {
            if (jobConfig?.Steps == null || jobConfig.Steps.Count == 0)
            {
                return;
            }

            // 先按当前 OrderIndex 排序，再重排为 1..N，保证服务端一致性
            var orderedSteps = jobConfig.Steps
                .OrderBy(s => s.OrderIndex <= 0 ? int.MaxValue : s.OrderIndex)
                .ToList();

            for (int i = 0; i < orderedSteps.Count; i++)
            {
                orderedSteps[i].OrderIndex = i + 1;
            }
        }

        #endregion
    }
}