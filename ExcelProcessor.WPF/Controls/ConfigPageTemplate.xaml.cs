using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.WPF.Controls
{
    public partial class ConfigPageTemplate : UserControl
    {
        private readonly ILogger<ConfigPageTemplate> _logger;
        private List<CustomConfigItem> _customConfigItems;

        public ConfigPageTemplate()
        {
            InitializeComponent();
            
            // 初始化日志
            var loggerFactory = LoggerFactory.Create(builder => 
                builder.AddConsole().AddDebug());
            _logger = loggerFactory.CreateLogger<ConfigPageTemplate>();
            
            InitializeData();
            SetupEventHandlers();
        }

        private void InitializeData()
        {
            _customConfigItems = new List<CustomConfigItem>();

            // 初始化配置类型下拉框
            ConfigTypeComboBox.Items.Clear();
            ConfigTypeComboBox.Items.Add("数据库配置");
            ConfigTypeComboBox.Items.Add("网络配置");
            ConfigTypeComboBox.Items.Add("文件配置");
            ConfigTypeComboBox.Items.Add("系统配置");
            ConfigTypeComboBox.Items.Add("自定义配置");
            ConfigTypeComboBox.SelectedIndex = 0;

            // 设置默认值
            ConfigNameTextBox.Text = "默认配置";
            DescriptionTextBox.Text = "这是一个默认的配置模板";
            TimeoutTextBox.Text = "30";
            RetryCountTextBox.Text = "3";
            MaxConcurrencyTextBox.Text = "5";
            BatchSizeTextBox.Text = "1000";

            _logger.LogInformation("配置页面模板数据初始化完成");
        }

        private void SetupEventHandlers()
        {
            // 这里可以添加各种事件处理器
            // 例如：按钮点击、文本变化等
        }

        /// <summary>
        /// 获取配置数据
        /// </summary>
        public ConfigData GetConfigData()
        {
            return new ConfigData
            {
                Name = ConfigNameTextBox.Text,
                Type = ConfigTypeComboBox.SelectedItem?.ToString(),
                Description = DescriptionTextBox.Text,
                IsEnabled = IsEnabledCheckBox.IsChecked ?? false,
                IsDefault = IsDefaultCheckBox.IsChecked ?? false,
                AutoSave = AutoSaveCheckBox.IsChecked ?? false,
                Timeout = int.TryParse(TimeoutTextBox.Text, out int timeout) ? timeout : 30,
                RetryCount = int.TryParse(RetryCountTextBox.Text, out int retryCount) ? retryCount : 3,
                MaxConcurrency = int.TryParse(MaxConcurrencyTextBox.Text, out int maxConcurrency) ? maxConcurrency : 5,
                BatchSize = int.TryParse(BatchSizeTextBox.Text, out int batchSize) ? batchSize : 1000,
                ValidationRules = ValidationRulesTextBox.Text,
                CustomItems = _customConfigItems
            };
        }

        /// <summary>
        /// 设置配置数据
        /// </summary>
        public void SetConfigData(ConfigData configData)
        {
            if (configData == null) return;

            ConfigNameTextBox.Text = configData.Name ?? "";
            ConfigTypeComboBox.SelectedItem = configData.Type;
            DescriptionTextBox.Text = configData.Description ?? "";
            IsEnabledCheckBox.IsChecked = configData.IsEnabled;
            IsDefaultCheckBox.IsChecked = configData.IsDefault;
            AutoSaveCheckBox.IsChecked = configData.AutoSave;
            TimeoutTextBox.Text = configData.Timeout.ToString();
            RetryCountTextBox.Text = configData.RetryCount.ToString();
            MaxConcurrencyTextBox.Text = configData.MaxConcurrency.ToString();
            BatchSizeTextBox.Text = configData.BatchSize.ToString();
            ValidationRulesTextBox.Text = configData.ValidationRules ?? "";

            _customConfigItems.Clear();
            if (configData.CustomItems != null)
            {
                _customConfigItems.AddRange(configData.CustomItems);
            }
            RefreshCustomConfigPanel();
        }

        /// <summary>
        /// 验证配置数据
        /// </summary>
        public bool ValidateConfig()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ConfigNameTextBox.Text))
            {
                errors.Add("配置名称不能为空");
            }

            if (ConfigTypeComboBox.SelectedItem == null)
            {
                errors.Add("请选择配置类型");
            }

            if (!int.TryParse(TimeoutTextBox.Text, out int timeout) || timeout <= 0)
            {
                errors.Add("超时时间必须是大于0的整数");
            }

            if (!int.TryParse(RetryCountTextBox.Text, out int retryCount) || retryCount < 0)
            {
                errors.Add("重试次数必须是非负整数");
            }

            if (!int.TryParse(MaxConcurrencyTextBox.Text, out int maxConcurrency) || maxConcurrency <= 0)
            {
                errors.Add("最大并发数必须是大于0的整数");
            }

            if (!int.TryParse(BatchSizeTextBox.Text, out int batchSize) || batchSize <= 0)
            {
                errors.Add("批处理大小必须是大于0的整数");
            }

            if (errors.Count > 0)
            {
                Extensions.MessageBoxExtensions.Show($"配置验证失败：\n{string.Join("\n", errors)}", "验证错误", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 清空配置数据
        /// </summary>
        public void ClearConfig()
        {
            var result = Extensions.MessageBoxExtensions.Show("确定要清空所有配置吗？此操作不可撤销。", "确认清空", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                ConfigNameTextBox.Text = "";
                ConfigTypeComboBox.SelectedIndex = 0;
                DescriptionTextBox.Text = "";
                IsEnabledCheckBox.IsChecked = true;
                IsDefaultCheckBox.IsChecked = false;
                AutoSaveCheckBox.IsChecked = true;
                TimeoutTextBox.Text = "30";
                RetryCountTextBox.Text = "3";
                MaxConcurrencyTextBox.Text = "5";
                BatchSizeTextBox.Text = "1000";
                ValidationRulesTextBox.Text = "";
                
                _customConfigItems.Clear();
                RefreshCustomConfigPanel();
                
                _logger.LogInformation("配置数据已清空");
            }
        }

        /// <summary>
        /// 添加自定义配置项
        /// </summary>
        private void AddCustomConfigItem()
        {
            var item = new CustomConfigItem
            {
                Name = $"自定义参数{_customConfigItems.Count + 1}",
                Value = $"custom_value_{_customConfigItems.Count + 1}"
            };
            
            _customConfigItems.Add(item);
            RefreshCustomConfigPanel();
            
            _logger.LogInformation("添加自定义配置项: {ItemName}", item.Name);
        }

        /// <summary>
        /// 删除自定义配置项
        /// </summary>
        private void RemoveCustomConfigItem(CustomConfigItem item)
        {
            _customConfigItems.Remove(item);
            RefreshCustomConfigPanel();
            
            _logger.LogInformation("删除自定义配置项: {ItemName}", item.Name);
        }

        /// <summary>
        /// 刷新自定义配置面板
        /// </summary>
        private void RefreshCustomConfigPanel()
        {
            CustomConfigPanel.Children.Clear();

            foreach (var item in _customConfigItems)
            {
                var container = CreateCustomConfigItemContainer(item);
                CustomConfigPanel.Children.Add(container);
            }
        }

        /// <summary>
        /// 创建自定义配置项容器
        /// </summary>
        private Border CreateCustomConfigItemContainer(CustomConfigItem item)
        {
            var container = new Border
            {
                Style = FindResource("ConfigItemContainerStyle") as Style
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 标题
            var titleBlock = new TextBlock
            {
                Text = item.Name,
                Style = FindResource("ConfigItemLabelStyle") as Style
            };
            Grid.SetRow(titleBlock, 0);
            Grid.SetColumn(titleBlock, 0);
            grid.Children.Add(titleBlock);

            // 删除按钮
            var deleteButton = new Button
            {
                Content = "删除",
                Style = FindResource("ConfigButtonStyle") as Style,
                Background = FindResource("DangerColor") as System.Windows.Media.Brush,
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 10,
                Height = 24
            };
            deleteButton.Click += (s, e) => RemoveCustomConfigItem(item);
            Grid.SetRow(deleteButton, 0);
            Grid.SetColumn(deleteButton, 1);
            grid.Children.Add(deleteButton);

            // 值输入框
            var valueTextBox = new TextBox
            {
                Text = item.Value,
                Style = FindResource("ConfigInputStyle") as Style
            };
            MaterialDesignThemes.Wpf.HintAssist.SetHint(valueTextBox, "参数值");
            valueTextBox.TextChanged += (s, e) => item.Value = valueTextBox.Text;
            Grid.SetRow(valueTextBox, 1);
            Grid.SetColumn(valueTextBox, 0);
            Grid.SetColumnSpan(valueTextBox, 2);
            grid.Children.Add(valueTextBox);

            container.Child = grid;
            return container;
        }
    }

    /// <summary>
    /// 配置数据模型
    /// </summary>
    public class ConfigData
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsDefault { get; set; }
        public bool AutoSave { get; set; }
        public int Timeout { get; set; }
        public int RetryCount { get; set; }
        public int MaxConcurrency { get; set; }
        public int BatchSize { get; set; }
        public string ValidationRules { get; set; }
        public List<CustomConfigItem> CustomItems { get; set; } = new List<CustomConfigItem>();
    }

    /// <summary>
    /// 自定义配置项模型
    /// </summary>
    public class CustomConfigItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
} 