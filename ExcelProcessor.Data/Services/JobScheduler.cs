using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using ExcelProcessor.Data.Repositories;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// 作业调度器
    /// </summary>
    public class JobScheduler : IDisposable
    {
        private readonly IJobRepository _jobRepository;
        private readonly ILogger<JobScheduler> _logger;
        
        // 作业执行事件
        public event Func<string, Dictionary<string, object>?, string, Task<(bool success, string message, string? executionId)>>? JobExecutionRequested;
        private readonly ConcurrentDictionary<string, ScheduledJobInfo> _scheduledJobs = new();
        private readonly Timer _schedulerTimer;
        private readonly object _schedulerLock = new object();
        private bool _isRunning = false;
        private bool _isPaused = false;
        private DateTime? _lastRunTime = null;

        public JobScheduler(IJobRepository jobRepository, ILogger<JobScheduler> logger)
        {
            _jobRepository = jobRepository;
            _logger = logger;
            
            // 创建定时器，每分钟检查一次
            _schedulerTimer = new Timer(CheckScheduledJobs, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// 启动调度器
        /// </summary>
        public async Task<bool> StartAsync()
        {
            lock (_schedulerLock)
            {
                if (_isRunning)
                {
                    _logger.LogWarning("调度器已经在运行中");
                    return false;
                }

                _isRunning = true;
                _isPaused = false;
                _schedulerTimer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(1));
                
                _logger.LogInformation("作业调度器已启动");
            }

            return true;
        }

        /// <summary>
        /// 停止调度器
        /// </summary>
        public async Task<bool> StopAsync()
        {
            lock (_schedulerLock)
            {
                if (!_isRunning)
                {
                    _logger.LogWarning("调度器未在运行");
                    return false;
                }

                _isRunning = false;
                _isPaused = false;
                _schedulerTimer.Change(Timeout.Infinite, Timeout.Infinite);
                
                _logger.LogInformation("作业调度器已停止");
            }

            return true;
        }

        /// <summary>
        /// 暂停调度器
        /// </summary>
        public async Task<bool> PauseAsync()
        {
            lock (_schedulerLock)
            {
                if (!_isRunning)
                {
                    _logger.LogWarning("调度器未在运行");
                    return false;
                }

                if (_isPaused)
                {
                    _logger.LogWarning("调度器已经暂停");
                    return false;
                }

                _isPaused = true;
                _logger.LogInformation("作业调度器已暂停");
            }

            return true;
        }

        /// <summary>
        /// 恢复调度器
        /// </summary>
        public async Task<bool> ResumeAsync()
        {
            lock (_schedulerLock)
            {
                if (!_isRunning)
                {
                    _logger.LogWarning("调度器未在运行");
                    return false;
                }

                if (!_isPaused)
                {
                    _logger.LogWarning("调度器未暂停");
                    return false;
                }

                _isPaused = false;
                _logger.LogInformation("作业调度器已恢复");
            }

            return true;
        }

        /// <summary>
        /// 获取调度器状态
        /// </summary>
        public (bool isRunning, bool isPaused, DateTime? lastRunTime) GetStatus()
        {
            lock (_schedulerLock)
            {
                return (_isRunning, _isPaused, _lastRunTime);
            }
        }

        /// <summary>
        /// 添加定时作业
        /// </summary>
        public async Task<bool> AddScheduledJobAsync(string jobId, string cronExpression)
        {
            try
            {
                // 验证Cron表达式
                if (!CronExpression.TryParse(cronExpression, out var cron))
                {
                    _logger.LogError("无效的Cron表达式: {CronExpression}", cronExpression);
                    return false;
                }

                // 获取作业配置
                var jobConfig = await _jobRepository.GetByIdAsync(jobId);
                if (jobConfig == null)
                {
                    _logger.LogError("作业配置不存在: {JobId}", jobId);
                    return false;
                }

                var scheduledJob = new ScheduledJobInfo
                {
                    JobId = jobId,
                    JobName = jobConfig.Name,
                    CronExpression = cronExpression,
                    Cron = cron,
                    NextRunTime = cron.GetNextOccurrence(DateTime.Now),
                    IsEnabled = jobConfig.IsEnabled
                };

                _scheduledJobs[jobId] = scheduledJob;
                
                _logger.LogInformation("添加定时作业成功: {JobName} ({JobId}), 下次执行时间: {NextRunTime}", 
                    jobConfig.Name, jobId, scheduledJob.NextRunTime);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加定时作业失败: {JobId}", jobId);
                return false;
            }
        }

        /// <summary>
        /// 移除定时作业
        /// </summary>
        public async Task<bool> RemoveScheduledJobAsync(string jobId)
        {
            try
            {
                if (_scheduledJobs.TryRemove(jobId, out var removedJob))
                {
                    _logger.LogInformation("移除定时作业成功: {JobName} ({JobId})", removedJob.JobName, jobId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("定时作业不存在: {JobId}", jobId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除定时作业失败: {JobId}", jobId);
                return false;
            }
        }

        /// <summary>
        /// 获取定时作业列表
        /// </summary>
        public List<(string jobId, string jobName, string cronExpression, DateTime? nextRunTime, bool isEnabled)> GetScheduledJobs()
        {
            return _scheduledJobs.Values
                .Select(job => (job.JobId, job.JobName, job.CronExpression, job.NextRunTime, job.IsEnabled))
                .ToList();
        }

        /// <summary>
        /// 检查定时作业
        /// </summary>
        private async void CheckScheduledJobs(object? state)
        {
            if (_isPaused)
            {
                return;
            }

            try
            {
                _lastRunTime = DateTime.Now;
                var now = DateTime.Now;

                var jobsToExecute = new List<ScheduledJobInfo>();

                // 检查需要执行的作业
                foreach (var job in _scheduledJobs.Values)
                {
                    if (job.IsEnabled && job.NextRunTime.HasValue && job.NextRunTime.Value <= now)
                    {
                        jobsToExecute.Add(job);
                    }
                }

                // 执行到期的作业
                foreach (var job in jobsToExecute)
                {
                    await ExecuteScheduledJobAsync(job);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查定时作业时发生异常");
            }
        }

        /// <summary>
        /// 执行定时作业
        /// </summary>
        private async Task ExecuteScheduledJobAsync(ScheduledJobInfo scheduledJob)
        {
            try
            {
                _logger.LogInformation("执行定时作业: {JobName} ({JobId})", scheduledJob.JobName, scheduledJob.JobId);

                // 通过事件请求执行作业
                if (JobExecutionRequested != null)
                {
                    var result = await JobExecutionRequested(scheduledJob.JobId, null, "Scheduler");
                    
                    if (result.success)
                    {
                        _logger.LogInformation("定时作业执行成功: {JobName} ({JobId})", scheduledJob.JobName, scheduledJob.JobId);
                    }
                    else
                    {
                        _logger.LogError("定时作业执行失败: {JobName} ({JobId}) - {Message}", 
                            scheduledJob.JobName, scheduledJob.JobId, result.message);
                    }
                }
                else
                {
                    _logger.LogWarning("没有注册作业执行处理器，跳过作业执行: {JobName} ({JobId})", 
                        scheduledJob.JobName, scheduledJob.JobId);
                }

                // 计算下次执行时间
                scheduledJob.NextRunTime = scheduledJob.Cron.GetNextOccurrence(DateTime.Now);
                
                _logger.LogInformation("定时作业下次执行时间: {JobName} ({JobId}) - {NextRunTime}", 
                    scheduledJob.JobName, scheduledJob.JobId, scheduledJob.NextRunTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行定时作业异常: {JobName} ({JobId})", scheduledJob.JobName, scheduledJob.JobId);
                
                // 计算下次执行时间
                scheduledJob.NextRunTime = scheduledJob.Cron.GetNextOccurrence(DateTime.Now);
            }
        }

        /// <summary>
        /// 更新作业启用状态
        /// </summary>
        public void UpdateJobEnabledStatus(string jobId, bool isEnabled)
        {
            if (_scheduledJobs.TryGetValue(jobId, out var job))
            {
                job.IsEnabled = isEnabled;
                _logger.LogInformation("更新定时作业状态: {JobName} ({JobId}) - 启用: {IsEnabled}", 
                    job.JobName, jobId, isEnabled);
            }
        }

        /// <summary>
        /// 更新作业Cron表达式
        /// </summary>
        public async Task<bool> UpdateJobCronExpressionAsync(string jobId, string newCronExpression)
        {
            try
            {
                // 验证新的Cron表达式
                if (!CronExpression.TryParse(newCronExpression, out var newCron))
                {
                    _logger.LogError("无效的Cron表达式: {CronExpression}", newCronExpression);
                    return false;
                }

                if (_scheduledJobs.TryGetValue(jobId, out var job))
                {
                    job.CronExpression = newCronExpression;
                    job.Cron = newCron;
                    job.NextRunTime = newCron.GetNextOccurrence(DateTime.Now);
                    
                    _logger.LogInformation("更新定时作业Cron表达式: {JobName} ({JobId}) - {NewCronExpression}", 
                        job.JobName, jobId, newCronExpression);
                    
                    return true;
                }
                else
                {
                    _logger.LogWarning("定时作业不存在: {JobId}", jobId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新定时作业Cron表达式失败: {JobId}", jobId);
                return false;
            }
        }

        public void Dispose()
        {
            _schedulerTimer?.Dispose();
        }

        /// <summary>
        /// 定时作业信息
        /// </summary>
        private class ScheduledJobInfo
        {
            public string JobId { get; set; } = string.Empty;
            public string JobName { get; set; } = string.Empty;
            public string CronExpression { get; set; } = string.Empty;
            public CronExpression Cron { get; set; } = null!;
            public DateTime? NextRunTime { get; set; }
            public bool IsEnabled { get; set; }
        }
    }
} 