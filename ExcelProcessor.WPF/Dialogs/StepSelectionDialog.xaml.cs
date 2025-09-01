using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ExcelProcessor.Models;
using ExcelProcessor.Core.Interfaces;
using ExcelProcessor.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelProcessor.WPF.Dialogs
{
    /// <summary>
    /// StepSelectionDialog.xaml 的交互逻辑
    /// </summary>
    public partial class StepSelectionDialog : Window
    {
        private readonly IExcelConfigService _excelConfigService;
        private readonly ISqlService _sqlService;
        
        public JobStep SelectedStep { get; private set; }
        public bool IsConfirmed { get; private set; }
        
        private List<ExcelConfig> _excelConfigs;
        private List<SqlConfig> _sqlConfigs;
        private StepType _selectedStepType;

        public StepSelectionDialog(IExcelConfigService excelConfigService, ISqlService sqlService)
        {
            InitializeComponent();
            
            _excelConfigService = excelConfigService;
            _sqlService = sqlService;
            
            _excelConfigs = new List<ExcelConfig>();
            _sqlConfigs = new List<SqlConfig>();
            
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                // 加载Excel配置
                var excelConfigs = await _excelConfigService.GetAllConfigsAsync();
                _excelConfigs = excelConfigs.ToList();
                ExcelConfigComboBox.ItemsSource = _excelConfigs;

                // 加载SQL配置
                var sqlConfigs = await _sqlService.GetAllSqlConfigsAsync();
                _sqlConfigs = sqlConfigs.ToList();
                SqlConfigComboBox.ItemsSource = _sqlConfigs;

                // 设置默认选择
                if (_excelConfigs.Any())
                {
                    ExcelConfigComboBox.SelectedIndex = 0;
                }
                
                if (_sqlConfigs.Any())
                {
                    SqlConfigComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExcelImportCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectStepType(StepType.ExcelImport);
        }

        private void SqlExecutionCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectStepType(StepType.SqlExecution);
        }

        private void SelectStepType(StepType stepType)
        {
            _selectedStepType = stepType;

            // 重置选择状态
            ExcelImportCard.BorderBrush = System.Windows.Media.Brushes.Transparent;
            SqlExecutionCard.BorderBrush = System.Windows.Media.Brushes.Transparent;

            // 隐藏所有配置选择区域
            ExcelConfigSection.Visibility = Visibility.Collapsed;
            SqlConfigSection.Visibility = Visibility.Collapsed;

            // 根据选择的类型显示对应的配置选择区域
            switch (stepType)
            {
                case StepType.ExcelImport:
                    ExcelImportCard.BorderBrush = System.Windows.Media.Brushes.Green;
                    ExcelConfigSection.Visibility = Visibility.Visible;
                    UpdateExcelConfigInfo();
                    break;
                    
                case StepType.SqlExecution:
                    SqlExecutionCard.BorderBrush = System.Windows.Media.Brushes.Blue;
                    SqlConfigSection.Visibility = Visibility.Visible;
                    UpdateSqlConfigInfo();
                    break;
            }
        }

        private void ExcelConfigComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateExcelConfigInfo();
        }

        private void SqlConfigComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSqlConfigInfo();
        }

        private void UpdateExcelConfigInfo()
        {
            if (ExcelConfigComboBox.SelectedItem is ExcelConfig config)
            {
                ExcelConfigInfoText.Text = $"配置名称：{config.ConfigName}\n" +
                                          $"文件路径：{config.FilePath}\n" +
                                          $"目标表：{config.TargetTableName}\n" +
                                          $"工作表：{config.SheetName}\n" +
                                          $"状态：{config.Status}";
            }
            else
            {
                ExcelConfigInfoText.Text = "请选择一个Excel配置";
            }
        }

        private void UpdateSqlConfigInfo()
        {
            if (SqlConfigComboBox.SelectedItem is SqlConfig config)
            {
                SqlConfigInfoText.Text = $"SQL名称：{config.Name}\n" +
                                        $"分类：{config.Category}\n" +
                                        $"输出类型：{config.OutputType}\n" +
                                        $"输出目标：{config.OutputTarget}\n" +
                                        $"描述：{config.Description}";
            }
            else
            {
                SqlConfigInfoText.Text = "请选择一个SQL配置";
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedStepType == StepType.ExcelImport && ExcelConfigComboBox.SelectedItem == null)
                {
                    MessageBox.Show("请先选择步骤类型", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 根据步骤类型创建对应的JobStep
                switch (_selectedStepType)
                {
                    case StepType.ExcelImport:
                        if (ExcelConfigComboBox.SelectedItem is ExcelConfig excelConfig)
                        {
                            SelectedStep = CreateExcelImportStep(excelConfig);
                        }
                        else
                        {
                            MessageBox.Show("请选择一个Excel配置", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        break;

                    case StepType.SqlExecution:
                        if (SqlConfigComboBox.SelectedItem is SqlConfig sqlConfig)
                        {
                            SelectedStep = CreateSqlExecutionStep(sqlConfig);
                        }
                        else
                        {
                            MessageBox.Show("请选择一个SQL配置", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        break;

                    default:
                        MessageBox.Show("不支持的步骤类型", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                }

                IsConfirmed = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建步骤失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private JobStep CreateExcelImportStep(ExcelConfig excelConfig)
        {
            return new JobStep
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Excel导入 - {excelConfig.ConfigName}",
                Description = $"导入Excel文件：{excelConfig.Description}",
                Type = StepType.ExcelImport,
                OrderIndex = 1, // 将在添加时重新设置
                IsEnabled = true,
                ExcelConfigId = excelConfig.Id.ToString(),
                TimeoutSeconds = 300,
                RetryCount = 1,
                RetryIntervalSeconds = 60,
                ContinueOnFailure = false,
                Dependencies = JsonSerializer.Serialize(new List<string>()),
                ConditionExpression = string.Empty
            };
        }

        private JobStep CreateSqlExecutionStep(SqlConfig sqlConfig)
        {
            return new JobStep
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"SQL执行 - {sqlConfig.Name}",
                Description = $"执行SQL查询：{sqlConfig.Description}",
                Type = StepType.SqlExecution,
                OrderIndex = 1, // 将在添加时重新设置
                IsEnabled = true,
                SqlConfigId = sqlConfig.Id.ToString(),
                TimeoutSeconds = 300,
                RetryCount = 1,
                RetryIntervalSeconds = 60,
                ContinueOnFailure = false,
                Dependencies = JsonSerializer.Serialize(new List<string>()),
                ConditionExpression = string.Empty
            };
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 