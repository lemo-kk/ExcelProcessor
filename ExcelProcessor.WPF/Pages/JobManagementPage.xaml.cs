using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Models;
using ExcelProcessor.WPF.Dialogs;
using ExcelProcessor.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.WPF.Pages
{
    /// <summary>
    /// JobManagementPage.xaml 的交互逻辑
    /// </summary>
    public partial class JobManagementPage : Page
    {
        private JobManagementViewModel _viewModel;

        public JobManagementPage()
        {
            InitializeComponent();
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            try
            {
                // 从依赖注入容器获取服务
                var jobService = App.Services.GetRequiredService<IJobService>();
                var loggerFactory = App.Services.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<JobManagementViewModel>();

                // 创建ViewModel
                _viewModel = new JobManagementViewModel(jobService, logger);

                // 设置数据上下文
                DataContext = _viewModel;

                // 绑定事件
                SearchTextBox.TextChanged += SearchTextBox_TextChanged;
                StatusFilterComboBox.SelectionChanged += StatusFilterComboBox_SelectionChanged;
                TypeFilterComboBox.SelectionChanged += TypeFilterComboBox_SelectionChanged;
                PriorityFilterComboBox.SelectionChanged += PriorityFilterComboBox_SelectionChanged;
                ExecutionModeFilterComboBox.SelectionChanged += ExecutionModeFilterComboBox_SelectionChanged;
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"初始化页面失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.SearchText = SearchTextBox.Text;
            }
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel != null && StatusFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _viewModel.SelectedStatusFilter = selectedItem.Content?.ToString() ?? "全部";
            }
        }

        private void TypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel != null && TypeFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _viewModel.SelectedTypeFilter = selectedItem.Content?.ToString() ?? "全部";
            }
        }

        private void PriorityFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel != null && PriorityFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _viewModel.SelectedPriorityFilter = selectedItem.Content?.ToString() ?? "全部";
            }
        }

        private void ExecutionModeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel != null && ExecutionModeFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _viewModel.SelectedExecutionModeFilter = selectedItem.Content?.ToString() ?? "全部";
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel?.RefreshCommand.Execute(null);
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"刷新数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateJobButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new JobEditDialog();
                dialog.Owner = Window.GetWindow(this);
                
                if (dialog.ShowDialog() == true && dialog.IsSaved)
                {
                    // 创建作业
                    _viewModel?.CreateJobAsync(dialog.JobConfig);
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"创建作业失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditJobButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string jobId)
                {
                    // 从ViewModel中获取作业配置
                    var job = _viewModel?.FilteredJobs.FirstOrDefault(j => j.Id == jobId);
                    if (job != null)
                    {
                        var dialog = new JobEditDialog(job);
                        dialog.Owner = Window.GetWindow(this);
                        
                        if (dialog.ShowDialog() == true && dialog.IsSaved)
                        {
                            // 更新作业
                            _viewModel?.EditJobAsync(dialog.JobConfig);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"编辑作业失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel?.ClearFiltersCommand.Execute(null);
                
                // 清除UI控件
                SearchTextBox.Text = string.Empty;
                if (StatusFilterComboBox != null) StatusFilterComboBox.SelectedIndex = 0;
                if (TypeFilterComboBox != null) TypeFilterComboBox.SelectedIndex = 0;
                if (PriorityFilterComboBox != null) PriorityFilterComboBox.SelectedIndex = 0;
                if (ExecutionModeFilterComboBox != null) ExecutionModeFilterComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"清除筛选失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 作业操作按钮事件处理
        private void ExecuteJob_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is JobConfig job)
                {
                    _viewModel?.ExecuteJobCommand.Execute(job);
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"执行作业失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PauseJob_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is JobConfig job)
                {
                    _viewModel?.PauseJobCommand.Execute(job);
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"暂停作业失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResumeJob_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is JobConfig job)
                {
                    _viewModel?.ResumeJobCommand.Execute(job);
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"恢复作业失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteJob_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is JobConfig job)
                {
                    _viewModel?.DeleteJobCommand.Execute(job);
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"删除作业失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewJobDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is JobConfig job)
                {
                    var jobService = App.Services.GetRequiredService<IJobService>();
                    var dlg = new Dialogs.JobExecutionHistoryDialog(jobService, job.Id, job.Name)
                    {
                        Owner = Application.Current.MainWindow
                    };
                    dlg.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"查看作业详情失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 