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
        private List<JobStepInfo> _jobSteps;

        public JobExecutionDialog(JobConfig jobConfig, IJobService jobService)
        {
            InitializeComponent();
            _jobConfig = jobConfig ?? throw new ArgumentNullException(nameof(jobConfig));
            _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
            
            // 设置窗口标题和作业名称
            Title = $"作业执行 - {_jobConfig.Name}";
            JobNameText.Text = _jobConfig.Name;
            
            // 异步初始化作业步骤
            _ = InitializeAsync();
        }

        /// <summary>
        /// 异步初始化
        /// </summary>
        private async Task InitializeAsync()
        {
            try
            {
                // 初始化作业步骤信息
                _jobSteps = await InitializeJobStepsAsync();
                
                // 显示作业步骤或提示信息
                await Dispatcher.InvokeAsync(() => DisplayJobSteps());
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    Extensions.MessageBoxExtensions.Show($"初始化失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        /// <summary>
        /// 初始化作业步骤信息
        /// </summary>
        private async Task<List<JobStepInfo>> InitializeJobStepsAsync()
        {
            var jobSteps = new List<JobStepInfo>();
            
            if (_jobConfig.Steps != null)
            {
                // 按OrderIndex排序获取所有步骤
                var orderedSteps = _jobConfig.Steps.OrderBy(s => s.OrderIndex).ToList();
                
                foreach (var step in orderedSteps)
                {
                    try
                    {
                        var stepInfo = new JobStepInfo
                        {
                            StepId = step.Id,
                            Name = step.Name,
                            Description = step.Description,
                            Type = step.Type,
                            OrderIndex = step.OrderIndex,
                            FilePath = string.Empty,
                            HasPath = false,
                            ShowBrowseButton = false
                        };

                        // 根据步骤类型获取路径信息
                        switch (step.Type)
                        {
                            case StepType.ExcelImport:
                                var excelConfig = await GetExcelConfigAsync(step.ExcelConfigId);
                                if (excelConfig != null)
                                {
                                    stepInfo.FilePath = excelConfig.FilePath;
                                    stepInfo.HasPath = true;
                                    stepInfo.ShowBrowseButton = true; // Excel导入步骤显示浏览按钮
                                }
                                break;

                            case StepType.SqlExecution:
                                // SQL执行步骤可能输出到Excel，检查是否有输出配置
                                var sqlConfig = await GetSqlConfigAsync(step.SqlConfigId);
                                if (sqlConfig != null)
                                {
                                    if (sqlConfig.OutputType.Equals("Excel工作表", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // 对于输出到Excel工作表的SQL，显示输出目标信息
                                        stepInfo.FilePath = $"输出到Excel工作表: {sqlConfig.OutputTarget}";
                                        stepInfo.HasPath = true;
                                        stepInfo.ShowBrowseButton = true; // 输出到Excel工作表时显示浏览按钮
                                    }
                                    else if (sqlConfig.OutputType.Equals("数据表", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // 对于输出到数据表的SQL，显示表名信息
                                        stepInfo.FilePath = $"输出到数据表: {sqlConfig.OutputTarget}";
                                        stepInfo.HasPath = true;
                                        stepInfo.ShowBrowseButton = false; // 输出到数据表时不显示浏览按钮
                                    }
                                    else
                                    {
                                        // 其他输出类型，显示输出类型和目标
                                        stepInfo.FilePath = $"输出类型: {sqlConfig.OutputType}, 目标: {sqlConfig.OutputTarget}";
                                        stepInfo.HasPath = true;
                                        stepInfo.ShowBrowseButton = false; // 其他输出类型不显示浏览按钮
                                    }
                                }
                                break;

                            case StepType.DataExport:
                                // 数据导出步骤，检查导出类型和路径
                                var exportConfig = await GetDataExportConfigAsync(step);
                                if (exportConfig != null && exportConfig.ExportType.Equals("Excel", StringComparison.OrdinalIgnoreCase))
                                {
                                    stepInfo.FilePath = exportConfig.TargetPath;
                                    stepInfo.HasPath = true;
                                    stepInfo.ShowBrowseButton = true; // Excel导出步骤显示浏览按钮
                                }
                                break;

                            case StepType.FileOperation:
                                // 文件操作步骤，检查源路径或目标路径
                                var fileOpConfig = await GetFileOperationConfigAsync(step);
                                if (fileOpConfig != null)
                                {
                                    if (!string.IsNullOrEmpty(fileOpConfig.SourcePath))
                                    {
                                        stepInfo.FilePath = fileOpConfig.SourcePath;
                                        stepInfo.HasPath = true;
                                        stepInfo.ShowBrowseButton = true; // 文件操作步骤显示浏览按钮
                                    }
                                    else if (!string.IsNullOrEmpty(fileOpConfig.TargetPath))
                                    {
                                        stepInfo.FilePath = fileOpConfig.TargetPath;
                                        stepInfo.HasPath = true;
                                        stepInfo.ShowBrowseButton = true; // 文件操作步骤显示浏览按钮
                                    }
                                }
                                break;
                        }
                        
                        jobSteps.Add(stepInfo);
                    }
                    catch (Exception ex)
                    {
                        // 如果获取配置失败，使用步骤基本信息
                        var stepInfo = new JobStepInfo
                        {
                            StepId = step.Id,
                            Name = step.Name,
                            Description = step.Description,
                            Type = step.Type,
                            OrderIndex = step.OrderIndex,
                            FilePath = $"配置获取失败: {ex.Message}",
                            HasPath = false,
                            ShowBrowseButton = false
                        };
                        
                        jobSteps.Add(stepInfo);
                    }
                }
            }
            
            return jobSteps;
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
        /// 获取SQL配置信息
        /// </summary>
        private async Task<SqlConfig?> GetSqlConfigAsync(string sqlConfigId)
        {
            try
            {
                // 通过依赖注入获取SQL服务
                var sqlService = App.Services.GetService(typeof(ExcelProcessor.Core.Interfaces.ISqlService)) 
                    as ExcelProcessor.Core.Interfaces.ISqlService;
                
                if (sqlService != null)
                {
                    return await sqlService.GetSqlConfigByIdAsync(sqlConfigId);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取SQL配置失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取数据导出配置信息
        /// </summary>
        private async Task<DataExportStepConfig?> GetDataExportConfigAsync(JobStep step)
        {
            try
            {
                // 这里需要根据实际的配置存储方式来实现
                // 暂时返回null，后续可以根据需要完善
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取数据导出配置失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取文件操作配置信息
        /// </summary>
        private async Task<FileOperationStepConfig?> GetFileOperationConfigAsync(JobStep step)
        {
            try
            {
                // 这里需要根据实际的配置存储方式来实现
                // 暂时返回null，后续可以根据需要完善
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取文件操作配置失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 显示作业步骤信息
        /// </summary>
        private void DisplayJobSteps()
        {
            if (_jobSteps.Any())
            {
                JobStepsList.ItemsSource = _jobSteps;
                JobStepsList.Visibility = Visibility.Visible;
                NoJobStepsBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                JobStepsList.Visibility = Visibility.Collapsed;
                NoJobStepsBorder.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 打开路径按钮点击事件
        /// </summary>
        private void OpenPathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is JobStepInfo stepInfo)
                {
                    var filePath = stepInfo.FilePath;
                    
                    // 根据步骤类型和输出类型处理路径
                    if (stepInfo.Type == StepType.SqlExecution)
                    {
                        // SQL执行步骤，检查输出类型
                        if (filePath.StartsWith("输出到Excel工作表:"))
                        {
                            // 从显示文本中提取实际的Excel文件路径
                            var actualPath = filePath.Replace("输出到Excel工作表: ", "");
                            
                            // 解析输出目标（格式：文件路径!Sheet名称）
                            if (actualPath.Contains("!"))
                            {
                                var parts = actualPath.Split('!');
                                if (parts.Length >= 2)
                                {
                                    var excelFilePath = parts[0];
                                    var sheetName = parts[1];
                                    
                                    // 检查Excel文件路径是否存在
                                    if (File.Exists(excelFilePath))
                                    {
                                        // 如果文件存在，打开文件所在目录
                                        var directory = Path.GetDirectoryName(excelFilePath);
                                        if (!string.IsNullOrEmpty(directory))
                                        {
                                            Process.Start("explorer.exe", directory);
                                            return;
                                        }
                                    }
                                    else if (Directory.Exists(Path.GetDirectoryName(excelFilePath) ?? ""))
                                    {
                                        // 如果目录存在但文件不存在，打开目录
                                        var directory = Path.GetDirectoryName(excelFilePath);
                                        if (!string.IsNullOrEmpty(directory))
                                        {
                                            Process.Start("explorer.exe", directory);
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        // 如果路径不存在，尝试打开父目录
                                        var directory = Path.GetDirectoryName(excelFilePath);
                                        if (!string.IsNullOrEmpty(directory))
                                        {
                                            try
                                            {
                                                Process.Start("explorer.exe", directory);
                                                return;
                                            }
                                            catch
                                            {
                                                // 如果父目录也不存在，显示提示信息
                                                Extensions.MessageBoxExtensions.Show($"Excel文件路径不存在：{excelFilePath}\n\nSheet名称：{sheetName}\n\n请检查配置的路径是否正确。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            
                            // 如果无法解析路径，显示提示信息
                            Extensions.MessageBoxExtensions.Show($"无法解析Excel文件路径：{actualPath}\n\n请检查SQL配置中的输出目标设置。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                    }
                    
                    // 处理文件路径
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
                        Extensions.MessageBoxExtensions.Show($"路径不存在：{filePath}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"打开路径失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
    /// 作业步骤信息类
    /// </summary>
    public class JobStepInfo
    {
        public string StepId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public StepType Type { get; set; }
        public int OrderIndex { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public bool HasPath { get; set; }
        public bool ShowBrowseButton { get; set; }
        
        /// <summary>
        /// 步骤类型显示名称
        /// </summary>
        public string TypeDisplayName => Type switch
        {
            StepType.ExcelImport => "Excel导入",
            StepType.SqlExecution => "SQL执行",
            StepType.DataProcessing => "数据处理",
            StepType.FileOperation => "文件操作",
            StepType.EmailSend => "邮件发送",
            StepType.Condition => "条件判断",
            StepType.Loop => "循环",
            StepType.Wait => "等待",
            StepType.CustomScript => "自定义脚本",
            StepType.DataValidation => "数据验证",
            StepType.ReportGeneration => "报表生成",
            StepType.DataExport => "数据导出",
            StepType.Notification => "通知",
            _ => "未知类型"
        };
        
        /// <summary>
        /// 浏览按钮文本
        /// </summary>
        public string BrowseButtonText => Type switch
        {
            StepType.ExcelImport => "打开导入路径",
            StepType.SqlExecution when FilePath.StartsWith("输出到Excel工作表:") => "打开导出路径",
            StepType.SqlExecution => "打开路径",
            StepType.DataExport => "打开导出路径",
            StepType.FileOperation => "打开路径",
            _ => "打开路径"
        };
    }
} 