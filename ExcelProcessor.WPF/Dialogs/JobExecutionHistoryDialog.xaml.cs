using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Models;

namespace ExcelProcessor.WPF.Dialogs
{
    public partial class JobExecutionHistoryDialog : Window
    {
        private readonly IJobService _jobService;
        private readonly JobExecutionHistoryViewModel _viewModel;
        private readonly string _jobId;

        public JobExecutionHistoryDialog(IJobService jobService, string jobId, string jobName)
        {
            InitializeComponent();
            _jobService = jobService;
            _jobId = jobId;
            Title = $"{jobName} - 执行记录";
            _viewModel = new JobExecutionHistoryViewModel();
            DataContext = _viewModel;
            Loaded += async (_, __) => await LoadExecutionsAsync();
        }

        private async Task LoadExecutionsAsync()
        {
            try
            {
                var (executions, _) = await _jobService.GetJobExecutionsAsync(_jobId, 1, 100);
                _viewModel.Executions = executions;
                if (_viewModel.Executions.Any())
                {
                    _viewModel.SelectedExecution = _viewModel.Executions.First();
                    await LoadStepExecutionsAsync(_viewModel.SelectedExecution.Id);
                }
                else
                {
                    _viewModel.StepExecutions = new List<JobStepExecution>();
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"加载执行记录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadStepExecutionsAsync(string executionId)
        {
            var steps = await _jobService.GetJobStepExecutionsAsync(executionId);
            _viewModel.StepExecutions = steps;
        }

        private async void RefreshExecutions_Click(object sender, RoutedEventArgs e)
        {
            await LoadExecutionsAsync();
        }

        private async void OnSelectedExecutionChanged()
        {
            if (_viewModel.SelectedExecution != null)
            {
                await LoadStepExecutionsAsync(_viewModel.SelectedExecution.Id);
            }
            else
            {
                _viewModel.StepExecutions = new List<JobStepExecution>();
            }
        }

        private async void Executions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await LoadStepExecutionsAsync(_viewModel.SelectedExecution?.Id ?? string.Empty);
        }

        private void CopyError_Click(object sender, RoutedEventArgs e)
        {
            var errors = _viewModel.StepExecutions?
                .Where(s => !string.IsNullOrWhiteSpace(s.ErrorMessage))
                .Select(s => $"[{s.StepName}] {s.ErrorMessage}")
                .ToList();
            if (errors == null || errors.Count == 0)
            {
                Extensions.MessageBoxExtensions.Show("无错误信息可复制。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Clipboard.SetText(string.Join(Environment.NewLine, errors));
            Extensions.MessageBoxExtensions.Show("错误信息已复制到剪贴板。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CopyCellContent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu && 
                contextMenu.PlacementTarget is DataGrid dataGrid)
            {
                var cell = dataGrid.CurrentCell;
                if (cell.Column is DataGridTemplateColumn templateColumn)
                {
                    var row = dataGrid.ItemContainerGenerator.ItemFromContainer(dataGrid.ItemContainerGenerator.ContainerFromItem(cell.Item));
                    if (row is JobStepExecution stepExecution)
                    {
                        string content = "";
                        if (templateColumn.Header.ToString() == "步骤名称")
                            content = stepExecution.StepName;
                        else if (templateColumn.Header.ToString() == "结果信息")
                            content = stepExecution.ResultMessage ?? "";
                        else if (templateColumn.Header.ToString() == "错误信息")
                            content = stepExecution.ErrorMessage ?? "";
                        
                        if (!string.IsNullOrEmpty(content))
                        {
                            Clipboard.SetText(content);
                            Extensions.MessageBoxExtensions.Show("内容已复制到剪贴板。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
        }

        private void CopyRowInfo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu && 
                contextMenu.PlacementTarget is DataGrid dataGrid)
            {
                var selectedItem = dataGrid.SelectedItem;
                if (selectedItem is JobStepExecution stepExecution)
                {
                    var info = $"步骤名称: {stepExecution.StepName}\n" +
                               $"步骤类型: {stepExecution.StepType}\n" +
                               $"执行状态: {stepExecution.Status}\n" +
                               $"开始时间: {stepExecution.StartTime:yyyy-MM-dd HH:mm:ss}\n" +
                               $"结束时间: {stepExecution.EndTime:yyyy-MM-dd HH:mm:ss}\n" +
                               $"执行耗时: {stepExecution.Duration}\n" +
                               $"结果信息: {stepExecution.ResultMessage ?? "无"}\n" +
                               $"错误信息: {stepExecution.ErrorMessage ?? "无"}";
                    
                    Clipboard.SetText(info);
                    Extensions.MessageBoxExtensions.Show("行信息已复制到剪贴板。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private class JobExecutionHistoryViewModel : System.ComponentModel.INotifyPropertyChanged
        {
            private List<JobExecution> _executions = new();
            private JobExecution? _selectedExecution;
            private List<JobStepExecution> _stepExecutions = new();

            public List<JobExecution> Executions
            {
                get => _executions;
                set { _executions = value; OnPropertyChanged(nameof(Executions)); }
            }

            public JobExecution? SelectedExecution
            {
                get => _selectedExecution;
                set
                {
                    _selectedExecution = value;
                    OnPropertyChanged(nameof(SelectedExecution));
                }
            }

            public List<JobStepExecution> StepExecutions
            {
                get => _stepExecutions;
                set { _stepExecutions = value; OnPropertyChanged(nameof(StepExecutions)); }
            }

            public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 成功步骤计数转换器
    /// </summary>
    public class SuccessCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is List<JobStepExecution> steps)
            {
                return steps.Count(s => s.Status == JobStatus.Completed);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 失败步骤计数转换器
    /// </summary>
    public class ErrorCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is List<JobStepExecution> steps)
            {
                return steps.Count(s => s.Status == JobStatus.Failed);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 