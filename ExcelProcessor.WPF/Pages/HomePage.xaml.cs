using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using ExcelProcessor.Core.Services;
using ExcelProcessor.WPF.ViewModels;
using ExcelProcessor.WPF.Dialogs;
using ExcelProcessor.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq; // Added for .Any()
using System.Windows; // Added for Window.GetWindow

namespace ExcelProcessor.WPF.Pages
{
    public partial class HomePage : Page
    {
        private HomePageViewModel _viewModel;

        public HomePage()
        {
            InitializeComponent();
            InitializeViewModel();
            
            // 订阅页面生命周期事件
            Loaded += HomePage_Loaded;
        }

        private void InitializeViewModel()
        {
            try
            {
                // 从依赖注入容器获取服务
                var jobService = App.Services.GetRequiredService<IJobService>();
                var loggerFactory = App.Services.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<HomePageViewModel>();

                // 创建ViewModel
                _viewModel = new HomePageViewModel(jobService, logger);

                // 设置数据上下文
                DataContext = _viewModel;
            }
            catch (Exception ex)
            {
                // 如果依赖注入失败，显示错误信息
                Extensions.MessageBoxExtensions.Show($"初始化页面失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 页面加载完成事件
        /// </summary>
        private async void HomePage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                // 页面加载完成后刷新数据
                if (_viewModel != null)
                {
                    await _viewModel.RefreshManualJobsAsync();
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不显示给用户，避免影响用户体验
                System.Diagnostics.Debug.WriteLine($"刷新首页数据失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 手动执行作业卡片加载完成事件
        /// </summary>
        private void ManualJobCard_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Controls.ManualJobCard jobCard && 
                    jobCard.DataContext is JobConfig job)
                {
                    // 获取作业服务
                    var jobService = App.Services.GetRequiredService<IJobService>();
                    
                    // 设置作业数据
                    jobCard.SetJobData(job, jobService);
                    
                    // 订阅作业执行事件
                    jobCard.JobExecuted += async (s, executedJob) =>
                    {
                        // 刷新手动执行作业列表和最近执行任务
                        await _viewModel.RefreshManualJobsAsync();
                        await _viewModel.RefreshRecentTasksAsync();
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置作业卡片数据失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 查看任务详情按钮点击事件
        /// </summary>
        private async void ViewTaskDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is RecentTaskViewModel task)
                {
                    // 获取作业服务
                    var jobService = App.Services.GetRequiredService<IJobService>();
                    
                    // 打开作业执行历史对话框
                    var dialog = new JobExecutionHistoryDialog(jobService, task.JobId, task.JobName);
                    dialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"查看任务详情失败：{ex.Message}");
                Extensions.MessageBoxExtensions.Show($"查看任务详情失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 重新执行任务按钮点击事件
        /// </summary>
        private async void ReExecuteTask_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is RecentTaskViewModel task)
                {
                    // 获取作业服务
                    var jobService = App.Services.GetRequiredService<IJobService>();
                    
                    // 确认重新执行
                    var result = Extensions.MessageBoxExtensions.Show($"确定要重新执行作业 '{task.JobName}' 吗？", "确认重新执行", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        // 执行作业
                        var (success, message, executionId) = await jobService.ExecuteJobAsync(task.JobId);
                        
                        if (success)
                        {
                            Extensions.MessageBoxExtensions.Show($"作业 '{task.JobName}' 已开始执行", "执行成功", 
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // 刷新最近执行任务列表
                            await _viewModel.RefreshRecentTasksAsync();
                        }
                        else
                        {
                            Extensions.MessageBoxExtensions.Show($"执行作业失败：{message}", "执行失败", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重新执行任务失败：{ex.Message}");
                Extensions.MessageBoxExtensions.Show($"重新执行任务失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 