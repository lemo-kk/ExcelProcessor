using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ExcelProcessor.WPF.ViewModels;
using ExcelProcessor.WPF.Models;
using ExcelProcessor.WPF.Dialogs;
using ExcelProcessor.WPF.Extensions;

namespace ExcelProcessor.WPF.Pages
{
    /// <summary>
    /// 导入导出管理页面
    /// </summary>
    public partial class ImportExportPage : Page, INotifyPropertyChanged
    {
        private ImportExportPageViewModel _viewModel;

        public ImportExportPage()
        {
            InitializeComponent();
            InitializeViewModel();
            Loaded += ImportExportPage_Loaded;
        }

        /// <summary>
        /// 初始化视图模型
        /// </summary>
        private void InitializeViewModel()
        {
            _viewModel = new ImportExportPageViewModel();
            DataContext = _viewModel;
        }

        /// <summary>
        /// 页面加载完成事件
        /// </summary>
        private async void ImportExportPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 加载可用作业列表
                await _viewModel.LoadAvailableJobsAsync();
                
                // 加载导入导出历史记录
                await _viewModel.LoadImportExportHistoryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"页面加载失败：{ex.Message}");
                MessageBoxExtensions.Show($"页面加载失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出作业配置包按钮点击事件
        /// </summary>
        private async void ExportJobPackage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel.SelectedJob == null)
                {
                    MessageBoxExtensions.Show("请先选择要导出的作业", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 显示保存文件对话框
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "导出作业配置包",
                    Filter = "作业配置包文件 (*.jobpkg)|*.jobpkg|所有文件 (*.*)|*.*",
                    FileName = $"{_viewModel.SelectedJob.Name}_配置包_{DateTime.Now:yyyyMMdd_HHmmss}.jobpkg",
                    DefaultExt = "jobpkg"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // 执行导出操作
                    var (success, message) = await _viewModel.ExportJobPackageAsync(saveFileDialog.FileName);
                    
                    if (success)
                    {
                        MessageBoxExtensions.Show($"作业配置包导出成功！\n保存路径：{saveFileDialog.FileName}", "导出成功", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // 刷新历史记录
                        await _viewModel.LoadImportExportHistoryAsync();
                    }
                    else
                    {
                        MessageBoxExtensions.Show($"导出失败：{message}", "导出失败", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导出作业配置包失败：{ex.Message}");
                MessageBoxExtensions.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 预览作业配置包按钮点击事件
        /// </summary>
        private async void PreviewJobPackage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel.SelectedJob == null)
                {
                    MessageBoxExtensions.Show("请先选择要预览的作业", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 生成预览信息
                var previewInfo = await _viewModel.GenerateJobPackagePreviewAsync();
                
                // 显示预览对话框
                var previewDialog = new JobPackagePreviewDialog(previewInfo);
                previewDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"预览作业配置包失败：{ex.Message}");
                MessageBoxExtensions.Show($"预览失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 浏览配置文件按钮点击事件
        /// </summary>
        private void BrowsePackageFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "选择作业配置包文件",
                    Filter = "作业配置包文件 (*.jobpkg)|*.jobpkg|所有文件 (*.*)|*.*",
                    DefaultExt = "jobpkg"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    _viewModel.PackageFilePath = openFileDialog.FileName;
                    
                    // 自动验证配置文件
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _viewModel.ValidatePackageFileAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"自动验证配置文件失败：{ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"浏览配置文件失败：{ex.Message}");
                MessageBoxExtensions.Show($"浏览文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导入作业配置包按钮点击事件
        /// </summary>
        private async void ImportJobPackage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_viewModel.PackageFilePath))
                {
                    MessageBoxExtensions.Show("请先选择要导入的配置文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 确认导入操作
                var result = MessageBoxExtensions.Show("确定要导入这个作业配置包吗？导入过程可能需要一些时间。", 
                    "确认导入", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // 执行导入操作
                    var (success, message) = await _viewModel.ImportJobPackageAsync();
                    
                    if (success)
                    {
                        MessageBoxExtensions.Show("作业配置包导入成功！", "导入成功", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // 刷新历史记录
                        await _viewModel.LoadImportExportHistoryAsync();
                        
                        // 清空文件路径
                        _viewModel.PackageFilePath = string.Empty;
                    }
                    else
                    {
                        MessageBoxExtensions.Show($"导入失败：{message}", "导入失败", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导入作业配置包失败：{ex.Message}");
                MessageBoxExtensions.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 验证作业配置包按钮点击事件
        /// </summary>
        private async void ValidateJobPackage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_viewModel.PackageFilePath))
                {
                    MessageBoxExtensions.Show("请先选择要验证的配置文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 执行验证操作
                var (isValid, message, validationDetails) = await _viewModel.ValidateJobPackageAsync();
                
                if (isValid)
                {
                    MessageBoxExtensions.Show("配置文件验证通过！可以安全导入。", "验证成功", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBoxExtensions.Show($"配置文件验证失败：{message}", "验证失败", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"验证作业配置包失败：{ex.Message}");
                MessageBoxExtensions.Show($"验证失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 查看历史详情按钮点击事件
        /// </summary>
        private void ViewHistoryDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is ImportExportHistoryViewModel history)
                {
                    // 显示历史详情对话框
                    var detailsDialog = new ImportExportHistoryDetailsDialog(history);
                    detailsDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"查看历史详情失败：{ex.Message}");
                MessageBoxExtensions.Show($"查看详情失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 从历史记录重新导入按钮点击事件
        /// </summary>
        private async void ReImportFromHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is ImportExportHistoryViewModel history)
                {
                    // 确认重新导入
                    var result = MessageBoxExtensions.Show($"确定要重新导入作业 '{history.JobName}' 的配置包吗？", 
                        "确认重新导入", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        // 执行重新导入
                        var (success, message) = await _viewModel.ReImportFromHistoryAsync(history);
                        
                        if (success)
                        {
                            MessageBoxExtensions.Show("重新导入成功！", "导入成功", 
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // 刷新历史记录
                            await _viewModel.LoadImportExportHistoryAsync();
                        }
                        else
                        {
                            MessageBoxExtensions.Show($"重新导入失败：{message}", "导入失败", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重新导入失败：{ex.Message}");
                MessageBoxExtensions.Show($"重新导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 刷新历史记录按钮点击事件
        /// </summary>
        private async void RefreshHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.LoadImportExportHistoryAsync();
                MessageBoxExtensions.Show("历史记录已刷新", "刷新成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新历史记录失败：{ex.Message}");
                MessageBoxExtensions.Show($"刷新失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出历史日志按钮点击事件
        /// </summary>
        private async void ExportHistoryLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "导出历史日志",
                    Filter = "CSV文件 (*.csv)|*.csv|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                    FileName = $"导入导出历史日志_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                    DefaultExt = "csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // 执行导出日志操作
                    var (success, message) = await _viewModel.ExportHistoryLogAsync(saveFileDialog.FileName);
                    
                    if (success)
                    {
                        MessageBoxExtensions.Show($"历史日志导出成功！\n保存路径：{saveFileDialog.FileName}", "导出成功", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBoxExtensions.Show($"导出历史日志失败：{message}", "导出失败", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导出历史日志失败：{ex.Message}");
                MessageBoxExtensions.Show($"导出日志失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 清空历史记录按钮点击事件
        /// </summary>
        private async void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBoxExtensions.Show("确定要清空所有导入导出历史记录吗？此操作不可恢复！", 
                    "确认清空", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    // 执行清空历史记录操作
                    var (success, message) = await _viewModel.ClearImportExportHistoryAsync();
                    
                    if (success)
                    {
                        MessageBoxExtensions.Show("历史记录已清空", "清空成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBoxExtensions.Show($"清空历史记录失败：{message}", "清空失败", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清空历史记录失败：{ex.Message}");
                MessageBoxExtensions.Show($"清空失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region INotifyPropertyChanged 实现

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 