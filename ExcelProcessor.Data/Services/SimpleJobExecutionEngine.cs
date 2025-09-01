using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Core.Interfaces;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// 简化的作业执行引擎实现
    /// </summary>
    public class SimpleJobExecutionEngine : IJobExecutionEngine
    {
        private readonly ILogger<SimpleJobExecutionEngine> _logger;
        private readonly ConcurrentDictionary<string, JobExecutionContext> _executionContexts = new();
        private readonly ConcurrentDictionary<string, ExecutionProgress> _executionProgress = new();
        private readonly Dictionary<string, Action<ExecutionEvent>> _executionEventCallbacks = new();
        private readonly object _eventLock = new object();

        public SimpleJobExecutionEngine(ILogger<SimpleJobExecutionEngine> logger)
        {
            _logger = logger;
        }

        public async Task<JobExecutionResult> ExecuteJobAsync(JobConfig jobConfig, Dictionary<string, object>? parameters = null, string executedBy = "", CancellationToken cancellationToken = default)
        {
            var executionId = Guid.NewGuid().ToString();
            var result = new JobExecutionResult
            {
                ExecutionId = executionId,
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInformation("开始执行作业: {JobName} ({JobId})", jobConfig.Name, jobConfig.Id);

                // 创建执行上下文
                var context = CreateExecutionContext(jobConfig, parameters, executedBy);
                context.CancellationToken = cancellationToken;
                _executionContexts[executionId] = context;

                // 触发作业开始事件
                TriggerExecutionEvent(executionId, ExecutionEventType.JobStarted, "作业开始执行");

                // 验证作业配置
                var validation = await ValidateJobConfigAsync(jobConfig);
                if (!validation.IsValid)
                {
                    var errorMessage = string.Join("; ", validation.Errors);
                    result.IsSuccess = false;
                    result.ErrorMessage = errorMessage;
                    result.ErrorDetails = errorMessage;
                    TriggerExecutionEvent(executionId, ExecutionEventType.JobFailed, errorMessage);
                    return result;
                }

                // 执行作业步骤
                if (jobConfig.Steps != null && jobConfig.Steps.Any())
                {
                    var stepResults = new List<StepExecutionResult>();
                    var totalSteps = jobConfig.Steps.Count;

                    for (int i = 0; i < totalSteps; i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            result.IsSuccess = false;
                            result.ErrorMessage = "作业执行被取消";
                            TriggerExecutionEvent(executionId, ExecutionEventType.JobCancelled, "作业执行被取消");
                            break;
                        }

                        var step = jobConfig.Steps[i];
                        context.CurrentStepIndex = i;
                        context.Progress = (i * 100) / totalSteps;

                        // 触发步骤开始事件
                        TriggerExecutionEvent(executionId, ExecutionEventType.StepStarted, $"开始执行步骤: {step.Name}");

                        var stepResult = await ExecuteStepAsync(step, context, cancellationToken);
                        stepResults.Add(stepResult);

                        if (!stepResult.IsSuccess)
                        {
                            result.IsSuccess = false;
                            result.ErrorMessage = $"步骤 '{step.Name}' 执行失败: {stepResult.ErrorMessage}";
                            result.ErrorDetails = stepResult.ErrorDetails;
                            TriggerExecutionEvent(executionId, ExecutionEventType.JobFailed, result.ErrorMessage);
                            break;
                        }

                        // 触发步骤完成事件
                        TriggerExecutionEvent(executionId, ExecutionEventType.StepCompleted, $"步骤完成: {step.Name}");
                    }

                    result.StepResults = stepResults;
                }

                result.IsSuccess = true;
                result.EndTime = DateTime.Now;
                result.DurationSeconds = (result.EndTime.Value - result.StartTime).TotalSeconds;

                TriggerExecutionEvent(executionId, ExecutionEventType.JobCompleted, "作业执行完成");
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.ErrorDetails = ex.ToString();
                TriggerExecutionEvent(executionId, ExecutionEventType.JobFailed, ex.Message);
            }
            finally
            {
                CleanupExecutionContext(executionId);
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

                // 这里应该根据步骤类型执行具体的逻辑
                // 暂时只是模拟执行
                await Task.Delay(100, cancellationToken);

                result.IsSuccess = true;
                result.EndTime = DateTime.Now;
                result.DurationSeconds = (result.EndTime.Value - result.StartTime).TotalSeconds;
                result.ExecutionLogs.Add($"步骤 {step.Name} 执行成功");
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.ErrorDetails = ex.ToString();
                result.ExecutionLogs.Add($"步骤 {step.Name} 执行失败: {ex.Message}");
            }

            return result;
        }

        public async Task<ExcelProcessor.Core.Services.ValidationResult> ValidateJobConfigAsync(JobConfig jobConfig)
        {
            var result = new ExcelProcessor.Core.Services.ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(jobConfig.Name))
            {
                result.IsValid = false;
                result.Errors.Add("作业名称不能为空");
            }

            if (jobConfig.Steps == null || !jobConfig.Steps.Any())
            {
                result.IsValid = false;
                result.Errors.Add("作业必须包含至少一个步骤");
            }

            return await Task.FromResult(result);
        }

        public async Task<ExcelProcessor.Core.Services.ValidationResult> ValidateStepConfigAsync(JobStep step)
        {
            var result = new ExcelProcessor.Core.Services.ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(step.Name))
            {
                result.IsValid = false;
                result.Errors.Add("步骤名称不能为空");
            }

            return await Task.FromResult(result);
        }

        public JobExecutionContext CreateExecutionContext(JobConfig jobConfig, Dictionary<string, object>? parameters = null, string executedBy = "")
        {
            return new JobExecutionContext
            {
                JobConfig = jobConfig,
                Parameters = parameters ?? new Dictionary<string, object>(),
                ExecutedBy = executedBy
            };
        }

        public JobExecutionContext? GetExecutionContext(string executionId)
        {
            _executionContexts.TryGetValue(executionId, out var context);
            return context;
        }

        public void CleanupExecutionContext(string executionId)
        {
            _executionContexts.TryRemove(executionId, out _);
            _executionProgress.TryRemove(executionId, out _);
        }

        public async Task<ExecutionProgress> GetExecutionProgressAsync(string executionId)
        {
            if (_executionProgress.TryGetValue(executionId, out var progress))
            {
                return await Task.FromResult(progress);
            }

            return await Task.FromResult(new ExecutionProgress());
        }

        public async Task UpdateExecutionProgressAsync(string executionId, ExecutionProgress progress)
        {
            _executionProgress[executionId] = progress;
            await Task.CompletedTask;
        }

        public void SubscribeToExecutionEvents(Action<ExecutionEvent> callback)
        {
            lock (_eventLock)
            {
                var key = callback.GetHashCode().ToString();
                _executionEventCallbacks[key] = callback;
            }
        }

        public void UnsubscribeFromExecutionEvents(Action<ExecutionEvent> callback)
        {
            lock (_eventLock)
            {
                var key = callback.GetHashCode().ToString();
                _executionEventCallbacks.Remove(key);
            }
        }

        public async Task<ErrorHandlingResult> HandleExecutionErrorAsync(JobExecutionContext context, Exception error, JobStep? step = null)
        {
            var result = new ErrorHandlingResult
            {
                IsHandled = true,
                ShouldContinue = false,
                Message = $"处理执行错误: {error.Message}",
                RetryCount = 0
            };

            _logger.LogError(error, "作业执行错误: {JobName}, 步骤: {StepName}", 
                context.JobConfig.Name, step?.Name ?? "未知");

            return await Task.FromResult(result);
        }

        public async Task<StepExecutionResult> RetryStepExecutionAsync(JobStep step, JobExecutionContext context, int retryCount, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("重试步骤执行: {StepName}, 重试次数: {RetryCount}", step.Name, retryCount);
            return await ExecuteStepAsync(step, context, cancellationToken);
        }

        private void TriggerExecutionEvent(string executionId, ExecutionEventType eventType, string message)
        {
            var executionEvent = new ExecutionEvent
            {
                ExecutionId = executionId,
                EventType = eventType,
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
    }
} 