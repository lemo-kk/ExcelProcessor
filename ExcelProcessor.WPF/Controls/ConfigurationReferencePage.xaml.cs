using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Core.Interfaces;
using ExcelProcessor.Models;

namespace ExcelProcessor.WPF.Controls
{
    /// <summary>
    /// 配置引用管理页面
    /// </summary>
    public partial class ConfigurationReferencePage : UserControl
    {
        private readonly IConfigurationReferenceService _referenceService;
        private readonly IExcelConfigService _excelConfigService;
        private readonly ISqlService _sqlService;
        private readonly IDataSourceService _dataSourceService;
        
        private ConfigurationReference? _currentReference;
        private bool _isEditMode = false;

        public ConfigurationReferencePage(
            IConfigurationReferenceService referenceService,
            IExcelConfigService excelConfigService,
            ISqlService sqlService,
            IDataSourceService dataSourceService)
        {
            InitializeComponent();
            
            _referenceService = referenceService;
            _excelConfigService = excelConfigService;
            _sqlService = sqlService;
            _dataSourceService = dataSourceService;

            InitializeControls();
            LoadReferencesAsync();
        }

        private void InitializeControls()
        {
            // 初始化引用类型下拉框
            ReferenceTypeComboBox.ItemsSource = Enum.GetValues(typeof(ReferenceType));
            ReferenceTypeComboBox.SelectedIndex = 0;

            // 设置默认覆盖参数
            OverrideParametersTextBox.Text = "{\n  \n}";
        }

