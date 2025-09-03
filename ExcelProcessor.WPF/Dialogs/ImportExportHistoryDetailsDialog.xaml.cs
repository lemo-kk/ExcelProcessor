using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using ExcelProcessor.WPF.ViewModels;
using ExcelProcessor.WPF.Extensions;

namespace ExcelProcessor.WPF.Dialogs
{
    /// <summary>
    /// 导入导出历史详情对话框
    /// </summary>
    public partial class ImportExportHistoryDetailsDialog : Window
    {
        private readonly ImportExportHistoryViewModel _history;

        public ImportExportHistoryDetailsDialog(ImportExportHistoryViewModel history)
        {
            InitializeComponent();
            _history = history;
            DataContext = history;
        }

        /// <summary>
        /// 重新导入按钮点击事件
        /// </summary>
        private void ReImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 这里应该触发重新导入事件或调用父页面的方法
                // 暂时显示提示信息
                MessageBoxExtensions.Show("重新导入功能将在后续版本中实现", "功能提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重新导入失败：{ex.Message}");
                MessageBoxExtensions.Show($"重新导入失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 打开文件位置按钮点击事件
        /// </summary>
        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_history.PackagePath) && File.Exists(_history.PackagePath))
                {
                    // 打开文件所在文件夹并选中文件
                    Process.Start("explorer.exe", $"/select,\"{_history.PackagePath}\"");
                }
                else
                {
                    MessageBoxExtensions.Show("文件不存在或路径无效", "提示", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打开文件位置失败：{ex.Message}");
                MessageBoxExtensions.Show($"打开文件位置失败：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
} 