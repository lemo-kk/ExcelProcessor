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
using System.Timers;

namespace ExcelProcessor.WPF.ViewModels
{
	/// <summary>
	/// 作业管理页面ViewModel
	/// </summary>
	public class JobManagementViewModel : INotifyPropertyChanged, IDisposable
	{
		private readonly IJobService _jobService;
		private readonly ILogger<JobManagementViewModel> _logger;
		private readonly ObservableCollection<JobConfig> _allJobs;
		private readonly ObservableCollection<JobConfig> _filteredJobs;
		private readonly Dictionary<string, string> _jobIdToExecutionId = new Dictionary<string, string>();
		private readonly Timer _progressTimer;

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

			// 启动进度轮询
			_progressTimer = new Timer(2000) { AutoReset = true, Enabled = true };
			_progressTimer.Elapsed += async (s, e) => await UpdateRunningJobsProgressSafeAsync();
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
					var successDialog = new SuccessDialog("成功", "作业创建成功", "确定");
					successDialog.ShowDialog();
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
					var successDialog = new SuccessDialog("成功", "作业更新成功", "确定");
					successDialog.ShowDialog();
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
						var successDialog = new SuccessDialog("成功", "作业删除成功", "确定");
						successDialog.ShowDialog();
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
					// 记录执行ID并更新状态
					if (!string.IsNullOrWhiteSpace(executionId))
					{
						lock (_jobIdToExecutionId)
						{
							_jobIdToExecutionId[job.Id] = executionId;
						}
						Application.Current.Dispatcher.Invoke(() =>
						{
							job.Status = JobStatus.Running;
							job.Progress = 0;
							RefreshJobInCollections(job);
						});
					}
					var successDialog = new SuccessDialog("成功", $"作业 '{job.Name}' 开始执行", "确定");
					successDialog.ShowDialog();
					// 保持现有刷新逻辑，确保列表与统计同步
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