        private async void LoadReferencesAsync()
        {
            try
            {
                var references = await _referenceService.GetAllReferencesAsync();
                ReferencesDataGrid.ItemsSource = references;
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"加载配置引用失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void NewReferenceButton_Click(object sender, RoutedEventArgs e)
        {
            _isEditMode = false;
            _currentReference = null;
            DialogTitle.Text = "新建配置引用";
            
            // 清空表单
            ReferenceNameTextBox.Text = string.Empty;
            ReferenceDescriptionTextBox.Text = string.Empty;
            ReferenceTypeComboBox.SelectedIndex = 0;
            ReferencedConfigComboBox.ItemsSource = null;
            OverrideParametersTextBox.Text = "{\n  \n}";
            IsEnabledCheckBox.IsChecked = true;

            ReferenceDialog.IsOpen = true;
        }

        private async void EditReferenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReference == null)
            {
                Extensions.MessageBoxExtensions.Show("请先选择一个配置引用", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _isEditMode = true;
            DialogTitle.Text = "编辑配置引用";

            // 填充表单
            ReferenceNameTextBox.Text = _currentReference.Name;
            ReferenceDescriptionTextBox.Text = _currentReference.Description;
            ReferenceTypeComboBox.SelectedItem = _currentReference.Type;
            
            // 加载引用的配置列表
            await LoadReferencedConfigsAsync(_currentReference.Type);
            
            // 设置选中的配置
            var selectedConfig = ReferencedConfigComboBox.ItemsSource?.Cast<object>()
                .FirstOrDefault(x => x.ToString().Contains(_currentReference.ReferencedConfigId));
            if (selectedConfig != null)
            {
                ReferencedConfigComboBox.SelectedItem = selectedConfig;
            }

            OverrideParametersTextBox.Text = _currentReference.OverrideParameters ?? "{\n  \n}";
            IsEnabledCheckBox.IsChecked = _currentReference.IsEnabled;

            ReferenceDialog.IsOpen = true;
        }

        private async void DeleteReferenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReference == null)
            {
                Extensions.MessageBoxExtensions.Show("请先选择一个配置引用", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = Extensions.MessageBoxExtensions.Show($"确定要删除配置引用 '{_currentReference.Name}' 吗？", 
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var (success, message) = await _referenceService.DeleteReferenceAsync(_currentReference.Id);
                    if (success)
                    {
                        Extensions.MessageBoxExtensions.Show("删除成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadReferencesAsync();
                    }
                    else
                    {
                        Extensions.MessageBoxExtensions.Show($"删除失败: {message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    Extensions.MessageBoxExtensions.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExecuteReferenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReference == null)
            {
                Extensions.MessageBoxExtensions.Show("请先选择一个配置引用", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var result = await _referenceService.ExecuteReferenceAsync(_currentReference.Id);
                if (result.IsSuccess)
                {
                    var message = $"执行成功！\n执行时间: {result.ExecutionTimeSeconds:F2}秒\n";
                    if (result.ExecutionLogs.Any())
                    {
                        message += "\n执行日志:\n" + string.Join("\n", result.ExecutionLogs);
                    }
                    
                    Extensions.MessageBoxExtensions.Show(message, "执行结果", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Extensions.MessageBoxExtensions.Show($"执行失败: {result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"执行失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadReferencesAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var keyword = SearchTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                LoadReferencesAsync();
                return;
            }

            try
            {
                var references = await _referenceService.SearchReferencesAsync(keyword);
                ReferencesDataGrid.ItemsSource = references;
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"搜索失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReferencesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentReference = ReferencesDataGrid.SelectedItem as ConfigurationReference;
            
            // 更新按钮状态
            EditReferenceButton.IsEnabled = _currentReference != null;
            DeleteReferenceButton.IsEnabled = _currentReference != null;
            ExecuteReferenceButton.IsEnabled = _currentReference != null;
        }

        private async void ReferenceTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReferenceTypeComboBox.SelectedItem is ReferenceType selectedType)
            {
                await LoadReferencedConfigsAsync(selectedType);
            }
        }

        private async Task LoadReferencedConfigsAsync(ReferenceType referenceType)
        {
            try
            {
                switch (referenceType)
                {
                    case ReferenceType.ExcelConfig:
                        var excelConfigs = await _excelConfigService.GetAllConfigsAsync();
                        ReferencedConfigComboBox.ItemsSource = excelConfigs.Select(c => new
                        {
                            Id = c.Id.ToString(),
                            Name = c.ConfigName,
                            DisplayText = $"{c.ConfigName} ({c.FilePath})"
                        });
                        break;

                    case ReferenceType.SqlConfig:
                        var sqlConfigs = await _sqlService.GetAllSqlConfigsAsync();
                        ReferencedConfigComboBox.ItemsSource = sqlConfigs.Select(c => new
                        {
                            Id = c.Id,
                            Name = c.Name,
                            DisplayText = $"{c.Name} ({c.Category})"
                        });
                        break;

                    case ReferenceType.DataSourceConfig:
                        var dataSourceConfigs = await _dataSourceService.GetAllDataSourcesAsync();
                        ReferencedConfigComboBox.ItemsSource = dataSourceConfigs.Select(c => new
                        {
                            Id = c.Id,
                            Name = c.Name,
                            DisplayText = $"{c.Name} ({c.Type})"
                        });
                        break;
                }

                ReferencedConfigComboBox.DisplayMemberPath = "DisplayText";
                ReferencedConfigComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"加载配置列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证表单
                if (string.IsNullOrWhiteSpace(ReferenceNameTextBox.Text))
                {
                    Extensions.MessageBoxExtensions.Show("请输入引用名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (ReferenceTypeComboBox.SelectedItem == null)
                {
                    Extensions.MessageBoxExtensions.Show("请选择引用类型", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (ReferencedConfigComboBox.SelectedItem == null)
                {
                    Extensions.MessageBoxExtensions.Show("请选择引用的配置", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 验证JSON格式
                if (!string.IsNullOrWhiteSpace(OverrideParametersTextBox.Text))
                {
                    try
                    {
                        JsonSerializer.Deserialize<Dictionary<string, object>>(OverrideParametersTextBox.Text);
                    }
                    catch
                    {
                        Extensions.MessageBoxExtensions.Show("覆盖参数格式不正确，请输入有效的JSON", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // 创建或更新配置引用
                var reference = _currentReference ?? new ConfigurationReference();
                reference.Name = ReferenceNameTextBox.Text.Trim();
                reference.Description = ReferenceDescriptionTextBox.Text.Trim();
                reference.Type = (ReferenceType)ReferenceTypeComboBox.SelectedItem;
                reference.OverrideParameters = OverrideParametersTextBox.Text.Trim();
                reference.IsEnabled = IsEnabledCheckBox.IsChecked ?? true;

                // 获取选中的配置
                var selectedConfig = ReferencedConfigComboBox.SelectedItem;
                var configIdProperty = selectedConfig.GetType().GetProperty("Id");
                var configNameProperty = selectedConfig.GetType().GetProperty("Name");
                
                if (configIdProperty != null && configNameProperty != null)
                {
                    reference.ReferencedConfigId = configIdProperty.GetValue(selectedConfig)?.ToString() ?? string.Empty;
                    reference.ReferencedConfigName = configNameProperty.GetValue(selectedConfig)?.ToString() ?? string.Empty;
                }

                bool success;
                string message;

                if (_isEditMode)
                {
                    reference.UpdatedAt = DateTime.Now;
                    reference.UpdatedBy = "当前用户"; // 这里应该获取实际的当前用户
                    (success, message) = await _referenceService.UpdateReferenceAsync(reference);
                }
                else
                {
                    reference.CreatedAt = DateTime.Now;
                    reference.UpdatedAt = DateTime.Now;
                    reference.CreatedBy = "当前用户"; // 这里应该获取实际的当前用户
                    reference.UpdatedBy = "当前用户";
                    (success, message) = await _referenceService.CreateReferenceAsync(reference);
                }

                if (success)
                {
                    Extensions.MessageBoxExtensions.Show(_isEditMode ? "更新成功" : "创建成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    ReferenceDialog.IsOpen = false;
                    LoadReferencesAsync();
                }
                else
                {
                    Extensions.MessageBoxExtensions.Show($"操作失败: {message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ReferenceDialog.IsOpen = false;
        }
    }
} 