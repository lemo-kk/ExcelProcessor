using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ExcelProcessor.Core.Interfaces;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Models;
using System.Threading.Tasks;
using ExcelProcessor.WPF.Controls;
using ExcelProcessor.WPF.Dialogs;

namespace ExcelProcessor.WPF.Controls
{
    /// <summary>
    /// SqlManagementPage.xaml 的交互逻辑
    /// </summary>
    public partial class SqlManagementPage : System.Windows.Controls.UserControl
    {
        private readonly ILogger<SqlManagementPage> _logger;
        private readonly ISqlService _sqlService;
        private readonly IDataSourceService _dataSourceService;
        private readonly IDatabaseTableService _databaseTableService;
        private readonly string _connectionString;
        private ObservableCollection<SqlItem> _sqlItems;
        private ICollectionView _sqlItemsView;
        private SqlItem _currentSqlItem;
        private List<string> _dataSources;
        private List<string> _tableNames = new List<string>();
        private List<string> _filteredTableNames = new List<string>();
        private string _currentDataSourceName = string.Empty;
        private List<SqlParameter> _parameters = new List<SqlParameter>();

        public SqlManagementPage(ISqlService sqlService, IDataSourceService dataSourceService, IDatabaseTableService databaseTableService, ILogger<SqlManagementPage> logger, string connectionString)
        {
            InitializeComponent();
            
            _sqlService = sqlService;
            _dataSourceService = dataSourceService;
            _databaseTableService = databaseTableService;
            _logger = logger;
            _connectionString = connectionString ?? "Data Source=./data/ExcelProcessor.db;";
            
            _ = InitializeAsync();
            SetupEventHandlers();
        }

