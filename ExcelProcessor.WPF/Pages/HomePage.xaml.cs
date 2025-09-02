using System;
using System.Windows.Controls;
using ExcelProcessor.Core.Services;
using ExcelProcessor.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows.Input;
using System.Windows.Media;

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
                System.Windows.MessageBox.Show($"初始化页面失败：{ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
        /// 手动执行作业卡片点击事件
        /// </summary>
        private async void ManualJobCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border border && border.Tag is string jobId)
                {
                    // 获取作业服务
                    var jobService = App.Services.GetRequiredService<IJobService>();
                    
                    // 执行作业
                    var (success, message, executionId) = await jobService.ExecuteJobAsync(jobId);
                    
                    if (success)
                    {
                        System.Windows.MessageBox.Show($"作业开始执行：{message}", "执行成功", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        
                        // 刷新手动执行作业列表
                        await _viewModel.RefreshManualJobsAsync();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show($"执行失败：{message}", "执行失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行作业时发生错误：{ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
} 