		private async Task PauseJobAsync(JobConfig job)
		{
			try
			{
				if (job == null) return;

				var executionId = await GetExecutionIdForJobAsync(job.Id);
				if (string.IsNullOrWhiteSpace(executionId))
				{
					MessageBox.Show("当前作业没有运行实例，无法暂停", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
					return;
				}

				var (success, message) = await _jobService.PauseJobExecutionAsync(executionId);
				if (success)
				{
					Application.Current.Dispatcher.Invoke(() =>
					{
						job.Status = JobStatus.Paused;
						RefreshJobInCollections(job);
					});
					var successDialog = new SuccessDialog("提示", $"已暂停作业：{job.Name}", "确定");
					successDialog.ShowDialog();
				}
				else
				{
					MessageBox.Show($"暂停失败：{message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "暂停作业失败");
				MessageBox.Show($"暂停作业失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private async Task ResumeJobAsync(JobConfig job)
		{
			try
			{
				if (job == null) return;

				var executionId = await GetExecutionIdForJobAsync(job.Id);
				if (string.IsNullOrWhiteSpace(executionId))
				{
					MessageBox.Show("当前作业没有可恢复的执行实例", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
					return;
				}

				var (success, message) = await _jobService.ResumeJobExecutionAsync(executionId);
				if (success)
				{
					Application.Current.Dispatcher.Invoke(() =>
					{
						job.Status = JobStatus.Running;
						RefreshJobInCollections(job);
					});
					var successDialog = new SuccessDialog("提示", $"已恢复作业：{job.Name}", "确定");
					successDialog.ShowDialog();
				}
				else
				{
					MessageBox.Show($"恢复失败：{message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "恢复作业失败");
				MessageBox.Show($"恢复作业失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
			}
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

		private async Task UpdateRunningJobsProgressSafeAsync()
		{
			try
			{
				await UpdateRunningJobsProgressAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "更新作业进度失败");
			}
		}

		private async Task UpdateRunningJobsProgressAsync()
		{
			try
			{
				// 获取运行中的执行记录并更新映射
				var runningExecutions = await _jobService.GetRunningJobsAsync();
				var map = runningExecutions
					.GroupBy(e => e.JobId)
					.ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.StartTime).First().Id);

				lock (_jobIdToExecutionId)
				{
					_jobIdToExecutionId.Clear();
					foreach (var kv in map)
					{
						_jobIdToExecutionId[kv.Key] = kv.Value;
					}
				}

				// 更新每个运行中作业的进度
				foreach (var exec in runningExecutions)
				{
					var job = _allJobs.FirstOrDefault(j => j.Id == exec.JobId);
					if (job == null) continue;
					var progress = await _jobService.GetJobProgressAsync(exec.Id);
					Application.Current.Dispatcher.Invoke(() =>
					{
						job.Status = JobStatus.Running;
						job.Progress = progress.progress;
						RefreshJobInCollections(job);
					});
				}

				// 检查并更新已完成或失败的作业状态
				await UpdateCompletedJobsStatusAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "更新作业进度失败");
			}
		}

		private async Task UpdateCompletedJobsStatusAsync()
		{
			try
			{
				// 创建作业列表的副本，避免在遍历时修改集合
				var jobsToUpdate = _allJobs.ToList();
				
				// 获取所有作业的最新执行状态
				foreach (var job in jobsToUpdate)
				{
					// 获取作业的最新执行记录
					var latestExecution = await _jobService.GetLatestJobExecutionAsync(job.Id);
					if (latestExecution != null)
					{
						// 如果作业状态与最新执行状态不一致，则更新
						if (job.Status != latestExecution.Status)
						{
							Application.Current.Dispatcher.Invoke(() =>
							{
								job.Status = latestExecution.Status;
								// 如果作业已完成或失败，重置进度
								if (latestExecution.Status == JobStatus.Completed || 
									latestExecution.Status == JobStatus.Failed)
								{
									job.Progress = 100;
								}
								RefreshJobInCollections(job);
							});
						}
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "更新已完成作业状态失败");
			}
		}

		private async Task<string?> GetExecutionIdForJobAsync(string jobId)
		{
			lock (_jobIdToExecutionId)
			{
				if (_jobIdToExecutionId.TryGetValue(jobId, out var id))
				{
					return id;
				}
			}

			// 刷新一次映射再尝试
			var runningExecutions = await _jobService.GetRunningJobsAsync();
			var exec = runningExecutions.OrderByDescending(e => e.StartTime).FirstOrDefault(e => e.JobId == jobId);
			if (exec != null)
			{
				lock (_jobIdToExecutionId)
				{
					_jobIdToExecutionId[jobId] = exec.Id;
				}
				return exec.Id;
			}

			return null;
		}

		private void RefreshJobInCollections(JobConfig job)
		{
			// 通过替换集合中的元素来触发UI刷新
			var idxAll = _allJobs.IndexOf(job);
			if (idxAll >= 0)
			{
				_allJobs.RemoveAt(idxAll);
				_allJobs.Insert(idxAll, job);
			}

			var idxFiltered = _filteredJobs.IndexOf(job);
			if (idxFiltered >= 0)
			{
				_filteredJobs.RemoveAt(idxFiltered);
				_filteredJobs.Insert(idxFiltered, job);
			}

			UpdateStatistics();
		}

		#endregion

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#region IDisposable Implementation

		private bool _disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed && disposing)
			{
				try
				{
					_logger?.LogInformation("JobManagementViewModel正在释放资源...");
					
					// 停止定时器
					if (_progressTimer != null)
					{
						_progressTimer.Stop();
						_progressTimer.Dispose();
						_logger?.LogInformation("进度定时器已停止");
					}
					
					_logger?.LogInformation("JobManagementViewModel资源释放完成");
				}
				catch (Exception ex)
				{
					_logger?.LogError(ex, "释放JobManagementViewModel资源时发生错误");
				}
				finally
				{
					_disposed = true;
				}
			}
		}

		~JobManagementViewModel()
		{
			Dispose(false);
		}

		#endregion

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