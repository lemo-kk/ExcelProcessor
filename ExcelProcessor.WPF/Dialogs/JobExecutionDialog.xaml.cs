using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Models;
using Microsoft.Extensions.DependencyInjection; // Added for App.Services

namespace ExcelProcessor.WPF.Dialogs
{
    /// <summary>
    /// 作业执行对话框
    /// </summary>
    public partial class JobExecutionDialog : Window
    {
        private readonly JobConfig _jobConfig;
        private readonly IJobService _jobService;
        private List<ExcelStepInfo> _excelSteps;

        public JobExecutionDialog(JobConfig jobConfig, IJobService jobService)
        {
            InitializeComponent();
            _jobConfig = jobConfig ?? throw new ArgumentNullException(nameof(jobConfig));
            _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
            
            // 设置窗口标题和作业名称
            Title = $"作业执行 - {_jobConfig.Name}";
            JobNameText.Text = _jobConfig.Name;
            
            // 异步初始化Excel步骤
            _ = InitializeAsync();
        }

        /// <summary>
        /// 异步初始化
        /// </summary>
        private async Task InitializeAsync()
        {
            try
            {
                // 初始化Excel步骤信息
                _excelSteps = await InitializeExcelStepsAsync();
                
                // 显示Excel步骤或提示信息
                await Dispatcher.InvokeAsync(() => DisplayExcelSteps());
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"初始化失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        /// <summary>
        /// 初始化Excel步骤信息
        /// </summary>
        private async Task<List<ExcelStepInfo>> InitializeExcelStepsAsync()
        {
            var excelSteps = new List<ExcelStepInfo>();
            
            if (_jobConfig.Steps != null)
            {
                foreach (var step in _jobConfig.Steps.Where(s => s.Type == StepType.ExcelImport))
                {
                    try
                    {
                        // 获取Excel配置的详细信息
                        var excelConfig = await GetExcelConfigAsync(step.ExcelConfigId);
                        
                        var excelStep = new ExcelStepInfo
                        {
                            StepId = step.Id,
                            Name = step.Name,
                            Description = step.Description,
                            FilePath = excelConfig?.FilePath ?? "路径未配置",
                            ExcelConfigId = step.ExcelConfigId
                        };
                        
                        excelSteps.Add(excelStep);
                    }
                    catch (Exception ex)
                    {
                        // 如果获取配置失败，使用步骤信息
                        var excelStep = new ExcelStepInfo
                        {
                            StepId = step.Id,
                            Name = step.Name,
                            Description = step.Description,
                            FilePath = $"配置获取失败: {ex.Message}",
                            ExcelConfigId = step.ExcelConfigId
                        };
                        
                        excelSteps.Add(excelStep);
                    }
                }
            }
            
            return excelSteps;
        }

        /// <summary>
        /// 获取Excel配置信息
        /// </summary>
        private async Task<ExcelConfig?> GetExcelConfigAsync(string excelConfigId)
        {
            try
            {
                // 通过依赖注入获取Excel配置服务
                var excelConfigService = App.Services.GetService(typeof(ExcelProcessor.Core.Services.IExcelConfigService)) 
                    as ExcelProcessor.Core.Services.IExcelConfigService;
                
                if (excelConfigService != null)
                {
                    return await excelConfigService.GetConfigByIdAsync(excelConfigId);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取Excel配置失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 显示Excel步骤信息
        /// </summary>
        private void DisplayExcelSteps()
        {
            if (_excelSteps.Any())
            {
                ExcelStepsList.ItemsSource = _excelSteps;
                ExcelStepsList.Visibility = Visibility.Visible;
                NoExcelStepsBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                ExcelStepsList.Visibility = Visibility.Collapsed;
                NoExcelStepsBorder.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 打开路径按钮点击事件
        /// </summary>
        private void OpenPathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is string filePath)
                {
                    if (Directory.Exists(filePath))
                    {
                        // 如果是目录，打开资源管理器
                        Process.Start("explorer.exe", filePath);
                    }
                    else if (File.Exists(filePath))
                    {
                        // 如果是文件，打开文件所在目录
                        var directory = Path.GetDirectoryName(filePath);
                        if (!string.IsNullOrEmpty(directory))
                        {
                            Process.Start("explorer.exe", directory);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"路径不存在：{filePath}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开路径失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 放入Excel按钮点击事件
        /// </summary>
        private void OpenExcelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is string filePath)
                {
                    if (File.Exists(filePath))
                    {
                        // 尝试用Excel打开文件
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = filePath,
                                UseShellExecute = true
                            });
                        }
                        catch
                        {
                            // 如果直接打开失败，尝试用默认程序打开
                            Process.Start("explorer.exe", filePath);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"文件不存在：{filePath}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开Excel文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 执行作业按钮点击事件
        /// </summary>
        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 禁用执行按钮，避免重复点击
                ExecuteButton.IsEnabled = false;
                ExecuteButton.Content = "执行中...";

                // 显示执行结果区域
                ExecutionResultBorder.Visibility = Visibility.Visible;
                ExecutionResultText.Text = "正在执行作业，请稍候...";

                // 执行作业
                var (success, message, executionId) = await _jobService.ExecuteJobAsync(_jobConfig.Id);

                // 显示执行结果
                if (success)
                {
                    ExecutionResultText.Text = $"作业执行成功！\n\n执行ID: {executionId}\n消息: {message}\n\n执行时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    ExecutionResultText.Foreground = System.Windows.Media.Brushes.LightGreen;
                }
                else
                {
                    ExecutionResultText.Text = $"作业执行失败！\n\n错误信息: {message}\n\n执行时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    ExecutionResultText.Foreground = System.Windows.Media.Brushes.LightCoral;
                }

                // 恢复执行按钮
                ExecuteButton.IsEnabled = true;
                ExecuteButton.Content = "执行作业";
            }
            catch (Exception ex)
            {
                ExecutionResultText.Text = $"执行作业时发生异常：\n\n{ex.Message}\n\n执行时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                ExecutionResultText.Foreground = System.Windows.Media.Brushes.LightCoral;
                
                // 恢复执行按钮
                ExecuteButton.IsEnabled = true;
                ExecuteButton.Content = "执行作业";
            }
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    /// <summary>
    /// Excel步骤信息类
    /// </summary>
    public class ExcelStepInfo
    {
        public string StepId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ExcelConfigId { get; set; } = string.Empty;
    }
} 