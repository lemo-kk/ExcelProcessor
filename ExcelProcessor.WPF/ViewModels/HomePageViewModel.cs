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

        public HomePageViewModel(IJobService jobService, ILogger<HomePageViewModel> logger)
        {
            _jobService = jobService;
            _logger = logger;
            _manualJobs = new ObservableCollection<JobConfig>();

            // 初始化数据
            _ = Task.Run(async () => await LoadManualJobsAsync());
        }

        #region 属性

        /// <summary>
        /// 手动执行的作业列表
        /// </summary>
        public ObservableCollection<JobConfig> ManualJobs => _manualJobs;

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
        /// 刷新手动执行作业
        /// </summary>
        public async Task RefreshManualJobsAsync()
        {
            await LoadManualJobsAsync();
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
} 