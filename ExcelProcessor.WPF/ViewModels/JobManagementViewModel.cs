using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Models;
using ExcelProcessor.WPF.Dialogs;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.WPF.ViewModels
{
    /// <summary>
    /// 作业管理页面ViewModel
    /// </summary>
    public class JobManagementViewModel : INotifyPropertyChanged
    {
        private readonly IJobService _jobService;
        private readonly ILogger<JobManagementViewModel> _logger;
        private readonly ObservableCollection<JobConfig> _allJobs;
        private readonly ObservableCollection<JobConfig> _filteredJobs;

        public JobManagementViewModel(IJobService jobService, ILogger<JobManagementViewModel> logger)
        {
            _jobService = jobService;
            _logger = logger;
            _allJobs = new ObservableCollection<JobConfig>();
            _filteredJobs = new ObservableCollection<JobConfig>();

            // 初始化命令
            RefreshCommand = new RelayCommand(async () => await RefreshJobsAsync());
            CreateJobCommand = new RelayCommand(() => { }); // 占位符，实际在Page中处理
            EditJobCommand = new RelayCommand<JobConfig>((job) => { }); // 占位符，实际在Page中处理
            DeleteJobCommand = new RelayCommand<JobConfig>(async (job) => await DeleteJobAsync(job));
            ExecuteJobCommand = new RelayCommand<JobConfig>(async (job) => await ExecuteJobAsync(job));
            PauseJobCommand = new RelayCommand<JobConfig>(async (job) => await PauseJobAsync(job));
            ResumeJobCommand = new RelayCommand<JobConfig>(async (job) => await ResumeJobAsync(job));
            ClearFiltersCommand = new RelayCommand(ClearFilters);

            // 初始化数据
            _ = Task.Run(async () => await LoadJobsAsync());
        }

        #region 属性

        public ObservableCollection<JobConfig> FilteredJobs => _filteredJobs;

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = Task.Run(async () => await ApplyFiltersAsync());
                }
            }
        }

        private string _selectedStatusFilter = "全部";
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    _ = Task.Run(async () => await ApplyFiltersAsync());
                }
            }
        }

        private string _selectedTypeFilter = "全部";
        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set
            {
                if (SetProperty(ref _selectedTypeFilter, value))
                {
                    _ = Task.Run(async () => await ApplyFiltersAsync());
                }
            }
        }

        private string _selectedPriorityFilter = "全部";
        public string SelectedPriorityFilter
        {
            get => _selectedPriorityFilter;
            set
            {
                if (SetProperty(ref _selectedPriorityFilter, value))
                {
                    _ = Task.Run(async () => await ApplyFiltersAsync());
                }
            }
        }

        private string _selectedExecutionModeFilter = "全部";
        public string SelectedExecutionModeFilter
        {
            get => _selectedExecutionModeFilter;
            set
            {
                if (SetProperty(ref _selectedExecutionModeFilter, value))
                {
                    _ = Task.Run(async () => await ApplyFiltersAsync());
                }
            }
        }

        private int _totalJobs;
        public int TotalJobs
        {
            get => _totalJobs;
            set => SetProperty(ref _totalJobs, value);
        }

        private int _runningJobs;
        public int RunningJobs
        {
            get => _runningJobs;
            set => SetProperty(ref _runningJobs, value);
        }

        private int _completedJobs;
        public int CompletedJobs
        {
            get => _completedJobs;
            set => SetProperty(ref _completedJobs, value);
        }

        private int _failedJobs;
        public int FailedJobs
        {
            get => _failedJobs;
            set => SetProperty(ref _failedJobs, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region 命令

        public ICommand RefreshCommand { get; }
        public ICommand CreateJobCommand { get; }
        public ICommand EditJobCommand { get; }
        public ICommand DeleteJobCommand { get; }
        public ICommand ExecuteJobCommand { get; }
        public ICommand PauseJobCommand { get; }
        public ICommand ResumeJobCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        #endregion

        #region 方法

        private async Task LoadJobsAsync()
        {
            try
            {
                IsLoading = true;
                _logger.LogInformation("开始加载作业数据");

                var jobs = await _jobService.GetAllJobsAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _allJobs.Clear();
                    foreach (var job in jobs)
                    {
                        _allJobs.Add(job);
                    }
                });

                await ApplyFiltersAsync();
                UpdateStatistics();
                
                _logger.LogInformation("作业数据加载完成，共 {Count} 个作业", jobs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载作业数据失败");
                MessageBox.Show($"加载作业数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private Task ApplyFiltersAsync()
        {
            try
            {
                var filteredJobs = _allJobs.AsEnumerable();

                // 搜索过滤
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchText = SearchText.ToLower();
                    filteredJobs = filteredJobs.Where(job =>
                        job.Name.ToLower().Contains(searchText) ||
                        job.Description.ToLower().Contains(searchText) ||
                        job.Type.ToLower().Contains(searchText)
                    );
                }

                // 状态过滤
                if (SelectedStatusFilter != "全部")
                {
                    var status = ParseJobStatus(SelectedStatusFilter);
                    if (status.HasValue)
                    {
                        filteredJobs = filteredJobs.Where(job => job.Status == status.Value);
                    }
                }

                // 类型过滤
                if (SelectedTypeFilter != "全部")
                {
                    filteredJobs = filteredJobs.Where(job => job.Type == SelectedTypeFilter);
                }

                // 优先级过滤
                if (SelectedPriorityFilter != "全部")
                {
                    var priority = ParseJobPriority(SelectedPriorityFilter);
                    if (priority.HasValue)
                    {
                        filteredJobs = filteredJobs.Where(job => job.Priority == priority.Value);
                    }
                }

                // 执行模式过滤
                if (SelectedExecutionModeFilter != "全部")
                {
                    var executionMode = ParseExecutionMode(SelectedExecutionModeFilter);
                    if (executionMode.HasValue)
                    {
                        filteredJobs = filteredJobs.Where(job => job.ExecutionMode == executionMode.Value);
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _filteredJobs.Clear();
                    foreach (var job in filteredJobs)
                    {
                        _filteredJobs.Add(job);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用过滤器失败");
            }

            return Task.CompletedTask;
        }

        private void UpdateStatistics()
        {
            TotalJobs = _allJobs.Count;
            RunningJobs = _allJobs.Count(job => job.Status == JobStatus.Running);
            CompletedJobs = _allJobs.Count(job => job.Status == JobStatus.Completed);
            FailedJobs = _allJobs.Count(job => job.Status == JobStatus.Failed);
        }

        private async Task RefreshJobsAsync()
        {
            await LoadJobsAsync();
        }

        public async Task CreateJobAsync(JobConfig jobConfig)
        {
            try
            {
                if (jobConfig == null) return;
                
                // 调用服务创建作业
                var (success, message) = await _jobService.CreateJobAsync(jobConfig);
                
                if (success)
                {
                    MessageBox.Show("作业创建成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadJobsAsync(); // 重新加载作业列表
                }
                else
                {
                    MessageBox.Show($"创建失败：{message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建作业失败");
                MessageBox.Show($"创建作业失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task EditJobAsync(JobConfig updatedJob)
        {
            try
            {
                if (updatedJob == null) return;
                
                // 调用服务更新作业
                var (success, message) = await _jobService.UpdateJobAsync(updatedJob);
                
                if (success)
                {
                    MessageBox.Show("作业更新成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadJobsAsync(); // 重新加载作业列表
                }
                else
                {
                    MessageBox.Show($"更新失败：{message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "编辑作业失败");
                MessageBox.Show($"编辑作业失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteJobAsync(JobConfig job)
        {
            try
            {
                if (job == null) return;

                var result = MessageBox.Show(
                    $"确定要删除作业 '{job.Name}' 吗？此操作不可恢复。",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var (success, message) = await _jobService.DeleteJobAsync(job.Id);
                    if (success)
                    {
                        MessageBox.Show("作业删除成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadJobsAsync();
                    }
                    else
                    {
                        MessageBox.Show($"删除失败：{message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除作业失败");
                MessageBox.Show($"删除作业失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteJobAsync(JobConfig job)
        {
            try
            {
                if (job == null) return;

                var (success, message, executionId) = await _jobService.ExecuteJobAsync(job.Id);
                if (success)
                {
                    MessageBox.Show($"作业 '{job.Name}' 开始执行", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadJobsAsync();
                }
                else
                {
                    MessageBox.Show($"执行失败：{message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行作业失败");
                MessageBox.Show($"执行作业失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Task PauseJobAsync(JobConfig job)
        {
            try
            {
                if (job == null) return Task.CompletedTask;

                // TODO: 实现暂停作业功能
                MessageBox.Show($"暂停作业：{job.Name}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "暂停作业失败");
                MessageBox.Show($"暂停作业失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return Task.CompletedTask;
        }

        private Task ResumeJobAsync(JobConfig job)
        {
            try
            {
                if (job == null) return Task.CompletedTask;

                // TODO: 实现恢复作业功能
                MessageBox.Show($"恢复作业：{job.Name}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复作业失败");
                MessageBox.Show($"恢复作业失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return Task.CompletedTask;
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedStatusFilter = "全部";
            SelectedTypeFilter = "全部";
            SelectedPriorityFilter = "全部";
            SelectedExecutionModeFilter = "全部";
        }

        private JobStatus? ParseJobStatus(string statusText)
        {
            return statusText switch
            {
                "运行中" => JobStatus.Running,
                "已完成" => JobStatus.Completed,
                "失败" => JobStatus.Failed,
                "暂停" => JobStatus.Paused,
                "待执行" => JobStatus.Pending,
                _ => null
            };
        }

        private JobPriority? ParseJobPriority(string priorityText)
        {
            return priorityText switch
            {
                "低" => JobPriority.Low,
                "普通" => JobPriority.Normal,
                "高" => JobPriority.High,
                "紧急" => JobPriority.Urgent,
                _ => null
            };
        }

        private ExecutionMode? ParseExecutionMode(string executionModeText)
        {
            return executionModeText switch
            {
                "手动执行" => ExecutionMode.Manual,
                "调度执行" => ExecutionMode.Scheduled,
                "触发执行" => ExecutionMode.Triggered,
                "事件驱动" => ExecutionMode.EventDriven,
                _ => null
            };
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    /// <summary>
    /// 简单的中继命令实现
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();
    }

    /// <summary>
    /// 带参数的中继命令实现
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;

        public void Execute(object parameter) => _execute((T)parameter);
    }
} 