using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.WPF.ViewModels
{
    /// <summary>
    /// 首页ViewModel
    /// </summary>
    public class HomePageViewModel : INotifyPropertyChanged
    {
        private readonly IJobService _jobService;
        private readonly ILogger<HomePageViewModel> _logger;
        private readonly ObservableCollection<JobConfig> _manualJobs;
        private readonly ObservableCollection<RecentTaskViewModel> _recentTasks;

        public HomePageViewModel(IJobService jobService, ILogger<HomePageViewModel> logger)
        {
            _jobService = jobService;
            _logger = logger;
            _manualJobs = new ObservableCollection<JobConfig>();
            _recentTasks = new ObservableCollection<RecentTaskViewModel>();

            // 初始化数据
            _ = Task.Run(async () => await LoadManualJobsAsync());
            _ = Task.Run(async () => await LoadRecentTasksAsync());
        }

        #region 属性

        /// <summary>
        /// 手动执行的作业列表
        /// </summary>
        public ObservableCollection<JobConfig> ManualJobs => _manualJobs;

        /// <summary>
        /// 最近执行任务列表
        /// </summary>
        public ObservableCollection<RecentTaskViewModel> RecentTasks => _recentTasks;

        /// <summary>
        /// 手动执行作业数量
        /// </summary>
        private int _manualJobsCount;
        public int ManualJobsCount
        {
            get => _manualJobsCount;
            private set => SetProperty(ref _manualJobsCount, value);
        }

        /// <summary>
        /// 最近执行任务数量
        /// </summary>
        private int _recentTasksCount;
        public int RecentTasksCount
        {
            get => _recentTasksCount;
            private set => SetProperty(ref _recentTasksCount, value);
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region 方法

        /// <summary>
        /// 检查作业是否包含Excel导入类型的步骤
        /// </summary>
        /// <param name="job">作业配置</param>
        /// <returns>是否包含Excel导入步骤</returns>
        public bool HasExcelImportSteps(JobConfig job)
        {
            try
            {
                if (job?.Steps == null || !job.Steps.Any())
                    return false;

                return job.Steps.Any(step => step.Type == StepType.ExcelImport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查作业Excel导入步骤时发生错误: {JobName}", job?.Name);
                return false;
            }
        }

        /// <summary>
        /// 加载手动执行的作业
        /// </summary>
        private async Task LoadManualJobsAsync()
        {
            try
            {
                IsLoading = true;
                _logger.LogInformation("开始加载手动执行作业");

                var allJobs = await _jobService.GetAllJobsAsync();
                
                // 筛选手动执行的作业
                var manualJobs = allJobs.Where(job => job.ExecutionMode == ExecutionMode.Manual).ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _manualJobs.Clear();
                    foreach (var job in manualJobs)
                    {
                        _manualJobs.Add(job);
                    }
                    // 更新作业数量属性，触发UI通知
                    ManualJobsCount = _manualJobs.Count;
                });

                _logger.LogInformation("手动执行作业加载完成，共 {Count} 个作业", manualJobs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载手动执行作业失败");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 加载最近执行的任务
        /// </summary>
        private async Task LoadRecentTasksAsync()
        {
            try
            {
                _logger.LogInformation("开始加载最近执行任务");

                // 获取所有作业
                var allJobs = await _jobService.GetAllJobsAsync();
                var recentTasks = new List<RecentTaskViewModel>();

                // 为每个作业获取最新的执行记录
                foreach (var job in allJobs)
                {
                    try
                    {
                        var latestExecution = await _jobService.GetLatestJobExecutionAsync(job.Id);
                        if (latestExecution != null)
                        {
                            recentTasks.Add(new RecentTaskViewModel
                            {
                                JobId = job.Id,
                                JobName = job.Name,
                                ExecuteTime = latestExecution.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                                Status = GetStatusDisplayText(latestExecution.Status),
                                StatusColor = GetStatusColor(latestExecution.Status),
                                ExecutionId = latestExecution.Id
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "获取作业 {JobName} 的最新执行记录失败", job.Name);
                    }
                }

                // 按执行时间排序，取最近的前10个
                var sortedTasks = recentTasks
                    .OrderByDescending(t => DateTime.Parse(t.ExecuteTime))
                    .Take(10)
                    .ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _recentTasks.Clear();
                    foreach (var task in sortedTasks)
                    {
                        _recentTasks.Add(task);
                    }
                    RecentTasksCount = _recentTasks.Count;
                });

                _logger.LogInformation("最近执行任务加载完成，共 {Count} 个任务", sortedTasks.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载最近执行任务失败");
            }
        }

        /// <summary>
        /// 获取状态显示文本
        /// </summary>
        private string GetStatusDisplayText(JobStatus status)
        {
            return status switch
            {
                JobStatus.Pending => "等待中",
                JobStatus.Running => "执行中",
                JobStatus.Completed => "已完成",
                JobStatus.Failed => "执行失败",
                JobStatus.Cancelled => "已取消",
                JobStatus.Paused => "已暂停",
                _ => "未知状态"
            };
        }

        /// <summary>
        /// 获取状态颜色
        /// </summary>
        private string GetStatusColor(JobStatus status)
        {
            return status switch
            {
                JobStatus.Pending => "#FFA500", // 橙色
                JobStatus.Running => "#2196F3", // 蓝色
                JobStatus.Completed => "#4CAF50", // 绿色
                JobStatus.Failed => "#F44336", // 红色
                JobStatus.Cancelled => "#9E9E9E", // 灰色
                JobStatus.Paused => "#FF9800", // 橙色
                _ => "#9E9E9E" // 默认灰色
            };
        }

        /// <summary>
        /// 刷新手动执行作业
        /// </summary>
        public async Task RefreshManualJobsAsync()
        {
            await LoadManualJobsAsync();
        }

        /// <summary>
        /// 刷新最近执行任务
        /// </summary>
        public async Task RefreshRecentTasksAsync()
        {
            await LoadRecentTasksAsync();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    /// <summary>
    /// 最近执行任务ViewModel
    /// </summary>
    public class RecentTaskViewModel : INotifyPropertyChanged
    {
        private string _jobId = string.Empty;
        private string _jobName = string.Empty;
        private string _executeTime = string.Empty;
        private string _status = string.Empty;
        private string _statusColor = string.Empty;
        private string _executionId = string.Empty;

        /// <summary>
        /// 作业ID
        /// </summary>
        public string JobId
        {
            get => _jobId;
            set => SetProperty(ref _jobId, value);
        }

        /// <summary>
        /// 作业名称
        /// </summary>
        public string JobName
        {
            get => _jobName;
            set => SetProperty(ref _jobName, value);
        }

        /// <summary>
        /// 执行时间
        /// </summary>
        public string ExecuteTime
        {
            get => _executeTime;
            set => SetProperty(ref _executeTime, value);
        }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// 状态颜色
        /// </summary>
        public string StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        /// <summary>
        /// 执行ID
        /// </summary>
        public string ExecutionId
        {
            get => _executionId;
            set => SetProperty(ref _executionId, value);
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
} 