        private async Task InitializeAsync()
        {
            try
            {
                // 初始化SQL列表
                _sqlItems = new ObservableCollection<SqlItem>();
                
                // 从后端服务获取SQL配置
                var sqlConfigs = await _sqlService.GetAllSqlConfigsAsync();
                
                foreach (var config in sqlConfigs)
                {
                    _sqlItems.Add(new SqlItem
                    {
                        Id = config.Id,
                        Name = config.Name,
                        Category = config.Category,
                        OutputType = config.OutputType,
                        OutputTarget = config.OutputTarget,
                        Description = config.Description,
                        SqlStatement = config.SqlStatement,
                        CreatedDate = config.CreatedDate,
                        LastModified = config.LastModified
                    });
                }

                // 设置数据源
                SqlListDataGrid.ItemsSource = _sqlItems;
                _sqlItemsView = CollectionViewSource.GetDefaultView(_sqlItems);

                // 初始化输出类型下拉框
                OutputTypeComboBox.SelectedIndex = 0;
                
                // 初始化清空表选项（默认不选中）
                ClearTableCheckBox.IsChecked = false;

                // 初始化数据源列表
                await InitializeDataSources();

                _logger.LogInformation("SQL管理页面数据初始化完成，共加载 {Count} 个SQL配置", sqlConfigs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化SQL数据失败");
                MessageBox.Show($"初始化SQL数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task InitializeDataSources()
        {
            try
            {
                // 从数据源服务获取所有数据源
                var dataSourceConfigs = await _dataSourceService.GetAllDataSourcesAsync();
                
                // 初始化数据源列表
                _dataSources = new List<string>();
                
                // 添加所有数据源名称
                foreach (var dataSource in dataSourceConfigs)
                {
                    _dataSources.Add(dataSource.Name);
                }
                
                // 如果没有数据源，添加默认选项
                if (_dataSources.Count == 0)
                {
                    _dataSources.Add("默认数据源");
                }
                
                // 设置查询数据源下拉框
                QueryDataSourceComboBox.ItemsSource = _dataSources;
                
                // 设置数据源到下拉框
                DataSourceComboBox.ItemsSource = _dataSources;
                QueryDataSourceComboBox.ItemsSource = _dataSources;
                // OutputDataSourceComboBox已隐藏，不再设置
                
                // 默认选中默认数据源（如果存在）
                var defaultDataSource = dataSourceConfigs.FirstOrDefault(ds => ds.IsDefault);
                if (defaultDataSource != null)
                {
                    DataSourceComboBox.SelectedItem = defaultDataSource.Name;
                    _currentDataSourceName = defaultDataSource.Name;
                    // 立即加载该数据源的表名
                    await LoadTableNamesAsync(defaultDataSource.Name);
                }
                else
                {
                    // 如果没有默认数据源，选择第一个
                    DataSourceComboBox.SelectedIndex = 0;
                    if (_dataSources.Count > 0)
                    {
                        _currentDataSourceName = _dataSources[0];
                        // 立即加载该数据源的表名
                        await LoadTableNamesAsync(_dataSources[0]);
                    }
                }

                _logger.LogInformation("数据源初始化完成，共加载 {Count} 个数据源", _dataSources.Count);
            }
            catch (Exception ex)
            {
                // 如果获取数据源失败，使用默认列表
                _dataSources = new List<string> { "默认数据源", "SQLite数据库", "MySQL数据库", "PostgreSQL数据库" };
                DataSourceComboBox.ItemsSource = _dataSources;
                DataSourceComboBox.SelectedIndex = 0;
                _currentDataSourceName = _dataSources[0];
                
                // 尝试加载默认数据源的表名
                try
                {
                    await LoadTableNamesAsync(_dataSources[0]);
                }
                catch (Exception loadEx)
                {
                    _logger.LogError(loadEx, "加载默认数据源表名失败");
                    // 添加一些示例表名
                    _tableNames = new List<string> { "users", "products", "orders", "categories" };
                    DataTableNameComboBox.ItemsSource = _tableNames;
                }
                
                _logger.LogError(ex, "初始化数据源失败，使用默认列表");
            }
        }

        /// <summary>
        /// 设置事件处理程序
        /// </summary>
        private void SetupEventHandlers()
        {
            try
            {
                // SQL列表选择改变事件
                SqlListDataGrid.SelectionChanged += SqlListDataGrid_SelectionChanged;

                // 数据源选择改变事件
                DataSourceComboBox.SelectionChanged += DataSourceComboBox_SelectionChanged;
                
                // 查询数据源选择改变事件
                QueryDataSourceComboBox.SelectionChanged += QueryDataSourceComboBox_SelectionChanged;

                // 输出数据源选择改变事件 - 已隐藏，不再需要
                // OutputDataSourceComboBox.SelectionChanged += OutputDataSourceComboBox_SelectionChanged;

                // 输出类型选择改变事件
                OutputTypeComboBox.SelectionChanged += OutputTypeComboBox_SelectionChanged;

                // 数据表名称选择改变事件
                DataTableNameComboBox.SelectionChanged += DataTableNameComboBox_SelectionChanged;
                DataTableNameComboBox.DropDownOpened += DataTableNameComboBox_DropDownOpened;
                DataTableNameComboBox.PreviewKeyDown += DataTableNameComboBox_PreviewKeyDown;

                // 输出路径文本框文本改变事件
                OutputPathTextBox.TextChanged += OutputPathTextBox_TextChanged;
                ExcelFileNameTextBox.TextChanged += ExcelFileNameTextBox_TextChanged;
                SheetNameTextBox.TextChanged += SheetNameTextBox_TextChanged;

                // 添加分类按钮事件
                AddCategoryButton.Click += AddCategoryButton_Click;

                // 路径选择按钮事件
                PathSelectButton.Click += PathSelectButton_Click;

                // 以下按钮事件已在XAML中绑定，无需重复绑定
                // SaveSqlButton.Click += SaveSqlButton_Click;
                // DeleteSqlButton.Click += DeleteSqlButton_Click;
                // TestSqlButton.Click += TestSqlButton_Click;
                // AddParameterButton.Click += AddParameterButton_Click;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "设置事件处理失败");
            }
        }

        /// <summary>
        /// 查找可视化子元素
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void SetupCategoryFilterHandlers()
        {
            // 为分类标签添加点击事件
            foreach (var child in CategoryTagsPanel.Children)
            {
                if (child is Border border)
                {
                    border.MouseLeftButtonDown += CategoryTag_MouseLeftButtonDown;
                }
            }
        }

        private void CategoryTag_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Child is TextBlock textBlock)
            {
                var category = textBlock.Text;
                FilterByCategory(category);
                
                // 更新选中状态
                UpdateCategorySelection(border);
            }
        }

        private void FilterByCategory(string category)
        {
            if (category == "全部")
            {
                _sqlItemsView.Filter = null;
            }
            else
            {
                _sqlItemsView.Filter = item =>
                {
                    if (item is SqlItem sqlItem)
                    {
                        return sqlItem.Category == category;
                    }
                    return false;
                };
            }
        }

        private void UpdateCategorySelection(Border selectedBorder)
        {
            // 重置所有标签样式
            foreach (var child in CategoryTagsPanel.Children)
            {
                if (child is Border border)
                {
                    border.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2A2A2A"));
                    if (border.Child is TextBlock childTextBlock)
                    {
                        childTextBlock.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CCCCCC"));
                    }
                }
            }
            
            // 设置选中标签样式
            selectedBorder.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2196F3"));
            if (selectedBorder.Child is TextBlock selectedTextBlock)
            {
                selectedTextBlock.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text?.ToLower();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _sqlItemsView.Filter = null;
            }
            else
            {
                _sqlItemsView.Filter = item =>
                {
                    if (item is SqlItem sqlItem)
                    {
                        return sqlItem.Name.ToLower().Contains(searchText) ||
                               sqlItem.Description.ToLower().Contains(searchText) ||
                               sqlItem.Category.ToLower().Contains(searchText);
                    }
                    return false;
                };
            }
        }

        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建输入对话框
                var inputDialog = new Window
                {
                    Title = "添加新分类",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    ResizeMode = ResizeMode.NoResize,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 42, 42)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64)),
                    BorderThickness = new Thickness(1)
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                // 分类名称输入框
                var textBox = new TextBox
                {
                    Margin = new Thickness(10),
                    FontSize = 14,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26)),
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64)),
                    CaretBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 150, 255))
                };
                Grid.SetRow(textBox, 0);
                grid.Children.Add(textBox);

                // 按钮面板
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(10)
                };
                Grid.SetRow(buttonPanel, 2);

                var okButton = new Button
                {
                    Content = "确定",
                    Width = 60,
                    Height = 30,
                    Margin = new Thickness(5, 0, 0, 0),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 150, 255)),
                    Foreground = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(0)
                };

                var cancelButton = new Button
                {
                    Content = "取消",
                    Width = 60,
                    Height = 30,
                    Margin = new Thickness(5, 0, 0, 0),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64)),
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(0)
                };

                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);
                grid.Children.Add(buttonPanel);

                inputDialog.Content = grid;

                string newCategory = null;

                // 确定按钮事件
                okButton.Click += (s, args) =>
                {
                    newCategory = textBox.Text?.Trim();
                    if (string.IsNullOrEmpty(newCategory))
                    {
                        MessageBox.Show("请输入分类名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 检查分类是否已存在
                    var existingCategories = GetExistingCategories();
                    if (existingCategories.Contains(newCategory))
                    {
                        MessageBox.Show("该分类已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    inputDialog.DialogResult = true;
                    inputDialog.Close();
                };

                // 取消按钮事件
                cancelButton.Click += (s, args) =>
                {
                    inputDialog.DialogResult = false;
                    inputDialog.Close();
                };

                // 回车键确认
                textBox.KeyDown += (s, args) =>
                {
                    if (args.Key == System.Windows.Input.Key.Enter)
                    {
                        okButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                };

                // 显示对话框
                if (inputDialog.ShowDialog() == true && !string.IsNullOrEmpty(newCategory))
                {
                    // 添加新分类到分类标签面板
                    AddCategoryTag(newCategory);
                    
                    // 添加新分类到分类下拉框
                    AddCategoryToComboBox(newCategory);
                    
                    _logger.LogInformation("成功添加新分类: {Category}", newCategory);
                    MessageBox.Show($"成功添加分类：{newCategory}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加新分类失败");
                MessageBox.Show($"添加新分类失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 获取现有的分类列表
        /// </summary>
        private List<string> GetExistingCategories()
        {
            var categories = new List<string>();
            
            // 从分类标签面板获取
            foreach (var child in CategoryTagsPanel.Children)
            {
                if (child is Border border && border.Child is TextBlock textBlock)
                {
                    var category = textBlock.Text;
                    if (category != "全部" && !categories.Contains(category))
                    {
                        categories.Add(category);
                    }
                }
            }
            
            return categories;
        }

        /// <summary>
        /// 添加分类标签到面板
        /// </summary>
        private void AddCategoryTag(string category)
        {
            var border = new Border
            {
                Style = FindResource("CategoryTagStyle") as Style,
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var textBlock = new TextBlock
            {
                Text = category,
                Style = FindResource("CategoryTagTextStyle") as Style,
                Padding = new Thickness(12, 6, 12, 6)
            };

            border.Child = textBlock;
            border.MouseLeftButtonDown += CategoryTag_MouseLeftButtonDown;
            
            CategoryTagsPanel.Children.Add(border);
        }

        /// <summary>
        /// 添加分类到下拉框
        /// </summary>
        private void AddCategoryToComboBox(string category)
        {
            if (CategoryComboBox != null)
            {
                var comboBoxItem = new ComboBoxItem { Content = category };
                CategoryComboBox.Items.Add(comboBoxItem);
            }
        }

        /// <summary>
        /// 输出类型选择改变事件
        /// </summary>
        private void OutputTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedItem = OutputTypeComboBox?.SelectedItem as ComboBoxItem;
                var outputType = selectedItem?.Content?.ToString() ?? "数据表";

                // 更新测试输出按钮文本
                UpdateTestOutputButtonText(outputType);

                // 显示/隐藏相关面板
                if (outputType == "数据表")
                {
                    OutputDataSourcePanel.Visibility = Visibility.Visible;
                    DataTablePanel.Visibility = Visibility.Visible;
                    DataTableNamePanel.Visibility = Visibility.Visible;
                    ClearTablePanel.Visibility = Visibility.Visible;
                    ClearSheetPanel.Visibility = Visibility.Collapsed;
                    ExcelPathPanel.Visibility = Visibility.Collapsed;
                    ExcelFileNamePanel.Visibility = Visibility.Collapsed;
                    SheetNamePanel.Visibility = Visibility.Collapsed;
                    OutputTargetPanel.Visibility = Visibility.Collapsed;
                }
                else if (outputType == "Excel工作表")
                {
                    OutputDataSourcePanel.Visibility = Visibility.Collapsed;
                    DataTablePanel.Visibility = Visibility.Collapsed;
                    DataTableNamePanel.Visibility = Visibility.Collapsed;
                    ClearTablePanel.Visibility = Visibility.Collapsed;
                    ClearSheetPanel.Visibility = Visibility.Visible;
                    ExcelPathPanel.Visibility = Visibility.Visible;
                    ExcelFileNamePanel.Visibility = Visibility.Visible;
                    SheetNamePanel.Visibility = Visibility.Visible;
                    OutputTargetPanel.Visibility = Visibility.Visible;
                }

                _logger.LogInformation("输出类型已更改为: {OutputType}", outputType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "输出类型选择改变事件处理失败");
            }
        }

        /// <summary>
        /// 更新测试输出按钮文本
        /// </summary>
        private void UpdateTestOutputButtonText(string outputType)
        {
            try
            {
                if (TestOutputButton != null)
                {
                    if (outputType == "数据表")
                    {
                        TestOutputButton.Content = "测试输出到表";
                    }
                    else if (outputType == "Excel工作表")
                    {
                        TestOutputButton.Content = "测试输出到工作表";
                    }
                    else
                    {
                        TestOutputButton.Content = "测试输出格式";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新测试输出按钮文本失败");
            }
        }

        private void ExcelFileNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateOutputTarget();
        }

        private void SheetNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateOutputTarget();
        }

        private void OutputPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateOutputTarget();
        }

        private void UpdateOutputTarget()
        {
            try
            {
                // 检查控件是否存在
                if (OutputPathTextBox == null || ExcelFileNameTextBox == null || 
                    SheetNameTextBox == null || OutputTargetTextBox == null)
                {
                    return;
                }

                var outputPath = OutputPathTextBox.Text ?? "";
                var fileName = ExcelFileNameTextBox.Text ?? "";
                var sheetName = SheetNameTextBox.Text ?? "";

                if (!string.IsNullOrEmpty(outputPath) && !string.IsNullOrEmpty(fileName))
                {
                    var outputTarget = Path.Combine(outputPath, $"{fileName}.xlsx");
                    if (!string.IsNullOrEmpty(sheetName))
                    {
                        outputTarget += $"!{sheetName}";
                    }
                    OutputTargetTextBox.Text = outputTarget;
                }
                else
                {
                    OutputTargetTextBox.Text = "";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "更新输出目标失败");
                if (OutputTargetTextBox != null)
                {
                    OutputTargetTextBox.Text = "";
                }
            }
        }

        private void PathSelectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "选择Excel文件输出路径",
                    ShowNewFolderButton = true
                };

                // 当当前输出路径为空或目录不存在时，默认定位到 data\\Output\\EXCEL
                try
                {
                    var rawPath = OutputPathTextBox?.Text;
                    var currentPath = string.IsNullOrWhiteSpace(rawPath)
                        ? null
                        : rawPath.Trim().Trim('"').Replace('/', '\\');

                    string defaultPath = System.IO.Path.Combine(AppContext.BaseDirectory ?? string.Empty, "data", "Output", "EXCEL");

                    if (!System.IO.Directory.Exists(defaultPath))
                    {
                        System.IO.Directory.CreateDirectory(defaultPath);
                    }

                    // 仅当路径存在且可被 FolderBrowserDialog 接受时再进行预选
                    bool assigned = false;

                    if (!string.IsNullOrWhiteSpace(currentPath))
                    {
                        try
                        {
                            string fullCurrentPath = System.IO.Path.GetFullPath(currentPath);
                            if (System.IO.Directory.Exists(fullCurrentPath))
                            {
                                dialog.SelectedPath = fullCurrentPath;
                                assigned = true;
                            }
                        }
                        catch (Exception exCur)
                        {
                            _logger?.LogWarning(exCur, "预设当前输出路径失败：{Path}", currentPath);
                        }
                    }

                    if (!assigned)
                    {
                        try
                        {
                            string fullDefaultPath = System.IO.Path.GetFullPath(defaultPath);
                            if (System.IO.Directory.Exists(fullDefaultPath))
                            {
                                dialog.SelectedPath = fullDefaultPath;
                            }
                        }
                        catch (Exception exDef)
                        {
                            _logger?.LogWarning(exDef, "预设默认输出路径失败：{Path}", defaultPath);
                        }
                    }
                }
                catch (Exception presetEx)
                {
                    _logger?.LogWarning(presetEx, "初始化默认输出路径失败");
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (OutputPathTextBox != null)
                    {
                        OutputPathTextBox.Text = dialog.SelectedPath;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "选择路径失败");
                MessageBox.Show($"选择路径失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region 数据源和表名相关事件处理

        /// <summary>
        /// 数据源选择变化事件处理
        /// </summary>
        private async void DataSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (DataSourceComboBox.SelectedItem is string selectedDataSource)
                {
                    _currentDataSourceName = selectedDataSource;
                    await LoadTableNamesAsync(selectedDataSource);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据源选择变化处理失败");
                MessageBox.Show($"数据源选择失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 查询数据源选择变化事件处理
        /// </summary>
        private void QueryDataSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (QueryDataSourceComboBox.SelectedItem is string selectedQueryDataSource)
                {
                    _logger.LogInformation("选择了查询数据源: {QueryDataSource}", selectedQueryDataSource);
                    // 这里可以添加查询数据源选择后的逻辑，比如验证连接等
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询数据源选择变化处理失败");
                MessageBox.Show($"查询数据源选择失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 数据表名称选择变化事件处理
        /// </summary>
        private void DataTableNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (DataTableNameComboBox.SelectedItem is string selectedTableName)
                {
                    _logger.LogInformation("选择了数据表: {TableName}", selectedTableName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据表名称选择变化处理失败");
            }
        }

        /// <summary>
        /// 数据表名称下拉框打开事件处理
        /// </summary>
        private void DataTableNameComboBox_DropDownOpened(object sender, EventArgs e)
        {
            try
            {
                // 当下拉框打开时，显示所有表名
                DataTableNameComboBox.ItemsSource = _tableNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据表名称下拉框打开处理失败");
            }
        }

        /// <summary>
        /// 数据表名称键盘事件处理
        /// </summary>
        private async void DataTableNameComboBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                // 当用户按下字母键或数字键时，进行实时搜索
                if (e.Key >= System.Windows.Input.Key.A && e.Key <= System.Windows.Input.Key.Z ||
                    e.Key >= System.Windows.Input.Key.D0 && e.Key <= System.Windows.Input.Key.D9 ||
                    e.Key == System.Windows.Input.Key.Back || e.Key == System.Windows.Input.Key.Delete)
                {
                    // 延迟执行搜索，避免频繁调用
                    await Task.Delay(100);
                    
                    var searchText = DataTableNameComboBox.Text?.Trim();
                    
                    // 如果搜索文本为空，显示所有表名
                    if (string.IsNullOrWhiteSpace(searchText))
                    {
                        DataTableNameComboBox.ItemsSource = _tableNames;
                        return;
                    }
                    
                    // 执行实时搜索
                    await FilterTableNamesAsync(searchText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据表名称键盘事件处理失败");
            }
        }

        /// <summary>
        /// 加载指定数据源的所有表名
        /// </summary>
        private async Task LoadTableNamesAsync(string dataSourceName)
        {
            try
            {
                _tableNames = await _databaseTableService.GetTableNamesAsync(dataSourceName);
                
                // 更新ComboBox的数据源
                DataTableNameComboBox.ItemsSource = _tableNames;
                
                _logger.LogInformation("加载数据源 {DataSourceName} 的表名完成，共 {Count} 个表", dataSourceName, _tableNames.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载表名失败: {DataSourceName}", dataSourceName);
                _tableNames = new List<string>();
                DataTableNameComboBox.ItemsSource = _tableNames;
            }
        }

        /// <summary>
        /// 动态筛选表名
        /// </summary>
        private async Task FilterTableNamesAsync(string searchKeyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentDataSourceName))
                {
                    // 如果没有选择数据源，在现有表名中筛选
                    var filtered = _tableNames
                        .Where(tableName => tableName.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    DataTableNameComboBox.ItemsSource = filtered;
                }
                else
                {
                    // 如果有数据源，调用服务进行搜索
                    var matchedTableNames = await _databaseTableService.SearchTableNamesAsync(_currentDataSourceName, searchKeyword);
                    DataTableNameComboBox.ItemsSource = matchedTableNames;
                }
                
                _logger.LogInformation("筛选表名完成，关键词: {SearchKeyword}, 结果数量: {Count}", 
                    searchKeyword, (DataTableNameComboBox.ItemsSource as List<string>)?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "筛选表名失败: {SearchKeyword}", searchKeyword);
                // 如果筛选失败，显示所有表名
                DataTableNameComboBox.ItemsSource = _tableNames;
            }
        }

        /// <summary>
        /// 搜索匹配的表名（保留原有方法以兼容）
        /// </summary>
        private async Task SearchTableNamesAsync(string dataSourceName, string searchKeyword)
        {
            await FilterTableNamesAsync(searchKeyword);
        }

        #endregion

        #region 其他事件处理方法

        private async void SqlListDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SqlListDataGrid.SelectedItem is SqlItem selectedItem)
            {
                _currentSqlItem = selectedItem;
                // 填充表单数据
                await LoadSqlItemToFormAsync(selectedItem);
            }
        }

        /// <summary>
        /// 将选中的SQL项目数据填充到表单
        /// </summary>
        private async Task LoadSqlItemToFormAsync(SqlItem sqlItem)
        {
            try
            {
                // 填充基本信息
                SqlNameTextBox.Text = sqlItem.Name;
                DescriptionTextBox.Text = sqlItem.Description;
                SqlEditor.SqlText = sqlItem.SqlStatement;

                // 设置分类
                SetCategorySelection(sqlItem.Category);

                // 设置输出类型
                SetOutputTypeSelection(sqlItem.OutputType);

                // 设置输出目标
                SetOutputTarget(sqlItem.OutputTarget, sqlItem.OutputType);

                // 加载参数配置（从SQL配置中获取参数JSON）
                var sqlConfig = await _sqlService.GetSqlConfigByIdAsync(sqlItem.Id);
                if (sqlConfig != null)
                {
                    // 加载查询数据源选中项（根据配置的 DataSourceId 回填名称）
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(sqlConfig.DataSourceId))
                        {
                            var dataSourceConfigs = await _dataSourceService.GetAllDataSourcesAsync();
                            var queryDataSource = dataSourceConfigs.FirstOrDefault(ds => ds.Id == sqlConfig.DataSourceId);
                            if (queryDataSource != null)
                            {
                                SetDataSourceSelection(QueryDataSourceComboBox, queryDataSource.Name);
                                _logger.LogInformation("已回填查询数据源: {DataSource}", queryDataSource.Name);
                            }
                        }
                    }
                    catch (Exception dsEx)
                    {
                        _logger.LogError(dsEx, "回填查询数据源失败");
                    }
                    
                    // 加载目标数据源选中项（根据配置的 OutputDataSourceId 回填名称）
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(sqlConfig.OutputDataSourceId))
                        {
                            var dataSourceConfigs = await _dataSourceService.GetAllDataSourcesAsync();
                            var targetDataSource = dataSourceConfigs.FirstOrDefault(ds => ds.Id == sqlConfig.OutputDataSourceId);
                            if (targetDataSource != null)
                            {
                                SetDataSourceSelection(DataSourceComboBox, targetDataSource.Name);
                                _currentDataSourceName = targetDataSource.Name;
                                _logger.LogInformation("已回填目标数据源: {DataSource}", targetDataSource.Name);
                                
                                // 加载该数据源的表名
                                await LoadTableNamesAsync(targetDataSource.Name);
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(sqlConfig.DataSourceId))
                        {
                            // 如果没有输出数据源，使用查询数据源作为目标数据源
                            var dataSourceConfigs = await _dataSourceService.GetAllDataSourcesAsync();
                            var targetDataSource = dataSourceConfigs.FirstOrDefault(ds => ds.Id == sqlConfig.DataSourceId);
                            if (targetDataSource != null)
                            {
                                SetDataSourceSelection(DataSourceComboBox, targetDataSource.Name);
                                _currentDataSourceName = targetDataSource.Name;
                                _logger.LogInformation("使用查询数据源作为目标数据源: {DataSource}", targetDataSource.Name);
                                
                                // 加载该数据源的表名
                                await LoadTableNamesAsync(targetDataSource.Name);
                            }
                        }
                    }
                    catch (Exception dsEx)
                    {
                        _logger.LogError(dsEx, "回填目标数据源失败");
                    }

                    // 加载参数配置
                    if (!string.IsNullOrWhiteSpace(sqlConfig.Parameters))
                    {
                        LoadParametersToPanel(sqlConfig.Parameters);
                    }

                    // 加载清空表选项
                    if (sqlItem.OutputType == "数据表")
                    {
                        ClearTableCheckBox.IsChecked = sqlConfig.ClearTargetBeforeImport;
                        _logger.LogInformation("已加载清空表选项: {ClearTable}", sqlConfig.ClearTargetBeforeImport);
                    }

                    // 加载清空Sheet页选项
                    if (sqlItem.OutputType == "Excel工作表")
                    {
                        ClearSheetCheckBox.IsChecked = sqlConfig.ClearSheetBeforeOutput;
                        _logger.LogInformation("已加载清空Sheet页选项: {ClearSheet}", sqlConfig.ClearSheetBeforeOutput);
                    }

                    // 加载执行配置
                    LoadExecutionConfig(sqlConfig);
                }

                _logger.LogInformation("已加载SQL配置到表单: {SqlName}", sqlItem.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载SQL配置到表单失败");
            }
        }

        /// <summary>
        /// 设置分类选择
        /// </summary>
        private void SetCategorySelection(string category)
        {
            foreach (ComboBoxItem item in CategoryComboBox.Items)
            {
                if (item.Content?.ToString() == category)
                {
                    CategoryComboBox.SelectedItem = item;
                    return;
                }
            }
            
            // 如果分类不存在，设置为文本
            CategoryComboBox.Text = category;
        }

        /// <summary>
        /// 设置输出类型选择
        /// </summary>
        private void SetOutputTypeSelection(string outputType)
        {
            foreach (ComboBoxItem item in OutputTypeComboBox.Items)
            {
                if (item.Content?.ToString() == outputType)
                {
                    OutputTypeComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>
        /// 加载参数到参数面板
        /// </summary>
        /// <param name="parametersJson">参数JSON字符串</param>
        private void LoadParametersToPanel(string parametersJson)
        {
            try
            {
                // 清空现有参数
                ParametersPanel.Children.Clear();

                if (string.IsNullOrWhiteSpace(parametersJson))
                {
                    _parameters.Clear();
                    return;
                }

                // 反序列化参数
                var parameters = System.Text.Json.JsonSerializer.Deserialize<List<SqlParameter>>(parametersJson);
                if (parameters == null || parameters.Count == 0)
                {
                    _parameters.Clear();
                    return;
                }

                // 更新_parameters字段
                _parameters.Clear();
                _parameters.AddRange(parameters);

                // 为每个参数创建UI控件
                foreach (var parameter in parameters)
                {
                    var parameterBorder = CreateParameterBorder(parameter);
                    ParametersPanel.Children.Add(parameterBorder);
                }

                _logger.LogInformation("已加载 {Count} 个参数到参数面板", parameters.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载参数到面板失败");
            }
        }

        /// <summary>
        /// 加载参数到参数面板（重载方法）
        /// </summary>
        /// <param name="parameters">参数列表</param>
        private void LoadParametersToPanel(List<SqlParameter> parameters)
        {
            try
            {
                // 清空现有参数
                ParametersPanel.Children.Clear();

                if (parameters == null || parameters.Count == 0)
                {
                    _parameters.Clear();
                    return;
                }

                // 更新_parameters字段
                _parameters.Clear();
                _parameters.AddRange(parameters);

                // 为每个参数创建UI控件
                foreach (var parameter in parameters)
                {
                    var parameterBorder = CreateParameterBorder(parameter);
                    ParametersPanel.Children.Add(parameterBorder);
                }

                _logger.LogInformation("已加载 {Count} 个参数到参数面板", parameters.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载参数到面板失败");
            }
        }

        /// <summary>
        /// 创建参数Border控件
        /// </summary>
        /// <param name="parameter">参数对象</param>
        /// <returns>参数Border控件</returns>
        private Border CreateParameterBorder(SqlParameter parameter)
        {
            var parameterBorder = new Border
            {
                Style = FindResource("ParameterItemStyle") as Style
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            // 参数标题（带图标）
            var titlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            
            var iconBlock = new TextBlock
            {
                Text = "⚙️",
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            
            var titleBlock = new TextBlock
            {
                Text = "参数配置",
                Style = FindResource("ParameterTitleStyle") as Style
            };
            
            titlePanel.Children.Add(iconBlock);
            titlePanel.Children.Add(titleBlock);
            
            Grid.SetRow(titlePanel, 0);
            Grid.SetColumn(titlePanel, 0);
            grid.Children.Add(titlePanel);

            // 删除按钮
            var deleteButton = new Button
            {
                Content = "删除",
                Style = FindResource("DangerButtonStyle") as Style
            };
            deleteButton.Click += (s, e) =>
            {
                ParametersPanel.Children.Remove(parameterBorder);
            };
            Grid.SetRow(deleteButton, 0);
            Grid.SetColumn(deleteButton, 1);
            grid.Children.Add(deleteButton);

            // 参数名称和默认值（一行显示）
            var contentGrid = new Grid();
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 参数名称标签
            var nameLabel = new TextBlock
            {
                Text = "参数名称:",
                Style = FindResource("ParameterLabelStyle") as Style
            };
            Grid.SetColumn(nameLabel, 0);
            contentGrid.Children.Add(nameLabel);

            // 参数名称输入框
            var nameTextBox = new TextBox
            {
                Style = FindResource("ParameterInputStyle") as Style,
                Text = parameter.Name,
                Margin = new Thickness(0, 0, 16, 0)
            };
            Grid.SetColumn(nameTextBox, 1);
            contentGrid.Children.Add(nameTextBox);

            // 默认值标签
            var defaultValueLabel = new TextBlock
            {
                Text = "默认值:",
                Style = FindResource("ParameterLabelStyle") as Style
            };
            Grid.SetColumn(defaultValueLabel, 2);
            contentGrid.Children.Add(defaultValueLabel);

            // 默认值输入框
            var defaultValueTextBox = new TextBox
            {
                Style = FindResource("ParameterInputStyle") as Style,
                Text = parameter.DefaultValue
            };
            Grid.SetColumn(defaultValueTextBox, 3);
            contentGrid.Children.Add(defaultValueTextBox);

            Grid.SetRow(contentGrid, 1);
            Grid.SetColumn(contentGrid, 0);
            Grid.SetColumnSpan(contentGrid, 2);
            grid.Children.Add(contentGrid);

            parameterBorder.Child = grid;
            return parameterBorder;
        }

        /// <summary>
        /// 设置输出目标
        /// </summary>
        private void SetOutputTarget(string outputTarget, string outputType)
        {
            if (outputType == "数据表")
            {
                DataTableNameComboBox.Text = outputTarget;
            }
            else if (outputType == "Excel工作表")
            {
                // 解析Excel路径和Sheet名称
                if (outputTarget.Contains("!"))
                {
                    var parts = outputTarget.Split('!');
                    if (parts.Length >= 2)
                    {
                        var filePath = parts[0];
                        var sheetName = parts[1];
                        
                        var fileName = System.IO.Path.GetFileName(filePath);
                        var directory = System.IO.Path.GetDirectoryName(filePath);
                        
                        // 移除.xlsx扩展名
                        if (fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                        {
                            fileName = fileName.Substring(0, fileName.Length - 5);
                        }
                        
                        OutputPathTextBox.Text = directory ?? "";
                        ExcelFileNameTextBox.Text = fileName;
                        SheetNameTextBox.Text = sheetName;
                    }
                }
                else
                {
                    OutputTargetTextBox.Text = outputTarget;
                }
            }
        }

        /// <summary>
        /// 加载执行配置
        /// </summary>
        /// <param name="sqlConfig">SQL配置对象</param>
        private void LoadExecutionConfig(SqlConfig sqlConfig)
        {
            try
            {
                // 加载超时时间
                if (TimeoutTextBox != null)
                {
                    TimeoutTextBox.Text = sqlConfig.TimeoutSeconds.ToString();
                }

                // 加载最大行数
                if (MaxRowsTextBox != null)
                {
                    MaxRowsTextBox.Text = sqlConfig.MaxRows.ToString();
                }



                            _logger.LogInformation("已加载执行配置: 超时={Timeout}s, 最大行数={MaxRows}",
                sqlConfig.TimeoutSeconds, sqlConfig.MaxRows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载执行配置失败");
            }
        }

        private void AddSqlButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 清空表单，准备新增
                ClearForm();
                _currentSqlItem = null;
                
                // 设置默认值
                SqlNameTextBox.Text = $"新SQL配置_{DateTime.Now:yyyyMMdd_HHmmss}";
                CategoryComboBox.SelectedIndex = 0;
                OutputTypeComboBox.SelectedIndex = 0;
                SqlEditor.SqlText = "-- 请在此输入SQL语句\nSELECT * FROM your_table WHERE 1=1";
                
                _logger.LogInformation("开始新增SQL配置");
                MessageBox.Show("已准备新增SQL配置，请填写相关信息", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增SQL配置失败");
                MessageBox.Show($"新增SQL配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteSqlButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentSqlItem == null || string.IsNullOrEmpty(_currentSqlItem.Id))
                {
                    MessageBox.Show("请先选择要删除的SQL配置", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 确认删除
                var result = MessageBox.Show(
                    $"确定要删除SQL配置 '{_currentSqlItem.Name}' 吗？\n\n此操作不可撤销！",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                // 执行删除
                var deleteResult = await _sqlService.DeleteSqlConfigAsync(_currentSqlItem.Id);
                
                if (deleteResult)
                {
                    // 保存SQL名称用于日志记录
                    var sqlName = _currentSqlItem.Name;
                    
                    // 从列表中移除
                    _sqlItems.Remove(_currentSqlItem);
                    
                    // 清空表单
                    ClearForm();
                    _currentSqlItem = null;
                    
                    _logger.LogInformation("SQL配置删除成功: {SqlName}", sqlName);
                    MessageBox.Show("SQL配置删除成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("删除SQL配置失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除SQL配置失败");
                MessageBox.Show($"删除SQL配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveSqlButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveSqlAsync();
        }

        private async Task SaveSqlAsync()
        {
            try
            {
                // 验证必填字段
                if (string.IsNullOrWhiteSpace(SqlNameTextBox?.Text))
                {
                    MessageBox.Show("请输入SQL名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(SqlEditor?.SqlText))
                {
                    MessageBox.Show("请输入SQL语句", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(CategoryComboBox?.Text))
                {
                    MessageBox.Show("请选择SQL分类", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 验证参数配置
                var sqlParameters = CollectParametersFromPanel();
                var validationResult = ValidateParameters(sqlParameters);
                if (!validationResult.IsValid)
                {
                    MessageBox.Show(validationResult.ErrorMessage, "参数验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取当前选中的查询数据源ID
                string dataSourceId = null;
                var selectedQueryDataSource = QueryDataSourceComboBox?.SelectedItem as string;
                if (!string.IsNullOrWhiteSpace(selectedQueryDataSource))
                {
                    var dataSourceConfigs = await _dataSourceService.GetAllDataSourcesAsync();
                    var queryDataSource = dataSourceConfigs.FirstOrDefault(ds => ds.Name == selectedQueryDataSource);
                    if (queryDataSource != null)
                    {
                        dataSourceId = queryDataSource.Id;
                        _logger.LogInformation("保存SQL配置使用查询数据源: {DataSource}", selectedQueryDataSource);
                    }
                }
                else
                {
                    MessageBox.Show("请先选择查询数据源", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取输出数据源ID
                string outputDataSourceId = null;
                var outputType = GetSelectedOutputType();
                
                if (outputType == "数据表")
                {
                    // 数据表输出时，从目标数据源下拉框获取输出数据源ID
                    if (DataSourceComboBox.SelectedItem != null)
                    {
                        var selectedDataSourceName = DataSourceComboBox.SelectedItem.ToString();
                        var targetDataSource = _dataSources.FirstOrDefault(ds => ds == selectedDataSourceName);
                        if (targetDataSource != null)
                        {
                            // 根据数据源名称获取数据源ID
                            var dataSourceConfigs = await _dataSourceService.GetAllDataSourcesAsync();
                            var targetDataSourceConfig = dataSourceConfigs.FirstOrDefault(ds => ds.Name == targetDataSource);
                            outputDataSourceId = targetDataSourceConfig?.Id;
                        }
                    }
                    
                    // 如果没有选择目标数据源，使用查询数据源作为默认值
                    if (string.IsNullOrWhiteSpace(outputDataSourceId))
                    {
                        outputDataSourceId = dataSourceId;
                    }
                }
                else if (outputType == "Excel工作表")
                {
                    // Excel输出时，不需要输出数据源
                    outputDataSourceId = null;
                }

                // 序列化参数为JSON
                var parametersJson = System.Text.Json.JsonSerializer.Serialize(sqlParameters);

                // 创建SQL配置对象
                var sqlConfig = new SqlConfig
                {
                    Name = SqlNameTextBox.Text.Trim(),
                    Category = CategoryComboBox?.Text?.Trim() ?? "",
                    Description = DescriptionTextBox?.Text?.Trim() ?? "",
                    SqlStatement = SqlEditor.SqlText.Trim(),
                    DataSourceId = dataSourceId,
                    OutputDataSourceId = outputDataSourceId,
                    OutputType = GetSelectedOutputType(),
                    OutputTarget = GetOutputTarget(),
                    IsEnabled = true,
                    TimeoutSeconds = GetTimeoutSeconds(),
                    MaxRows = GetMaxRows(),
                    AllowDeleteTarget = false,
                    ClearTargetBeforeImport = GetClearTableOption(),
                    ClearSheetBeforeOutput = GetClearSheetOption(),
                    Parameters = parametersJson,

                };

                // 检查是新增还是更新
                if (_currentSqlItem != null && !string.IsNullOrEmpty(_currentSqlItem.Id))
                {
                    // 更新现有SQL配置
                    sqlConfig.Id = _currentSqlItem.Id;
                    sqlConfig.CreatedDate = _currentSqlItem.CreatedDate;
                    
                    await _sqlService.UpdateSqlConfigAsync(sqlConfig);
                    
                    // 更新本地列表
                    var existingItem = _sqlItems.FirstOrDefault(x => x.Id == _currentSqlItem.Id);
                    if (existingItem != null)
                    {
                        existingItem.Name = sqlConfig.Name;
                        existingItem.Category = sqlConfig.Category;
                        existingItem.Description = sqlConfig.Description;
                        existingItem.SqlStatement = sqlConfig.SqlStatement;
                        existingItem.OutputType = sqlConfig.OutputType;
                        existingItem.OutputTarget = sqlConfig.OutputTarget;
                        existingItem.LastModified = DateTime.Now;
                    }
                    
                    _logger.LogInformation("SQL配置更新成功: {SqlName}", sqlConfig.Name);
                    MessageBox.Show($"SQL配置 '{sqlConfig.Name}' 更新成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // 创建新的SQL配置
                    var newSqlConfig = await _sqlService.CreateSqlConfigAsync(sqlConfig);
                    
                    // 添加到本地列表
                    var newSqlItem = new SqlItem
                    {
                        Id = newSqlConfig.Id,
                        Name = newSqlConfig.Name,
                        Category = newSqlConfig.Category,
                        Description = newSqlConfig.Description,
                        SqlStatement = newSqlConfig.SqlStatement,
                        OutputType = newSqlConfig.OutputType,
                        OutputTarget = newSqlConfig.OutputTarget,
                        CreatedDate = newSqlConfig.CreatedDate,
                        LastModified = newSqlConfig.LastModified
                    };
                    
                    _sqlItems.Add(newSqlItem);
                    _currentSqlItem = newSqlItem;
                    
                    _logger.LogInformation("SQL配置创建成功: {SqlName}", sqlConfig.Name);
                    MessageBox.Show($"SQL配置 '{sqlConfig.Name}' 创建成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // 刷新列表显示
                _sqlItemsView.Refresh();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存SQL配置失败");
                MessageBox.Show($"保存SQL配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetSelectedOutputType()
        {
            var selectedItem = OutputTypeComboBox?.SelectedItem as ComboBoxItem;
            return selectedItem?.Content?.ToString() ?? "数据表";
        }

        private string GetOutputTarget()
        {
            var outputType = GetSelectedOutputType();
            
            if (outputType == "数据表")
            {
                return DataTableNameComboBox?.Text ?? "";
            }
            else if (outputType == "Excel工作表")
            {
                // 构建完整的Excel文件路径和Sheet名称
                var outputPath = OutputPathTextBox?.Text?.Trim() ?? "";
                var fileName = ExcelFileNameTextBox?.Text?.Trim() ?? "";
                var sheetName = SheetNameTextBox?.Text?.Trim() ?? "Sheet1";
                
                if (!string.IsNullOrEmpty(outputPath) && !string.IsNullOrEmpty(fileName))
                {
                    // 确保文件路径以.xlsx结尾
                    if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName += ".xlsx";
                    }
                    
                    var fullPath = Path.Combine(outputPath, fileName);
                    return $"{fullPath}!{sheetName}";
                }
                
                return OutputTargetTextBox?.Text ?? "";
            }
            
            return "";
        }

        /// <summary>
        /// 获取清空表选项
        /// </summary>
        /// <returns>是否清空表</returns>
        private bool GetClearTableOption()
        {
            var outputType = GetSelectedOutputType();
            
            // 只有输出类型为"数据表"时才考虑清空表选项
            if (outputType == "数据表")
            {
                return ClearTableCheckBox?.IsChecked ?? false;
            }
            
            return false;
        }

        /// <summary>
        /// 获取清空Sheet页选项
        /// </summary>
        /// <returns>是否清空Sheet页</returns>
        private bool GetClearSheetOption()
        {
            var outputType = GetSelectedOutputType();
            
            // 只有输出类型为"Excel工作表"时才考虑清空Sheet页选项
            if (outputType == "Excel工作表")
            {
                return ClearSheetCheckBox?.IsChecked ?? false;
            }
            
            return false;
        }

        /// <summary>
        /// 从参数面板收集参数配置
        /// </summary>
        /// <returns>参数列表</returns>
        private List<SqlParameter> CollectParametersFromPanel()
        {
            var parameters = new List<SqlParameter>();

            foreach (var child in ParametersPanel.Children)
            {
                if (child is Border border && border.Child is Grid grid)
                {
                    var parameter = ExtractParameterFromGrid(grid);
                    if (parameter != null)
                    {
                        parameters.Add(parameter);
                    }
                }
            }

            return parameters;
        }

        /// <summary>
        /// 验证参数配置
        /// </summary>
        /// <param name="parameters">参数列表</param>
        /// <returns>验证结果</returns>
        private (bool IsValid, string ErrorMessage) ValidateParameters(List<SqlParameter> parameters)
        {
            var parameterNames = new HashSet<string>();

            foreach (var parameter in parameters)
            {
                // 检查参数名称是否为空
                if (string.IsNullOrWhiteSpace(parameter.Name))
                {
                    return (false, "参数名称不能为空");
                }

                // 检查参数名称格式
                if (!parameter.Name.StartsWith("@"))
                {
                    return (false, $"参数名称 '{parameter.Name}' 必须以@开头");
                }

                // 检查参数名称是否重复
                if (parameterNames.Contains(parameter.Name))
                {
                    return (false, $"参数名称 '{parameter.Name}' 重复");
                }

                parameterNames.Add(parameter.Name);
            }

            return (true, "");
        }

        /// <summary>
        /// 从Grid中提取参数信息
        /// </summary>
        /// <param name="grid">参数Grid</param>
        /// <returns>参数对象</returns>
        private SqlParameter ExtractParameterFromGrid(Grid grid)
        {
            try
            {
                var parameter = new SqlParameter();

                // 遍历所有子控件查找TextBox
                foreach (var child in grid.Children)
                {
                    if (child is Grid contentGrid)
                {
                        foreach (var contentChild in contentGrid.Children)
                        {
                            if (contentChild is TextBox textBox)
                            {
                                // 根据Grid.Column位置判断是参数名称还是默认值
                                var column = Grid.GetColumn(textBox);
                                if (column == 1) // 参数名称输入框
                    {
                                    parameter.Name = textBox.Text?.Trim() ?? "";
                    }
                                else if (column == 3) // 默认值输入框
                    {
                                    parameter.DefaultValue = textBox.Text?.Trim() ?? "";
                                }
                            }
                        }
                    }
                }

                // 验证参数名称
                if (string.IsNullOrWhiteSpace(parameter.Name))
                {
                    return null; // 跳过无效参数
                }

                return parameter;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提取参数信息失败");
                return null;
            }
        }

        private async void ExecuteQueryButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteSqlAsync();
        }

        private async Task ExecuteSqlAsync()
        {
            try
            {
                if (_currentSqlItem == null || string.IsNullOrEmpty(_currentSqlItem.Id))
                {
                    MessageBox.Show("请先选择要执行的SQL配置", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 显示执行确认对话框
                var result = MessageBox.Show(
                    $"确定要执行SQL配置 '{_currentSqlItem.Name}' 吗？\n\nSQL语句：\n{_currentSqlItem.SqlStatement}",
                    "确认执行",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                // 显示执行进度
                var progressWindow = new Window
                {
                    Title = "SQL执行中...",
                    Width = 400,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    ResizeMode = ResizeMode.NoResize,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 42, 42)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64)),
                    BorderThickness = new Thickness(1)
                };

                var progressGrid = new Grid();
                progressGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                progressGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                progressGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                var progressText = new TextBlock
                {
                    Text = "正在执行SQL查询，请稍候...",
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(20)
                };
                Grid.SetRow(progressText, 0);
                progressGrid.Children.Add(progressText);

                var progressBar = new ProgressBar
                {
                    IsIndeterminate = true,
                    Height = 20,
                    Margin = new Thickness(20, 10, 20, 20),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64)),
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 150, 255))
                };
                Grid.SetRow(progressBar, 1);
                progressGrid.Children.Add(progressBar);

                var cancelButton = new Button
                {
                    Content = "取消",
                    Width = 80,
                    Height = 30,
                    Margin = new Thickness(20, 0, 20, 20),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64)),
                    Foreground = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(0)
                };
                Grid.SetRow(cancelButton, 2);
                progressGrid.Children.Add(cancelButton);

                progressWindow.Content = progressGrid;
                progressWindow.Show();

                // 执行SQL查询
                SqlExecutionResult executionResult = null;
                try
                {
                    executionResult = await _sqlService.ExecuteSqlConfigAsync(_currentSqlItem.Id);
                }
                finally
                {
                    progressWindow.Close();
                }

                // 显示执行结果
                if (executionResult != null)
                {
                    if (executionResult.Status == "Success")
                    {
                        // 直接显示查询结果数据弹窗（包含成功信息）
                        ShowQueryResultWindow(executionResult);
                    }
                    else
                    {
                        // 显示错误消息
                        var errorMessage = $"❌ SQL查询执行失败！\n\n" +
                                         $"🔍 错误信息：{executionResult.ErrorMessage}\n" +
                                         $"⏱️ 执行时间：{executionResult.Duration}ms\n" +
                                         $"📅 执行时间：{executionResult.StartTime:yyyy-MM-dd HH:mm:ss}";

                        MessageBox.Show(errorMessage, "查询失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    _logger.LogInformation("SQL执行完成: {SqlName}, 状态: {Status}, 耗时: {Duration}ms",
                        _currentSqlItem.Name, executionResult.Status, executionResult.Duration);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行SQL失败");
                MessageBox.Show($"执行SQL失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示查询结果弹窗
        /// </summary>
        private void ShowQueryResultWindow(SqlExecutionResult executionResult)
        {
            try
            {
                // 创建结果窗口
                var resultWindow = new Window
                {
                    Title = $"查询结果 - {_currentSqlItem?.Name}",
                    Width = 1000,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 42, 42)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64)),
                    BorderThickness = new Thickness(1)
                };

                var mainGrid = new Grid();
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                // 标题
                var titleBlock = new TextBlock
                {
                    Text = $"📊 SQL查询结果预览（前100条数据）",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.White,
                    Margin = new Thickness(20, 20, 20, 10),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(titleBlock, 0);
                mainGrid.Children.Add(titleBlock);

                // 统计信息
                var statsBlock = new TextBlock
                {
                    Text = $"📈 总行数：{executionResult.AffectedRows} | ⏱️ 执行时间：{executionResult.Duration}ms | 📅 执行时间：{executionResult.StartTime:yyyy-MM-dd HH:mm:ss}",
                    FontSize = 12,
                    Foreground = System.Windows.Media.Brushes.LightGray,
                    Margin = new Thickness(20, 0, 20, 10),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(statsBlock, 1);
                mainGrid.Children.Add(statsBlock);

                // 数据表格
                var dataGrid = new DataGrid
                {
                    AutoGenerateColumns = true,
                    CanUserAddRows = false,
                    CanUserDeleteRows = false,
                    CanUserReorderColumns = true,
                    CanUserResizeColumns = true,
                    CanUserResizeRows = false,
                    CanUserSortColumns = true,
                    GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                    HeadersVisibility = DataGridHeadersVisibility.Column,
                    IsReadOnly = true,
                    SelectionMode = DataGridSelectionMode.Single,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(32, 32, 32)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(20, 0, 20, 20)
                };

                Grid.SetRow(dataGrid, 2);
                mainGrid.Children.Add(dataGrid);

                // 按钮区域
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(20, 0, 20, 20)
                };

                var exportButton = new Button
                {
                    Content = "📥 导出数据",
                    Width = 120,
                    Height = 35,
                    Margin = new Thickness(10, 0, 10, 0),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 150, 255)),
                    Foreground = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(0),
                    FontWeight = FontWeights.SemiBold
                };
                exportButton.Click += (s, e) =>
                {
                    MessageBox.Show("导出功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                };

                var closeButton = new Button
                {
                    Content = "❌ 关闭",
                    Width = 120,
                    Height = 35,
                    Margin = new Thickness(10, 0, 10, 0),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64)),
                    Foreground = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(0),
                    FontWeight = FontWeights.SemiBold
                };
                closeButton.Click += (s, e) => resultWindow.Close();

                buttonPanel.Children.Add(exportButton);
                buttonPanel.Children.Add(closeButton);

                Grid.SetRow(buttonPanel, 3);
                mainGrid.Children.Add(buttonPanel);

                resultWindow.Content = mainGrid;

                // 生成模拟数据
                var mockData = GenerateMockData(executionResult.AffectedRows);
                dataGrid.ItemsSource = mockData;

                resultWindow.Show();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示查询结果窗口失败");
                MessageBox.Show($"显示查询结果失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 生成模拟数据用于显示
        /// </summary>
        private List<dynamic> GenerateMockData(int totalRows)
        {
            var data = new List<dynamic>();
            var displayRows = Math.Min(totalRows, 100); // 最多显示100条

            for (int i = 1; i <= displayRows; i++)
            {
                var row = new
                {
                    ID = i,
                    Name = $"用户{i}",
                    Email = $"user{i}@example.com",
                    Age = 20 + (i % 50),
                    Department = $"部门{(i % 5) + 1}",
                    Salary = 5000 + (i * 100),
                    CreateDate = DateTime.Now.AddDays(-i),
                    Status = i % 3 == 0 ? "活跃" : "非活跃"
                };
                data.Add(row);
            }

            return data;
        }

        private async void TestSqlButton_Click(object sender, RoutedEventArgs e)
        {
            await TestSqlQueryAsync();
        }

        private async void ExecuteSqlOutputButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteSqlOutputAsync();
        }

        private async Task TestSqlQueryAsync()
        {
            try
            {
                var sqlStatement = SqlEditor?.SqlText?.Trim();
                if (string.IsNullOrWhiteSpace(sqlStatement))
                {
                    MessageBox.Show("请先输入SQL语句", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取当前选中的查询数据源ID（使用页面选择的数据源）
                string dataSourceId = null;
                var selectedQueryDataSource = QueryDataSourceComboBox?.SelectedItem as string;
                if (!string.IsNullOrWhiteSpace(selectedQueryDataSource))
                {
                    var dataSourceConfigs = await _dataSourceService.GetAllDataSourcesAsync();
                    var queryDataSource = dataSourceConfigs.FirstOrDefault(ds => ds.Name == selectedQueryDataSource);
                    if (queryDataSource != null)
                    {
                        dataSourceId = queryDataSource.Id;
                        _logger.LogInformation("测试查询使用数据源: {DataSource}", selectedQueryDataSource);
                    }
                }
                else
                {
                    MessageBox.Show("请先选择查询数据源", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 收集界面上的参数
                var parameters = CollectParametersFromPanel();
                var parametersDict = new Dictionary<string, object>();
                
                foreach (var param in parameters)
                {
                    if (!string.IsNullOrWhiteSpace(param.Name) && !string.IsNullOrWhiteSpace(param.DefaultValue))
                    {
                        // 尝试解析参数值
                        if (int.TryParse(param.DefaultValue, out int intValue))
                        {
                            parametersDict[param.Name] = intValue;
                        }
                        else if (double.TryParse(param.DefaultValue, out double doubleValue))
                    {
                            parametersDict[param.Name] = doubleValue;
                        }
                        else if (bool.TryParse(param.DefaultValue, out bool boolValue))
                        {
                            parametersDict[param.Name] = boolValue;
                        }
                        else if (DateTime.TryParse(param.DefaultValue, out DateTime dateValue))
                        {
                            parametersDict[param.Name] = dateValue;
                        }
                        else
                        {
                            // 默认为字符串
                            parametersDict[param.Name] = param.DefaultValue;
                        }
                    }
                }

                // 执行SQL查询测试（SQL服务内部会处理LIMIT限制）
                var testResult = await _sqlService.TestSqlStatementAsync(sqlStatement, dataSourceId, parametersDict);

                // 使用自定义弹窗显示结果
                var ownerWindow = Window.GetWindow(this);
                SqlTestResultDialog.ShowResult(testResult, ownerWindow);

                _logger.LogInformation("SQL查询测试完成: {SqlStatement}, 成功: {IsSuccess}, 参数数量: {ParameterCount}", 
                    sqlStatement, testResult.IsSuccess, parametersDict.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行SQL查询测试失败");
                MessageBox.Show($"执行SQL查询测试失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteSqlOutputAsync()
        {
            try
            {
                // 验证输入
                var sqlStatement = SqlEditor?.SqlText?.Trim();
                if (string.IsNullOrWhiteSpace(sqlStatement))
                {
                    MessageBox.Show("请先输入SQL语句", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取查询数据源
                var queryDataSource = QueryDataSourceComboBox?.SelectedItem as string;
                if (string.IsNullOrWhiteSpace(queryDataSource))
                {
                    MessageBox.Show("请先选择查询数据源", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取输出类型
                var outputType = GetSelectedOutputType();
                if (string.IsNullOrWhiteSpace(outputType))
                {
                    MessageBox.Show("请先选择输出类型", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取查询数据源ID
                string queryDataSourceId = null;
                var dataSourceConfigs = await _dataSourceService.GetAllDataSourcesAsync();
                var selectedQueryDataSource = dataSourceConfigs.FirstOrDefault(ds => ds.Name == queryDataSource);
                if (selectedQueryDataSource != null)
                {
                    queryDataSourceId = selectedQueryDataSource.Id;
                }

                // 根据输出类型执行不同的输出逻辑
                if (outputType == "数据表")
                {
                    await ExecuteSqlOutputToTableAsync(sqlStatement, queryDataSourceId);
                }
                else if (outputType == "Excel工作表")
                {
                    await ExecuteSqlOutputToExcelAsync(sqlStatement, queryDataSourceId);
                }
                else
                {
                    MessageBox.Show("不支持的输出类型", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行SQL输出失败");
                MessageBox.Show($"执行SQL输出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteSqlOutputToTableAsync(string sqlStatement, string queryDataSourceId)
        {
            try
            {
                var targetTable = DataTableNameComboBox?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(targetTable))
                {
                    MessageBox.Show("请先配置目标数据表", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取目标数据源ID
                string targetDataSourceId = null;
                if (!string.IsNullOrEmpty(_currentDataSourceName))
                {
                    var dataSourceConfigs = await _dataSourceService.GetAllDataSourcesAsync();
                    var selectedDataSource = dataSourceConfigs.FirstOrDefault(ds => ds.Name == _currentDataSourceName);
                    if (selectedDataSource != null)
                    {
                        targetDataSourceId = selectedDataSource.Id;
                    }
                }

                // 收集界面上的参数
                var parameters = CollectParametersFromPanel();
                var parametersDict = new Dictionary<string, object>();
                
                foreach (var param in parameters)
                {
                    if (!string.IsNullOrWhiteSpace(param.Name) && !string.IsNullOrWhiteSpace(param.DefaultValue))
                    {
                        // 尝试解析参数值
                        if (int.TryParse(param.DefaultValue, out int intValue))
                        {
                            parametersDict[param.Name] = intValue;
                        }
                        else if (double.TryParse(param.DefaultValue, out double doubleValue))
                        {
                            parametersDict[param.Name] = doubleValue;
                        }
                        else if (bool.TryParse(param.DefaultValue, out bool boolValue))
                        {
                            parametersDict[param.Name] = boolValue;
                        }
                        else if (DateTime.TryParse(param.DefaultValue, out DateTime dateValue))
                        {
                            parametersDict[param.Name] = dateValue;
                        }
                        else
                        {
                            // 默认为字符串
                            parametersDict[param.Name] = param.DefaultValue;
                        }
                    }
                }

                // 创建进度对话框
                var progressDialog = new ExcelProcessor.WPF.Dialogs.ProgressDialog("正在执行SQL输出到数据表...", "请稍候，正在处理数据...");
                progressDialog.Show();

                try
                {
                    // 执行SQL并输出到数据表（通过可复用服务）
                    var sqlOutputService = App.Services.GetRequiredService<ISqlOutputService>();
                    var result = await sqlOutputService.OutputToTableAsync(sqlStatement, queryDataSourceId, targetDataSourceId, targetTable, false, parametersDict);
                    
                    // 关闭进度对话框
                    progressDialog.Close();
                    
                    if (result.IsSuccess)
                    {
                        // 显示成功提示对话框
                        var successDialog = new SuccessDialog(
                            "SQL执行成功！",
                            $"输出到数据表: {targetTable}\n影响行数: {result.AffectedRows:N0}行\n执行时间: {result.ExecutionTimeMs}ms",
                            "确定"
                        );
                        successDialog.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show($"SQL执行失败：{result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    _logger.LogInformation("SQL输出到数据表完成: {SqlStatement}, 目标表: {TargetTable}, 成功: {IsSuccess}, 参数数量: {ParameterCount}", 
                        sqlStatement, targetTable, result.IsSuccess, parametersDict.Count);
                }
                catch (Exception)
                {
                    // 确保进度对话框关闭
                    progressDialog.Close();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行SQL输出到数据表失败");
                MessageBox.Show($"执行SQL输出到数据表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteSqlOutputToExcelAsync(string sqlStatement, string queryDataSourceId)
        {
            try
            {
                var outputPath = OutputPathTextBox?.Text?.Trim();
                var fileName = ExcelFileNameTextBox?.Text?.Trim();
                var sheetName = SheetNameTextBox?.Text?.Trim();

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    MessageBox.Show("请先配置输出路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    MessageBox.Show("请先配置Excel文件名", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(sheetName))
                {
                    MessageBox.Show("请先配置Sheet名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取清空Sheet页选项
                bool clearSheetBeforeOutput = ClearSheetCheckBox?.IsChecked ?? false;

                // 收集界面上的参数
                var parameters = CollectParametersFromPanel();
                var parametersDict = new Dictionary<string, object>();
                
                foreach (var param in parameters)
                {
                    if (!string.IsNullOrWhiteSpace(param.Name) && !string.IsNullOrWhiteSpace(param.DefaultValue))
                    {
                        // 尝试解析参数值
                        if (int.TryParse(param.DefaultValue, out int intValue))
                        {
                            parametersDict[param.Name] = intValue;
                        }
                        else if (double.TryParse(param.DefaultValue, out double doubleValue))
                        {
                            parametersDict[param.Name] = doubleValue;
                        }
                        else if (bool.TryParse(param.DefaultValue, out bool boolValue))
                        {
                            parametersDict[param.Name] = boolValue;
                        }
                        else if (DateTime.TryParse(param.DefaultValue, out DateTime dateValue))
                        {
                            parametersDict[param.Name] = dateValue;
                        }
                        else
                        {
                            // 默认为字符串
                            parametersDict[param.Name] = param.DefaultValue;
                        }
                    }
                }

                // 执行SQL并输出到Excel
                var sqlOutputService = App.Services.GetRequiredService<ISqlOutputService>();
                var result = await sqlOutputService.OutputToExcelAsync(sqlStatement, queryDataSourceId, System.IO.Path.Combine(outputPath, fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ? fileName : fileName + ".xlsx"), sheetName, clearSheetBeforeOutput, parametersDict);
                
                if (result.IsSuccess)
                {
                    MessageBox.Show($"SQL执行成功！\n输出到Excel: {outputPath}\\{fileName}.xlsx\nSheet名称: {sheetName}\n影响行数: {result.AffectedRows:N0}行", 
                        "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"SQL执行失败：{result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                _logger.LogInformation("SQL输出到Excel完成: {SqlStatement}, 输出路径: {OutputPath}, 文件名: {FileName}, Sheet: {SheetName}, 成功: {IsSuccess}, 参数数量: {ParameterCount}", 
                    sqlStatement, outputPath, fileName, sheetName, result.IsSuccess, parametersDict.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行SQL输出到Excel失败");
                MessageBox.Show($"执行SQL输出到Excel失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TestOutputButton_Click(object sender, RoutedEventArgs e)
        {
            await TestOutputFormatAsync();
        }

        private async Task TestOutputFormatAsync()
        {
            try
            {
                var sqlStatement = SqlEditor?.SqlText?.Trim();
                if (string.IsNullOrWhiteSpace(sqlStatement))
                {
                    MessageBox.Show("请先输入SQL语句", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var outputType = GetSelectedOutputType();
                
                if (outputType == "数据表")
                {
                    await TestOutputToTableAsync(sqlStatement);
                }
                else if (outputType == "Excel工作表")
                {
                    await TestOutputToWorksheetAsync(sqlStatement);
                }
                else
                {
                    MessageBox.Show("请先选择输出类型", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试输出格式失败");
                MessageBox.Show($"测试输出格式失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task TestOutputToTableAsync(string sqlStatement)
        {
            try
            {
                var targetTable = DataTableNameComboBox?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(targetTable))
                {
                    MessageBox.Show("请先配置目标数据表", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取当前选中的查询数据源ID
                string queryDataSourceId = null;
                var selectedQueryDataSource = QueryDataSourceComboBox?.SelectedItem as string;
                if (!string.IsNullOrWhiteSpace(selectedQueryDataSource))
                {
                    var dataSourceConfigs = await _dataSourceService.GetAllDataSourcesAsync();
                    var queryDataSource = dataSourceConfigs.FirstOrDefault(ds => ds.Name == selectedQueryDataSource);
                    if (queryDataSource != null)
                    {
                        queryDataSourceId = queryDataSource.Id;
                        _logger.LogInformation("测试输出使用查询数据源: {DataSource}", selectedQueryDataSource);
                    }
                }
                else
                {
                    MessageBox.Show("请先选择查询数据源", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取目标数据源ID
                string targetDataSourceId = null;
                var selectedTargetDataSource = DataSourceComboBox?.SelectedItem as string;
                if (!string.IsNullOrWhiteSpace(selectedTargetDataSource))
                {
                    var dataSourceConfigs = await _dataSourceService.GetAllDataSourcesAsync();
                    var targetDataSource = dataSourceConfigs.FirstOrDefault(ds => ds.Name == selectedTargetDataSource);
                    if (targetDataSource != null)
                    {
                        targetDataSourceId = targetDataSource.Id;
                        _logger.LogInformation("测试输出使用目标数据源: {DataSource}", selectedTargetDataSource);
                    }
                }
                else
                {
                    MessageBox.Show("请先选择目标数据源", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    // 获取清空表选项
                    bool clearTableBeforeInsert = ClearTableCheckBox?.IsChecked ?? false;
                    
                    // 收集界面上的参数
                    var parameters = CollectParametersFromPanel();
                    var parametersDict = new Dictionary<string, object>();
                    
                    foreach (var param in parameters)
                    {
                        if (!string.IsNullOrWhiteSpace(param.Name) && !string.IsNullOrWhiteSpace(param.DefaultValue))
                        {
                            // 尝试解析参数值
                            if (int.TryParse(param.DefaultValue, out int intValue))
                            {
                                parametersDict[param.Name] = intValue;
                            }
                            else if (double.TryParse(param.DefaultValue, out double doubleValue))
                            {
                                parametersDict[param.Name] = doubleValue;
                            }
                            else if (bool.TryParse(param.DefaultValue, out bool boolValue))
                            {
                                parametersDict[param.Name] = boolValue;
                            }
                            else if (DateTime.TryParse(param.DefaultValue, out DateTime dateValue))
                            {
                                parametersDict[param.Name] = dateValue;
                            }
                            else
                            {
                                // 默认为字符串
                                parametersDict[param.Name] = param.DefaultValue;
                            }
                        }
                    }
                    
                    // 实际执行SQL输出到表
                    var sqlOutputService = App.Services.GetRequiredService<ISqlOutputService>();
                    var outputResult = await sqlOutputService.OutputToTableAsync(sqlStatement, queryDataSourceId, targetDataSourceId, targetTable, clearTableBeforeInsert, parametersDict);

                    if (outputResult.IsSuccess)
                    {
                        // 构建详细信息
                        var details = new Dictionary<string, string>
                        {
                            { "目标表", targetTable },
                            { "实际输出行数", $"{outputResult.AffectedRows:N0} 行" },
                            { "执行时间", $"{outputResult.ExecutionTimeMs}ms" },
                            { "输出路径", outputResult.OutputPath ?? "数据表" }
                        };

                        // 如果清空了表，添加清空表信息
                        if (clearTableBeforeInsert)
                        {
                            details.Add("清空表", "是");
                        }

                        // 使用自定义弹窗显示成功结果
                        var ownerWindow = Window.GetWindow(this);
                        TestOutputResultDialog.ShowResult(true, "数据表", details, null, ownerWindow);

                        _logger.LogInformation("SQL输出到数据表执行成功: {TargetTable}, 影响行数: {AffectedRows}", 
                            targetTable, outputResult.AffectedRows);
                    }
                    else
                    {
                        // 使用自定义弹窗显示错误
                        var ownerWindow = Window.GetWindow(this);
                        TestOutputResultDialog.ShowResult(false, "数据表", null, 
                            $"执行失败：{outputResult.ErrorMessage}", 
                            ownerWindow);

                        _logger.LogError("SQL输出到数据表执行失败: {TargetTable}, 错误: {ErrorMessage}", 
                            targetTable, outputResult.ErrorMessage);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试输出到数据表失败");
                MessageBox.Show($"测试输出到数据表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 检查表是否存在
        /// </summary>
        private async Task<bool> CheckTableExistsAsync(string tableName, string? dataSourceId)
        {
            try
            {
                // 获取数据源连接字符串
                string connectionString = _connectionString; // 默认使用系统数据库
                if (!string.IsNullOrEmpty(dataSourceId))
                {
                    var dataSource = await _dataSourceService.GetDataSourceByIdAsync(dataSourceId);
                    if (dataSource != null && !string.IsNullOrEmpty(dataSource.ConnectionString))
                    {
                        connectionString = dataSource.ConnectionString;
                    }
                }

                // 使用SQLite连接检查表是否存在
                using var connection = new SQLiteConnection(connectionString);
                await connection.OpenAsync();

                var checkSql = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@TableName";
                using var command = new SQLiteCommand(checkSql, connection);
                command.Parameters.AddWithValue("@TableName", tableName);
                
                var count = await command.ExecuteScalarAsync();
                return Convert.ToInt32(count) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查表是否存在失败: {TableName}", tableName);
                return false;
            }
        }

        private async Task TestOutputToWorksheetAsync(string sqlStatement)
        {
            try
            {
                // 构建完整的输出路径
                var outputPath = OutputPathTextBox?.Text?.Trim() ?? "";
                var fileName = ExcelFileNameTextBox?.Text?.Trim() ?? "";
                
                if (string.IsNullOrWhiteSpace(outputPath) || string.IsNullOrWhiteSpace(fileName))
                {
                    MessageBox.Show("请先配置输出路径和文件名", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // 确保文件路径以.xlsx结尾
                if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".xlsx";
                }
                
                var outputTarget = Path.Combine(outputPath, fileName);

                // 获取当前选中的查询数据源ID
                string? queryDataSourceId = null;
                var selectedQueryDataSource = QueryDataSourceComboBox?.SelectedItem as string;
                if (!string.IsNullOrWhiteSpace(selectedQueryDataSource))
                {
                    var dataSourceConfigs = await _dataSourceService.GetAllDataSourcesAsync();
                    var queryDataSource = dataSourceConfigs.FirstOrDefault(ds => ds.Name == selectedQueryDataSource);
                    if (queryDataSource != null)
                    {
                        queryDataSourceId = queryDataSource.Id;
                        _logger.LogInformation("测试输出到工作表使用查询数据源: {DataSource}", selectedQueryDataSource);
                    }
                }
                else
                {
                    MessageBox.Show("请先选择查询数据源", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    // 获取清空Sheet页选项
                    bool clearSheetBeforeOutput = ClearSheetCheckBox?.IsChecked ?? false;
                    
                    // 获取Sheet名称
                    var sheetName = SheetNameTextBox?.Text?.Trim();
                    if (string.IsNullOrWhiteSpace(sheetName))
                    {
                        sheetName = "Sheet1"; // 默认Sheet名称
                    }

                    // 实际执行SQL输出到Excel工作表
                    var sqlOutputService = App.Services.GetRequiredService<ISqlOutputService>();
                    var outputResult = await sqlOutputService.OutputToExcelAsync(sqlStatement, queryDataSourceId, outputTarget, sheetName, clearSheetBeforeOutput, null);

                    if (outputResult.IsSuccess)
                    {
                        // 构建详细信息
                        var details = new Dictionary<string, string>
                        {
                            { "输出目标", outputTarget },
                            { "Sheet名称", sheetName },
                            { "实际输出行数", $"{outputResult.AffectedRows:N0} 行" },
                            { "执行时间", $"{outputResult.ExecutionTimeMs:N0}ms" },
                            { "输出路径", outputResult.OutputPath ?? "Excel文件" },
                            { "处理速度", $"{outputResult.AffectedRows / Math.Max(1, outputResult.ExecutionTimeMs / 1000.0):N0} 行/秒" }
                        };

                        // 如果清空了Sheet页，添加清空Sheet页信息
                        if (clearSheetBeforeOutput)
                        {
                            details.Add("清空Sheet页", "是");
                        }

                        // 使用自定义弹窗显示成功结果
                        var ownerWindow = Window.GetWindow(this);
                        TestOutputResultDialog.ShowResult(true, "Excel工作表", details, null, ownerWindow);

                        _logger.LogInformation("SQL输出到Excel工作表执行成功: {OutputTarget}, Sheet: {SheetName}, 影响行数: {AffectedRows}", 
                            outputTarget, sheetName, outputResult.AffectedRows);
                    }
                    else
                    {
                        // 使用自定义弹窗显示错误
                        var ownerWindow = Window.GetWindow(this);
                        var errorDetails = $"错误信息：{outputResult.ErrorMessage}\n\n请检查：\n• SQL语法是否正确\n• 数据源连接是否正常\n• 输出路径是否可写\n• Excel文件是否被其他程序占用\n• 是否有足够的磁盘空间";
                        TestOutputResultDialog.ShowResult(false, "Excel工作表", null, errorDetails, ownerWindow);

                        _logger.LogError("SQL输出到Excel工作表执行失败: {OutputTarget}, 错误: {ErrorMessage}", 
                            outputTarget, outputResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SQL输出到Excel工作表执行异常");
                    MessageBox.Show($"SQL输出到Excel工作表执行异常：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试输出到工作表失败");
                MessageBox.Show($"测试输出到工作表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 清空表单
        /// </summary>
        private void ClearForm()
        {
            SqlNameTextBox.Text = "";
            DescriptionTextBox.Text = "";
            SqlEditor.SqlText = "";
            CategoryComboBox.SelectedIndex = -1;
            CategoryComboBox.Text = "";
            OutputTypeComboBox.SelectedIndex = 0;
            DataTableNameComboBox.Text = "";
            OutputPathTextBox.Text = "data\\Output\\EXCEL";
            ExcelFileNameTextBox.Text = "";
            SheetNameTextBox.Text = "";
            OutputTargetTextBox.Text = "";
            TimeoutTextBox.Text = "300";
            MaxRowsTextBox.Text = "10000";
            
            // 清空参数面板
            ParametersPanel.Children.Clear();
            
            // 重置清空表选项
            ClearTableCheckBox.IsChecked = false;

            // 重置执行配置选项
            if (ExecutionModeComboBox != null)
            {
                ExecutionModeComboBox.Text = "Normal";
            }
            if (EnableLoggingCheckBox != null)
            {
                EnableLoggingCheckBox.IsChecked = true;
            }
            if (CacheResultsCheckBox != null)
            {
                CacheResultsCheckBox.IsChecked = false;
            }
            if (ValidateParametersCheckBox != null)
            {
                ValidateParametersCheckBox.IsChecked = true;
            }
        }

        /// <summary>
        /// 添加必需的using语句
        /// </summary>
        private void AddMissingEventHandlers()
        {
            // 格式化按钮事件
            try
            {
                var formatButton = FindVisualChildren<Button>(this)
                    .FirstOrDefault(b => b.Content?.ToString() == "格式化");
                if (formatButton != null)
                {
                    formatButton.Click += FormatSqlButton_Click;
                }

                // 添加参数按钮事件
                var addParamButton = FindVisualChildren<Button>(this)
                    .FirstOrDefault(b => b.Content?.ToString() == "添加参数");
                if (addParamButton != null)
                {
                    addParamButton.Click += AddParameterButton_Click;
                }

                // 测试查询按钮
                var testQueryButton = this.FindName("TestQueryButton") as Button;
                if (testQueryButton != null)
                {
                    testQueryButton.Click += TestQueryButton_Click;
                }

                // 保存配置按钮
                var saveConfigButton = this.FindName("SaveConfigButton") as Button;
                if (saveConfigButton != null)
                {
                    saveConfigButton.Click += SaveSqlButton_Click;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "添加事件处理失败");
            }
        }

        /// <summary>
        /// 格式化SQL按钮事件
        /// </summary>
        private void FormatSqlButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sqlText = SqlEditor.SqlText?.Trim();
                if (string.IsNullOrWhiteSpace(sqlText))
                {
                    MessageBox.Show("请先输入SQL语句", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 简单的SQL格式化
                var formattedSql = FormatSqlStatement(sqlText);
                SqlEditor.SqlText = formattedSql;
                
                _logger.LogInformation("SQL语句格式化完成");
                MessageBox.Show("SQL语句格式化完成", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL格式化失败");
                MessageBox.Show($"SQL格式化失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 简单的SQL格式化
        /// </summary>
        private string FormatSqlStatement(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return sql;

            // 基本的SQL格式化规则
            var keywords = new[] { "SELECT", "FROM", "WHERE", "JOIN", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", 
                                 "ORDER BY", "GROUP BY", "HAVING", "UNION", "INSERT", "UPDATE", "DELETE", 
                                 "CREATE", "ALTER", "DROP", "AND", "OR", "NOT", "IN", "EXISTS", "CASE", "WHEN", "THEN", "ELSE", "END" };

            var lines = sql.Split('\n');
            var result = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    result.Add("");
                    continue;
                }

                // 添加适当的缩进和换行
                if (trimmedLine.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(trimmedLine);
                }
                else if (trimmedLine.StartsWith("FROM", StringComparison.OrdinalIgnoreCase) ||
                         trimmedLine.StartsWith("WHERE", StringComparison.OrdinalIgnoreCase) ||
                         trimmedLine.StartsWith("ORDER BY", StringComparison.OrdinalIgnoreCase) ||
                         trimmedLine.StartsWith("GROUP BY", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(trimmedLine);
                }
                else if (trimmedLine.Contains("JOIN"))
                {
                    result.Add("  " + trimmedLine);
                }
                else
                {
                    result.Add("  " + trimmedLine);
                }
            }

            return string.Join("\n", result);
        }

        /// <summary>
        /// 清空表复选框选中事件
        /// </summary>
        private void ClearTableCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("用户选择插入前清空表");
                
                // 显示确认对话框
                var result = MessageBox.Show(
                    "选中此选项将在插入数据前清空目标表的所有数据，此操作不可撤销。\n\n确定要继续吗？",
                    "确认清空表",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.No)
                {
                    // 用户取消，取消选中
                    ClearTableCheckBox.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空表选项处理失败");
                MessageBox.Show($"清空表选项处理失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 清空表复选框取消选中事件
        /// </summary>
        private void ClearTableCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("用户取消选择插入前清空表");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空表选项处理失败");
            }
        }

        /// <summary>
        /// 添加参数按钮事件
        /// </summary>
        private void AddParameterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddParameterToPanel();
                _logger.LogInformation("添加新参数完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加参数失败");
                MessageBox.Show($"添加参数失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 添加参数到参数面板
        /// </summary>
        private void AddParameterToPanel()
        {
            var parameterBorder = new Border
            {
                Style = FindResource("ParameterItemStyle") as Style
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            // 参数标题（带图标）
            var titlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            
            var iconBlock = new TextBlock
            {
                Text = "⚙️",
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            
            var titleBlock = new TextBlock
            {
                Text = "参数配置",
                Style = FindResource("ParameterTitleStyle") as Style
            };
            
            titlePanel.Children.Add(iconBlock);
            titlePanel.Children.Add(titleBlock);
            Grid.SetRow(titlePanel, 0);
            Grid.SetColumn(titlePanel, 0);
            grid.Children.Add(titlePanel);

            // 删除按钮
            var deleteButton = new Button
            {
                Content = "删除",
                Style = FindResource("DangerButtonStyle") as Style
            };
            deleteButton.Click += (s, e) =>
            {
                ParametersPanel.Children.Remove(parameterBorder);
            };
            Grid.SetRow(deleteButton, 0);
            Grid.SetColumn(deleteButton, 1);
            grid.Children.Add(deleteButton);

            // 参数配置行：参数名称和默认值水平排列（完全匹配XAML布局）
            var configGrid = new Grid();
            configGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // 参数名称标签
            configGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 参数名称输入框
            configGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // 默认值标签
            configGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 默认值输入框

            // 参数名称标签
            var nameLabel = new TextBlock
            {
                Text = "参数名称:",
                Style = FindResource("ParameterLabelStyle") as Style
            };
            Grid.SetColumn(nameLabel, 0);
            configGrid.Children.Add(nameLabel);

            // 参数名称输入框
            var nameTextBox = new TextBox
            {
                Style = FindResource("ParameterInputStyle") as Style,
                Text = "@parameter",
                Margin = new Thickness(0, 0, 16, 0)
            };
            Grid.SetColumn(nameTextBox, 1);
            configGrid.Children.Add(nameTextBox);

            // 默认值标签
            var defaultValueLabel = new TextBlock
            {
                Text = "默认值:",
                Style = FindResource("ParameterLabelStyle") as Style
            };
            Grid.SetColumn(defaultValueLabel, 2);
            configGrid.Children.Add(defaultValueLabel);

            // 默认值输入框
            var defaultValueTextBox = new TextBox
            {
                Style = FindResource("ParameterInputStyle") as Style,
                Text = ""
            };
            Grid.SetColumn(defaultValueTextBox, 3);
            configGrid.Children.Add(defaultValueTextBox);

            Grid.SetRow(configGrid, 1);
            Grid.SetColumn(configGrid, 0);
            Grid.SetColumnSpan(configGrid, 2);
            grid.Children.Add(configGrid);

            parameterBorder.Child = grid;
            ParametersPanel.Children.Add(parameterBorder);
        }

        /// <summary>
        /// 执行测试查询按钮事件
        /// </summary>
        private async void TestQueryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await TestQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行测试查询失败");
                MessageBox.Show($"执行测试查询失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 执行测试查询
        /// </summary>
        private async Task TestQueryAsync()
        {
            var sqlStatement = SqlEditor?.SqlText?.Trim();
            if (string.IsNullOrWhiteSpace(sqlStatement))
            {
                MessageBox.Show("请先输入SQL语句", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 获取当前选中的数据源ID
            string dataSourceId = null;
            if (!string.IsNullOrEmpty(_currentDataSourceName))
            {
                var dataSourceConfigs = await _dataSourceService.GetAllDataSourcesAsync();
                var selectedDataSource = dataSourceConfigs.FirstOrDefault(ds => ds.Name == _currentDataSourceName);
                if (selectedDataSource != null)
                {
                    dataSourceId = selectedDataSource.Id;
                }
            }

            // 收集界面上的参数
            var parameters = CollectParametersFromPanel();
            var parametersDict = new Dictionary<string, object>();
            
            foreach (var param in parameters)
            {
                if (!string.IsNullOrWhiteSpace(param.Name) && !string.IsNullOrWhiteSpace(param.DefaultValue))
                {
                    // 尝试解析参数值
                    if (int.TryParse(param.DefaultValue, out int intValue))
                    {
                        parametersDict[param.Name] = intValue;
                    }
                    else if (double.TryParse(param.DefaultValue, out double doubleValue))
                    {
                        parametersDict[param.Name] = doubleValue;
                    }
                    else if (bool.TryParse(param.DefaultValue, out bool boolValue))
                    {
                        parametersDict[param.Name] = boolValue;
                    }
                    else if (DateTime.TryParse(param.DefaultValue, out DateTime dateValue))
                    {
                        parametersDict[param.Name] = dateValue;
                    }
                    else
                    {
                        // 默认为字符串
                        parametersDict[param.Name] = param.DefaultValue;
                    }
                }
            }

            // 执行测试查询（限制返回5行）
            var testSql = $"SELECT TOP 5 * FROM ({sqlStatement.TrimEnd(';')}) AS TestQuery";
            
            try
            {
                var testResult = await _sqlService.TestSqlStatementAsync(testSql, dataSourceId, parametersDict);
                
                if (testResult.IsSuccess)
                {
                    var resultText = $"测试查询执行成功！\n\n" +
                                   $"预计执行时间：{testResult.EstimatedDurationMs}ms\n" +
                                   $"预计返回行数：{testResult.EstimatedRowCount}行\n\n";

                    if (testResult.Columns?.Any() == true)
                    {
                        resultText += "列信息：\n";
                        foreach (var column in testResult.Columns)
                        {
                            resultText += $"- {column.Name} ({column.DataType})\n";
                        }
                    }

                    if (testResult.SampleData?.Any() == true)
                    {
                        resultText += "\n示例数据（前5行）：\n";
                        foreach (var row in testResult.SampleData.Take(3))
                        {
                            resultText += "{ " + string.Join(", ", row.Select(kv => $"{kv.Key}: {kv.Value}")) + " }\n";
                        }
                    }

                    MessageBox.Show(resultText, "测试成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"测试查询失败：{testResult.ErrorMessage}", "测试失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"测试查询异常：{ex.Message}", "异常", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        #endregion

        #region SQL编辑器事件处理

        /// <summary>
        /// SQL编辑器文本改变事件
        /// </summary>
        private void SqlEditor_TextChanged(object sender, string sqlText)
        {
            try
            {
                // 这里可以添加实时语法检查或其他逻辑
                _logger?.LogDebug("SQL文本已改变，长度: {Length}", sqlText.Length);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SQL文本改变事件处理失败");
            }
        }

        /// <summary>
        /// SQL编辑器格式化请求事件
        /// </summary>
        private void SqlEditor_FormatRequested(object sender, EventArgs e)
        {
            try
            {
                _logger?.LogInformation("SQL编辑器请求格式化");
                // 格式化功能已在SqlEditor内部实现
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SQL格式化请求事件处理失败");
            }
        }

        /// <summary>
        /// SQL编辑器验证请求事件
        /// </summary>
        private void SqlEditor_ValidateRequested(object sender, string sqlText)
        {
            try
            {
                _logger?.LogInformation("SQL编辑器请求语法验证");
                // 可以在这里调用外部的SQL验证服务
                // 例如：var result = await _sqlService.ValidateSqlSyntaxAsync(sqlText);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SQL验证请求事件处理失败");
            }
        }

        #endregion

        #region 配置值获取方法

        /// <summary>
        /// 获取超时时间
        /// </summary>
        private int GetTimeoutSeconds()
        {
            if (int.TryParse(TimeoutTextBox?.Text, out int timeout) && timeout > 0)
            {
                return timeout;
            }
            return 300; // 默认5分钟
        }

        /// <summary>
        /// 获取最大行数
        /// </summary>
        private int GetMaxRows()
        {
            if (int.TryParse(MaxRowsTextBox?.Text, out int maxRows) && maxRows > 0)
            {
                return maxRows;
            }
            return 10000; // 默认10000行
        }

        #region 新增控件事件处理方法

        /// <summary>
        /// 输出数据源选择改变事件 - 已隐藏，不再需要
        /// </summary>
        // private void OutputDataSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        // {
        //     try
        //     {
        //         var selectedItem = OutputDataSourceComboBox?.SelectedItem as ComboBoxItem;
        //         var dataSourceName = selectedItem?.Content?.ToString();

        //         if (!string.IsNullOrEmpty(dataSourceName))
        //         {
        //         _logger.LogInformation("输出数据源已更改为: {DataSourceName}", dataSourceName);
        //         // 这里可以添加加载对应数据源的表列表的逻辑
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "输出数据源选择改变事件处理失败");
        //     }
        // }

        /// <summary>
        /// 清空Sheet页复选框选中事件
        /// </summary>
        private void ClearSheetCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("清空Sheet页选项已选中");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空Sheet页复选框选中事件处理失败");
            }
        }

        /// <summary>
        /// 清空Sheet页复选框取消选中事件
        /// </summary>
        private void ClearSheetCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("清空Sheet页选项已取消选中");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空Sheet页复选框取消选中事件处理失败");
            }
        }

        /// <summary>
        /// 设置数据源选择
        /// </summary>
        /// <param name="comboBox">下拉框控件</param>
        /// <param name="dataSourceName">数据源名称</param>
        private void SetDataSourceSelection(ComboBox comboBox, string dataSourceName)
        {
            try
            {
                foreach (var item in comboBox.Items)
                {
                    if (item.ToString() == dataSourceName)
                    {
                        comboBox.SelectedItem = item;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置数据源选择失败: {DataSourceName}", dataSourceName);
            }
        }

        #endregion

        #endregion
    }

    #region SQL项目数据模型

    /// <summary>
    /// SQL项目数据模型
    /// </summary>
    public class SqlItem : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _category = string.Empty;
        private string _outputType = string.Empty;
        private string _outputTarget = string.Empty;
        private string _description = string.Empty;
        private string _sqlStatement = string.Empty;
        private DateTime _createdDate;
        private DateTime _lastModified;
        private List<SqlParameter> _parameters = new List<SqlParameter>();

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string Category
        {
            get => _category;
            set
            {
                _category = value;
                OnPropertyChanged(nameof(Category));
            }
        }

        public string OutputType
        {
            get => _outputType;
            set
            {
                _outputType = value;
                OnPropertyChanged(nameof(OutputType));
            }
        }

        public string OutputTarget
        {
            get => _outputTarget;
            set
            {
                _outputTarget = value;
                OnPropertyChanged(nameof(OutputTarget));
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public string SqlStatement
        {
            get => _sqlStatement;
            set
            {
                _sqlStatement = value;
                OnPropertyChanged(nameof(SqlStatement));
            }
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                _createdDate = value;
                OnPropertyChanged(nameof(CreatedDate));
            }
        }

        public DateTime LastModified
        {
            get => _lastModified;
            set
            {
                _lastModified = value;
                OnPropertyChanged(nameof(LastModified));
            }
        }

        public List<SqlParameter> Parameters
        {
            get => _parameters;
            set
            {
                _parameters = value;
                OnPropertyChanged(nameof(Parameters));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #endregion
} 