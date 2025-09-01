using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ExcelProcessor.Core.Interfaces;

namespace ExcelProcessor.WPF.Controls
{
    /// <summary>
    /// 测试输出结果弹窗
    /// </summary>
    public partial class TestOutputResultDialog : Window
    {
        private string _outputFilePath;

        public TestOutputResultDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 显示测试输出结果
        /// </summary>
        /// <param name="isSuccess">是否成功</param>
        /// <param name="outputType">输出类型</param>
        /// <param name="details">详细信息</param>
        /// <param name="errorMessage">错误信息</param>
        /// <param name="owner">父窗口</param>
        public static void ShowResult(bool isSuccess, string outputType, Dictionary<string, string> details, string errorMessage = null, Window owner = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"TestOutputResultDialog.ShowResult 开始 - 成功: {isSuccess}, 类型: {outputType}");
                
                var dialog = new TestOutputResultDialog();
                dialog.Owner = owner;
                
                System.Diagnostics.Debug.WriteLine($"对话框创建成功，开始设置结果");
                
                dialog.SetResult(isSuccess, outputType, details, errorMessage);
                
                System.Diagnostics.Debug.WriteLine($"结果设置完成，开始显示对话框");
                
                dialog.ShowDialog();
                
                System.Diagnostics.Debug.WriteLine($"对话框显示完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TestOutputResultDialog.ShowResult 异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                
                // 如果自定义对话框出现问题，回退到简单的MessageBox
                var message = isSuccess 
                    ? $"测试输出到{outputType}成功！" 
                    : $"测试输出到{outputType}失败！\n\n错误信息：{errorMessage}";
                
                MessageBox.Show(message, "测试结果", MessageBoxButton.OK, 
                    isSuccess ? MessageBoxImage.Information : MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 设置测试结果
        /// </summary>
        /// <param name="isSuccess">是否成功</param>
        /// <param name="outputType">输出类型</param>
        /// <param name="details">详细信息</param>
        /// <param name="errorMessage">错误信息</param>
        private void SetResult(bool isSuccess, string outputType, Dictionary<string, string> details, string errorMessage = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"SetResult 开始 - 成功: {isSuccess}, 类型: {outputType}");
                
                // 重置输出文件路径
                _outputFilePath = null;
                
                // 确保UI元素存在
                System.Diagnostics.Debug.WriteLine($"检查UI元素 - StatusIcon: {StatusIcon != null}, StatusText: {StatusText != null}, OutputTypeText: {OutputTypeText != null}");
                
                if (StatusIcon == null || StatusText == null || OutputTypeText == null)
                {
                    throw new InvalidOperationException("UI元素未正确初始化");
                }
                
                if (isSuccess)
                {
                    System.Diagnostics.Debug.WriteLine($"设置成功状态");
                    
                    // 成功状态
                    StatusIcon.Text = "✅";
                    StatusText.Text = $"测试输出到{outputType}成功！";
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;
                    
                    // 基本信息
                    OutputTypeText.Text = $"📊 输出类型：{outputType}";
                    
                    // 显示详细信息
                    System.Diagnostics.Debug.WriteLine($"检查DetailsPanel: {DetailsPanel != null}");
                    if (DetailsPanel != null)
                    {
                        DetailsPanel.Visibility = Visibility.Visible;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"检查DetailsStackPanel: {DetailsStackPanel != null}");
                    if (DetailsStackPanel != null)
                    {
                        DetailsStackPanel.Children.Clear();
                        
                        System.Diagnostics.Debug.WriteLine($"Details数量: {details?.Count ?? 0}");
                        if (details != null && details.Count > 0)
                        {
                            foreach (var detail in details)
                            {
                                try
                                {
                                    System.Diagnostics.Debug.WriteLine($"处理详情项: {detail.Key} = {detail.Value}");
                                    
                                    var detailItem = new StackPanel();
                                    var detailItemStyle = FindResource("DetailItemStyle") as Style;
                                    if (detailItemStyle != null)
                                    {
                                        detailItem.Style = detailItemStyle;
                                    }
                                    
                                    var label = new TextBlock
                                    {
                                        Text = detail.Key ?? "未知"
                                    };
                                    var labelStyle = FindResource("DetailLabelStyle") as Style;
                                    if (labelStyle != null)
                                    {
                                        label.Style = labelStyle;
                                    }
                                    
                                    var value = new TextBlock
                                    {
                                        Text = detail.Value ?? ""
                                    };
                                    var valueStyle = FindResource("DetailValueStyle") as Style;
                                    if (valueStyle != null)
                                    {
                                        value.Style = valueStyle;
                                    }
                                    
                                    detailItem.Children.Add(label);
                                    detailItem.Children.Add(value);
                                    DetailsStackPanel.Children.Add(detailItem);
                                    
                                    // 检查是否是输出路径，如果是Excel工作表输出
                                    if (outputType == "Excel工作表" && detail.Key == "输出目标" && !string.IsNullOrEmpty(detail.Value))
                                    {
                                        _outputFilePath = detail.Value;
                                        System.Diagnostics.Debug.WriteLine($"设置输出文件路径: {_outputFilePath}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // 如果单个详情项处理失败，继续处理其他项
                                    System.Diagnostics.Debug.WriteLine($"处理详情项失败: {ex.Message}");
                                }
                            }
                        }
                    }
                    
                    // 如果是Excel工作表输出且成功，显示打开文件位置按钮
                    System.Diagnostics.Debug.WriteLine($"检查OpenFileLocationButton: {OpenFileLocationButton != null}, 输出路径: {_outputFilePath}");
                    if (outputType == "Excel工作表" && !string.IsNullOrEmpty(_outputFilePath) && OpenFileLocationButton != null)
                    {
                        OpenFileLocationButton.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"设置失败状态");
                    
                    // 失败状态
                    StatusIcon.Text = "❌";
                    StatusText.Text = $"测试输出到{outputType}失败！";
                    StatusText.Foreground = System.Windows.Media.Brushes.Red;
                    
                    // 基本信息
                    OutputTypeText.Text = $"📊 输出类型：{outputType}";
                    
                    // 显示错误信息
                    System.Diagnostics.Debug.WriteLine($"检查ErrorPanel: {ErrorPanel != null}, ErrorText: {ErrorText != null}");
                    if (!string.IsNullOrEmpty(errorMessage) && ErrorPanel != null && ErrorText != null)
                    {
                        ErrorPanel.Visibility = Visibility.Visible;
                        ErrorText.Text = errorMessage;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"SetResult 完成");
            }
            catch (Exception ex)
            {
                // 如果设置结果时出现异常，记录错误并关闭窗口
                System.Diagnostics.Debug.WriteLine($"设置测试结果失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                MessageBox.Show($"显示测试结果时出现错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 打开文件位置按钮点击事件
        /// </summary>
        private void OpenFileLocationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_outputFilePath) && File.Exists(_outputFilePath))
                {
                    // 打开文件所在文件夹并选中文件
                    Process.Start("explorer.exe", $"/select,\"{_outputFilePath}\"");
                }
                else if (!string.IsNullOrEmpty(_outputFilePath))
                {
                    // 如果文件不存在，只打开文件夹
                    var directory = Path.GetDirectoryName(_outputFilePath);
                    if (Directory.Exists(directory))
                    {
                        Process.Start("explorer.exe", $"\"{directory}\"");
                    }
                    else
                    {
                        MessageBox.Show("无法找到输出文件或文件夹", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开文件位置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 