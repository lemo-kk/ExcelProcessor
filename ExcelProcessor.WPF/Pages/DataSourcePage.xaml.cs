using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Models;
using System.Threading.Tasks;

namespace ExcelProcessor.WPF.Pages
{
    public partial class DataSourcePage : Page, INotifyPropertyChanged
    {
        private readonly ILogger<DataSourcePage> _logger;
        private readonly IDataSourceService _dataSourceService;
        private ObservableCollection<DataSourceConfig> _dataSources;
        private DataSourceConfig _currentDataSource;
        private bool _isEditMode;
        private string _searchKeyword = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<DataSourceConfig> DataSources
        {
            get => _dataSources;
            set
            {
                _dataSources = value;
                OnPropertyChanged(nameof(DataSources));
                UpdateDataSourceCount();
            }
        }

        public DataSourcePage(IDataSourceService dataSourceService, ILogger<DataSourcePage> logger)
        {
            InitializeComponent();
            
            _dataSourceService = dataSourceService;
            _logger = logger;
            
            _ = InitializeData();
            SetupEventHandlers();
            DataContext = this;
        }

        private async Task InitializeData()
        {
            try
            {
                var dataSources = await _dataSourceService.GetAllDataSourcesAsync();
                DataSources = new ObservableCollection<DataSourceConfig>(dataSources);

            _logger.LogInformation("数据源配置页面数据初始化完成，共{Count}个数据源", DataSources.Count);
                
                // 初始化完成后应用搜索过滤
                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化数据源数据失败");
                MessageBox.Show($"初始化数据源数据失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                
                // 使用空集合
                DataSources = new ObservableCollection<DataSourceConfig>();
            }
        }

        private void SetupEventHandlers()
        {
            // 数据源列表绑定
            DataSourceList.ItemsSource = DataSources;
            
            // 数据源类型选择事件
            DataSourceTypeComboBox.SelectionChanged += DataSourceTypeComboBox_SelectionChanged;
            
            // 对话框事件
            DataSourceDialog.Closed += DataSourceDialog_Closed;
            DataSourceDialog.Opened += DataSourceDialog_Opened;
        }

        private void UpdateDataSourceCount()
        {
            DataSourceCountTextBlock.Text = DataSources.Count.ToString();
        }

        #region 搜索功能

        /// <summary>
        /// 搜索文本框文本变化事件
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchKeyword = SearchTextBox.Text?.Trim() ?? string.Empty;
            ApplySearchFilter();
        }

        /// <summary>
        /// 清除搜索按钮点击事件
        /// </summary>
        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            _searchKeyword = string.Empty;
            ApplySearchFilter();
        }

        /// <summary>
        /// 应用搜索过滤
        /// </summary>
        private void ApplySearchFilter()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_searchKeyword))
                {
                    // 没有搜索关键词，显示所有数据源
                    DataSourceList.ItemsSource = DataSources;
                    ClearSearchButton.Visibility = Visibility.Collapsed;
                    UpdateFilteredCount(DataSources.Count, false);
                }
                else
                {
                    // 有搜索关键词，过滤数据源
                    var filteredDataSources = DataSources.Where(dataSource =>
                        ContainsKeyword(dataSource.Name, _searchKeyword) ||
                        ContainsKeyword(dataSource.Type, _searchKeyword) ||
                        ContainsKeyword(dataSource.Description, _searchKeyword) ||
                        ContainsKeyword(dataSource.ConnectionString, _searchKeyword)
                    ).ToList();

                    var filteredCollection = new ObservableCollection<DataSourceConfig>(filteredDataSources);
                    DataSourceList.ItemsSource = filteredCollection;
                    ClearSearchButton.Visibility = Visibility.Visible;
                    UpdateFilteredCount(filteredDataSources.Count, true);
                }

                _logger.LogInformation("应用搜索过滤，关键词: {Keyword}, 过滤后数量: {Count}", 
                    _searchKeyword, DataSourceList.ItemsSource is ObservableCollection<DataSourceConfig> collection ? collection.Count : 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用搜索过滤失败");
            }
        }

        /// <summary>
        /// 检查文本是否包含关键词（不区分大小写）
        /// </summary>
        private bool ContainsKeyword(string text, string keyword)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
                return false;

            return text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 更新过滤后的数量显示
        /// </summary>
        private void UpdateFilteredCount(int filteredCount, bool isFiltered)
        {
            if (isFiltered)
            {
                FilteredCountTextBlock.Text = $"（已过滤: {filteredCount}）";
                FilteredCountTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                FilteredCountTextBlock.Text = string.Empty;
                FilteredCountTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region 按钮事件处理

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("刷新数据源列表");
                
                // 重新加载数据
                await InitializeData();
                
                // 重新应用搜索过滤
                ApplySearchFilter();
                
                MessageBox.Show("数据源列表已刷新", "刷新完成", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新数据源列表失败");
                MessageBox.Show($"刷新数据源列表失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddDataSourceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("打开添加数据源对话框");
                
                _isEditMode = false;
                _currentDataSource = new DataSourceConfig
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "",
                    Type = "SQLite",
                    Description = "",
                    ConnectionString = "",
                    IsConnected = false,
                    Status = "未配置",
                    LastTestTime = DateTime.MinValue,
                    IsEnabled = true
                };

                ShowDataSourceDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开添加数据源对话框失败");
                MessageBox.Show($"打开添加数据源对话框失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 数据源操作事件

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is DataSourceConfig dataSource)
                {
                    _logger.LogInformation("测试数据源连接: {DataSourceName}", dataSource.Name);
                
                    var (isConnected, errorMessage) = await _dataSourceService.TestConnectionWithDetailsAsync(dataSource);

                    if (isConnected)
                {
                        dataSource.IsConnected = true;
                        dataSource.Status = "已连接";
                        ShowTopLevelSystemMessageBox($"数据源 '{dataSource.Name}' 连接测试成功！", "测试成功", true);
                    }
                    else
                    {
                        dataSource.IsConnected = false;
                        dataSource.Status = "连接失败";
                        var errorMsg = string.IsNullOrEmpty(errorMessage) 
                            ? "连接测试失败，请检查连接信息！" 
                            : $"连接测试失败：{errorMessage}";
                        ShowTopLevelSystemMessageBox($"数据源 '{dataSource.Name}' {errorMsg}", "测试失败", false);
                    }
                    
                    dataSource.LastTestTime = DateTime.Now;
                    
                    // 更新数据库中的状态
                    await _dataSourceService.UpdateDataSourceAsync(dataSource);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试数据源连接失败");
                ShowTopLevelSystemMessageBox($"测试数据源连接失败: {ex.Message}", "错误", false);
            }
        }

        private void EditDataSourceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is DataSourceConfig dataSource)
                {
                    _logger.LogInformation("编辑数据源: {DataSourceName}", dataSource.Name);
                    
                    _isEditMode = true;
                    _currentDataSource = dataSource.Clone();
                    
                    ShowDataSourceDialog();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "编辑数据源失败");
                ShowTopLevelSystemMessageBox($"编辑数据源失败: {ex.Message}", "错误", false);
            }
        }

        private async void DeleteDataSourceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is DataSourceConfig dataSource)
                {
                    var result = ShowTopLevelConfirmDialog($"确定要删除数据源 '{dataSource.Name}' 吗？", "确认删除");
                    
                    if (result)
                    {
                        var success = await _dataSourceService.DeleteDataSourceAsync(dataSource.Id);
                        
                        if (success)
                        {
                            DataSources.Remove(dataSource);
                            
                            _logger.LogInformation("删除数据源: {DataSourceName}", dataSource.Name);
                            ShowTopLevelSystemMessageBox("数据源已删除", "删除成功", true);
                        }
                        else
                        {
                            ShowTopLevelSystemMessageBox("删除数据源失败", "删除失败", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除数据源失败");
                ShowTopLevelSystemMessageBox($"删除数据源失败: {ex.Message}", "错误", false);
            }
        }

        /// <summary>
        /// 设置默认数据库按钮点击事件
        /// </summary>
        private async void SetDefaultDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is DataSourceConfig dataSource)
                {
                    // 确认设置默认数据库
                    var result = ShowTopLevelConfirmDialog(
                        $"确定要将 '{dataSource.Name}' 设为默认数据库吗？\n\n注意：设置新的默认数据库会取消之前的默认数据库设置。", 
                        "设置默认数据库");
                    
                    if (result)
                    {
                        // 先取消所有数据源的默认状态
                        foreach (var ds in DataSources)
                        {
                            if (ds.IsDefault)
                            {
                                ds.IsDefault = false;
                                await _dataSourceService.UpdateDataSourceAsync(ds);
                            }
                        }

                        // 设置当前数据源为默认
                        dataSource.IsDefault = true;
                        var success = await _dataSourceService.UpdateDataSourceAsync(dataSource);
                        
                        if (success)
                        {
                            _logger.LogInformation("设置默认数据库: {DataSourceName}", dataSource.Name);
                            ShowTopLevelSystemMessageBox($"已将 '{dataSource.Name}' 设为默认数据库", "设置成功", true);
                        }
                        else
                        {
                            ShowTopLevelSystemMessageBox("设置默认数据库失败", "设置失败", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置默认数据库失败");
                ShowTopLevelSystemMessageBox($"设置默认数据库失败: {ex.Message}", "错误", false);
            }
        }

        /// <summary>
        /// 取消默认数据库按钮点击事件
        /// </summary>
        private async void RemoveDefaultDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is DataSourceConfig dataSource)
                {
                    // 确认取消默认数据库
                    var result = ShowTopLevelConfirmDialog(
                        $"确定要取消 '{dataSource.Name}' 的默认数据库设置吗？", 
                        "取消默认数据库");
                    
                    if (result)
                    {
                        dataSource.IsDefault = false;
                        var success = await _dataSourceService.UpdateDataSourceAsync(dataSource);
                        
                        if (success)
                        {
                            _logger.LogInformation("取消默认数据库: {DataSourceName}", dataSource.Name);
                            ShowTopLevelSystemMessageBox($"已取消 '{dataSource.Name}' 的默认数据库设置", "取消成功", true);
                        }
                        else
                        {
                            ShowTopLevelSystemMessageBox("取消默认数据库设置失败", "取消失败", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消默认数据库失败");
                ShowTopLevelSystemMessageBox($"取消默认数据库失败: {ex.Message}", "错误", false);
            }
        }

        #endregion

        #region 对话框事件处理

        private async void ShowDataSourceDialog()
        {
            try
            {
                // 设置对话框标题
                DialogTitle.Text = _isEditMode ? "编辑数据源" : "添加数据源";
                
                // 填充表单数据
                DataSourceNameTextBox.Text = _currentDataSource.Name;
                DataSourceDescriptionTextBox.Text = _currentDataSource.Description;
                
                // 设置数据源类型
                foreach (ComboBoxItem item in DataSourceTypeComboBox.Items)
                {
                    if (item.Tag.ToString() == _currentDataSource.Type)
                    {
                        DataSourceTypeComboBox.SelectedItem = item;
                        break;
                    }
                }
                
                // 显示对应的连接面板
                ShowConnectionPanel(_currentDataSource.Type);
                
                // 如果处于编辑模式，解析连接字符串并填充表单
                if (_isEditMode && !string.IsNullOrEmpty(_currentDataSource.ConnectionString))
                {
                    ParseConnectionString(_currentDataSource.ConnectionString, _currentDataSource.Type);
                }
                
                // 显示对话框
                DataSourceDialog.IsOpen = true;
                
                // 延迟设置焦点，确保对话框完全加载
                await Task.Delay(100);
                DataSourceNameTextBox.Focus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示数据源对话框失败");
                ShowTopLevelSystemMessageBox($"显示数据源对话框失败: {ex.Message}", "错误", false);
            }
        }

        private async void TestConnectionDialogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var connectionString = BuildConnectionString();
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    ShowTopLevelSystemMessageBox("请先填写完整的连接信息", "提示", false);
                    return;
                }

                // 创建临时数据源进行测试
                var tempDataSource = new DataSourceConfig
                {
                    Name = "测试连接",
                    Type = GetSelectedDataSourceType(),
                    ConnectionString = connectionString
                };

                var (isConnected, errorMessage) = await _dataSourceService.TestConnectionWithDetailsAsync(tempDataSource);
                
                if (isConnected)
                {
                    ShowTopLevelSystemMessageBox("连接测试成功！", "测试成功", true);
                }
                else
                {
                    var errorMsg = string.IsNullOrEmpty(errorMessage) 
                        ? "连接测试失败，请检查连接信息！" 
                        : $"连接测试失败：{errorMessage}";
                    ShowTopLevelSystemMessageBox(errorMsg, "测试失败", false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试连接失败");
                ShowTopLevelSystemMessageBox($"测试连接失败: {ex.Message}", "错误", false);
            }
        }

        private async void SaveDataSourceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证表单
                if (string.IsNullOrWhiteSpace(DataSourceNameTextBox.Text))
                {
                    ShowTopLevelSystemMessageBox("请输入数据源名称", "验证失败", false);
                    return;
                }

                var connectionString = BuildConnectionString();
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    ShowTopLevelSystemMessageBox("请填写完整的连接信息", "验证失败", false);
                    return;
                }

                // 检查名称是否已存在
                var nameExists = await _dataSourceService.IsDataSourceNameExistsAsync(
                    DataSourceNameTextBox.Text.Trim(), 
                    _isEditMode ? _currentDataSource.Id : null);
                
                if (nameExists)
                {
                    ShowTopLevelSystemMessageBox("数据源名称已存在，请使用其他名称", "验证失败", false);
                    return;
                }

                // 更新数据源信息
                _currentDataSource.Name = DataSourceNameTextBox.Text.Trim();
                _currentDataSource.Description = DataSourceDescriptionTextBox.Text.Trim();
                _currentDataSource.ConnectionString = connectionString;
                _currentDataSource.Type = GetSelectedDataSourceType();

                bool success;
                if (_isEditMode)
                {
                    // 编辑模式：更新现有数据源
                    success = await _dataSourceService.UpdateDataSourceAsync(_currentDataSource);
                    
                    if (success)
                    {
                        // 更新本地集合中的数据
                    var existingDataSource = DataSources.FirstOrDefault(ds => ds.Id == _currentDataSource.Id);
                    if (existingDataSource != null)
                    {
                        existingDataSource.Name = _currentDataSource.Name;
                        existingDataSource.Description = _currentDataSource.Description;
                        existingDataSource.ConnectionString = _currentDataSource.ConnectionString;
                        existingDataSource.Type = _currentDataSource.Type;
                        existingDataSource.LastTestTime = DateTime.Now;
                        }
                    }
                    
                    _logger.LogInformation("更新数据源: {DataSourceName}", _currentDataSource.Name);
                }
                else
                {
                    // 添加模式：添加新数据源
                    _currentDataSource.Status = "未测试";
                    _currentDataSource.LastTestTime = DateTime.MinValue;
                    
                    success = await _dataSourceService.SaveDataSourceAsync(_currentDataSource);
                    
                    if (success)
                    {
                    DataSources.Add(_currentDataSource);
                    }
                    
                    _logger.LogInformation("添加数据源: {DataSourceName}", _currentDataSource.Name);
                }

                // 如果保存成功，显示成功提示并关闭对话框
                if (success)
                {
                    // 使用系统原生消息框显示成功信息
                    ShowTopLevelSystemMessageBox(
                        _isEditMode ? "数据源更新成功！" : "数据源添加成功！",
                        "保存成功", 
                        true
                    );
                    
                    // 清理表单并关闭对话框
                    ClearDataSourceForm();
                DataSourceDialog.IsOpen = false;
                }
                else
                {
                    // 保存失败，显示错误信息
                    ShowTopLevelSystemMessageBox(_isEditMode ? "数据源更新失败" : "数据源添加失败", 
                        "操作失败", false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存数据源失败");
                ShowTopLevelSystemMessageBox($"保存数据源失败: {ex.Message}", "错误", false);
            }
        }

        private void CancelDataSourceButton_Click(object sender, RoutedEventArgs e)
        {
            ClearDataSourceForm();
            DataSourceDialog.IsOpen = false;
        }

        private void DataSourceDialog_Closed(object sender, EventArgs e)
        {
            // 确保对话框关闭后清理表单状态
            ClearDataSourceForm();
        }

        private void DataSourceDialog_Opened(object sender, EventArgs e)
        {
            // 确保对话框打开时焦点正确设置
            Dispatcher.BeginInvoke(new Action(() =>
            {
                DataSourceNameTextBox.Focus();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void ClearDataSourceForm()
        {
            try
            {
                // 清理所有输入框
                DataSourceNameTextBox.Text = string.Empty;
                DataSourceDescriptionTextBox.Text = string.Empty;
                
                // 清理SQLite输入框
                SQLiteFilePathTextBox.Text = string.Empty;
                
                // 清理MySQL输入框
                MySQLServerTextBox.Text = string.Empty;
                MySQLPortTextBox.Text = "3306";
                MySQLDatabaseTextBox.Text = string.Empty;
                MySQLUsernameTextBox.Text = string.Empty;
                MySQLPasswordBox.Password = string.Empty;
                
                // 清理SQL Server输入框
                SQLServerServerTextBox.Text = string.Empty;
                SQLServerPortTextBox.Text = "1433";
                SQLServerDatabaseTextBox.Text = string.Empty;
                SQLServerUsernameTextBox.Text = string.Empty;
                SQLServerPasswordBox.Password = string.Empty;
                
                // 清理PostgreSQL输入框
                PostgreSQLServerTextBox.Text = string.Empty;
                PostgreSQLPortTextBox.Text = "5432";
                PostgreSQLDatabaseTextBox.Text = string.Empty;
                PostgreSQLUsernameTextBox.Text = string.Empty;
                PostgreSQLPasswordBox.Password = string.Empty;
                
                // 清理Oracle输入框
                OracleServerTextBox.Text = string.Empty;
                OraclePortTextBox.Text = "1521";
                OracleServiceNameTextBox.Text = string.Empty;
                OracleUsernameTextBox.Text = string.Empty;
                OraclePasswordBox.Password = string.Empty;
                
                // 重置数据源类型选择
                DataSourceTypeComboBox.SelectedIndex = 0;
                
                // 隐藏所有连接面板
                ShowConnectionPanel("SQLite");
                
                // 重置编辑模式
                _isEditMode = false;
                _currentDataSource = null;
                
                // 清除焦点
                DataSourceNameTextBox.Focus();
                
                _logger.LogInformation("数据源表单已清理");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理数据源表单失败");
            }
        }

        private void DataSourceTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedType = GetSelectedDataSourceType();
                ShowConnectionPanel(selectedType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换数据源类型失败");
            }
        }

        private void ShowConnectionPanel(string dataSourceType)
        {
            // 隐藏所有连接面板
            SQLiteConnectionPanel.Visibility = Visibility.Collapsed;
            MySQLConnectionPanel.Visibility = Visibility.Collapsed;
            SQLServerConnectionPanel.Visibility = Visibility.Collapsed;
            PostgreSQLConnectionPanel.Visibility = Visibility.Collapsed;
            OracleConnectionPanel.Visibility = Visibility.Collapsed;

            // 显示对应的连接面板
            switch (dataSourceType)
            {
                case "SQLite":
                    SQLiteConnectionPanel.Visibility = Visibility.Visible;
                    break;
                case "MySQL":
                    MySQLConnectionPanel.Visibility = Visibility.Visible;
                    break;
                case "SQLServer":
                    SQLServerConnectionPanel.Visibility = Visibility.Visible;
                    break;
                case "PostgreSQL":
                    PostgreSQLConnectionPanel.Visibility = Visibility.Visible;
                    break;
                case "Oracle":
                    OracleConnectionPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        #endregion

        #region 辅助方法

        private string GetSelectedDataSourceType()
        {
            if (DataSourceTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Tag.ToString();
            }
            return "SQLite";
        }

        private string BuildConnectionString()
        {
            var dataSourceType = GetSelectedDataSourceType();
            
            switch (dataSourceType)
            {
                case "SQLite":
                    return BuildSQLiteConnectionString();
                case "MySQL":
                    return BuildMySQLConnectionString();
                case "SQLServer":
                    return BuildSQLServerConnectionString();
                case "PostgreSQL":
                    return BuildPostgreSQLConnectionString();
                case "Oracle":
                    return BuildOracleConnectionString();
                default:
                    return string.Empty;
            }
        }

        private string BuildSQLiteConnectionString()
        {
            var filePath = SQLiteFilePathTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(filePath))
                return string.Empty;
                
            return $"Data Source={filePath}";
        }

        private string BuildMySQLConnectionString()
        {
            var server = MySQLServerTextBox.Text?.Trim();
            var port = MySQLPortTextBox.Text?.Trim();
            var database = MySQLDatabaseTextBox.Text?.Trim();
            var username = MySQLUsernameTextBox.Text?.Trim();
            var password = MySQLPasswordBox.Password?.Trim();
            
            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database) || 
                string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return string.Empty;
                
            return $"Server={server};Port={port};Database={database};Uid={username};Pwd={password};";
        }

        private string BuildSQLServerConnectionString()
        {
            var server = SQLServerServerTextBox.Text?.Trim();
            var port = SQLServerPortTextBox.Text?.Trim();
            var database = SQLServerDatabaseTextBox.Text?.Trim();
            var username = SQLServerUsernameTextBox.Text?.Trim();
            var password = SQLServerPasswordBox.Password?.Trim();
            
            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database) || 
                string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return string.Empty;
                
            return $"Server={server},{port};Database={database};User Id={username};Password={password};";
        }

        private string BuildPostgreSQLConnectionString()
        {
            var server = PostgreSQLServerTextBox.Text?.Trim();
            var port = PostgreSQLPortTextBox.Text?.Trim();
            var database = PostgreSQLDatabaseTextBox.Text?.Trim();
            var username = PostgreSQLUsernameTextBox.Text?.Trim();
            var password = PostgreSQLPasswordBox.Password?.Trim();
            
            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database) || 
                string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return string.Empty;
                
            return $"Host={server};Port={port};Database={database};Username={username};Password={password};";
        }

        private string BuildOracleConnectionString()
        {
            var server = OracleServerTextBox.Text?.Trim();
            var port = OraclePortTextBox.Text?.Trim();
            var serviceName = OracleServiceNameTextBox.Text?.Trim();
            var username = OracleUsernameTextBox.Text?.Trim();
            var password = OraclePasswordBox.Password?.Trim();
            
            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(serviceName) || 
                string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return string.Empty;
                
            return $"Data Source={server}:{port}/{serviceName};User Id={username};Password={password};";
        }

        private void ParseConnectionString(string connectionString, string dataSourceType)
        {
            try
            {
                switch (dataSourceType)
                {
                    case "SQLite":
                        ParseSQLiteConnectionString(connectionString);
                        break;
                    case "MySQL":
                        ParseMySQLConnectionString(connectionString);
                        break;
                    case "SQLServer":
                        ParseSQLServerConnectionString(connectionString);
                        break;
                    case "PostgreSQL":
                        ParsePostgreSQLConnectionString(connectionString);
                        break;
                    case "Oracle":
                        ParseOracleConnectionString(connectionString);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析连接字符串失败");
            }
        }

        private void ParseSQLiteConnectionString(string connectionString)
        {
            var parameters = ParseConnectionStringParameters(connectionString);
            if (parameters.ContainsKey("Data Source"))
            {
                SQLiteFilePathTextBox.Text = parameters["Data Source"];
            }
        }

        private void ParseMySQLConnectionString(string connectionString)
        {
            var parameters = ParseConnectionStringParameters(connectionString);
            if (parameters.ContainsKey("Server")) MySQLServerTextBox.Text = parameters["Server"];
            if (parameters.ContainsKey("Port")) MySQLPortTextBox.Text = parameters["Port"];
            if (parameters.ContainsKey("Database")) MySQLDatabaseTextBox.Text = parameters["Database"];
            if (parameters.ContainsKey("Uid")) MySQLUsernameTextBox.Text = parameters["Uid"];
            if (parameters.ContainsKey("Pwd")) MySQLPasswordBox.Password = parameters["Pwd"];
        }

        private void ParseSQLServerConnectionString(string connectionString)
        {
            var parameters = ParseConnectionStringParameters(connectionString);
            if (parameters.ContainsKey("Server")) SQLServerServerTextBox.Text = parameters["Server"];
            if (parameters.ContainsKey("Port")) SQLServerPortTextBox.Text = parameters["Port"];
            if (parameters.ContainsKey("Database")) SQLServerDatabaseTextBox.Text = parameters["Database"];
            if (parameters.ContainsKey("User Id")) SQLServerUsernameTextBox.Text = parameters["User Id"];
            if (parameters.ContainsKey("Password")) SQLServerPasswordBox.Password = parameters["Password"];
        }

        private void ParsePostgreSQLConnectionString(string connectionString)
        {
            var parameters = ParseConnectionStringParameters(connectionString);
            if (parameters.ContainsKey("Host")) PostgreSQLServerTextBox.Text = parameters["Host"];
            if (parameters.ContainsKey("Port")) PostgreSQLPortTextBox.Text = parameters["Port"];
            if (parameters.ContainsKey("Database")) PostgreSQLDatabaseTextBox.Text = parameters["Database"];
            if (parameters.ContainsKey("Username")) PostgreSQLUsernameTextBox.Text = parameters["Username"];
            if (parameters.ContainsKey("Password")) PostgreSQLPasswordBox.Password = parameters["Password"];
        }

        private void ParseOracleConnectionString(string connectionString)
        {
            var parameters = ParseConnectionStringParameters(connectionString);
            if (parameters.ContainsKey("Data Source")) 
            {
                var dataSource = parameters["Data Source"];
                var parts = dataSource.Split(':');
                if (parts.Length >= 2)
                {
                    OracleServerTextBox.Text = parts[0];
                    var portService = parts[1].Split('/');
                    if (portService.Length >= 2)
                    {
                        OraclePortTextBox.Text = portService[0];
                        OracleServiceNameTextBox.Text = portService[1];
                    }
                }
            }
            if (parameters.ContainsKey("User Id")) OracleUsernameTextBox.Text = parameters["User Id"];
            if (parameters.ContainsKey("Password")) OraclePasswordBox.Password = parameters["Password"];
        }

        private Dictionary<string, string> ParseConnectionStringParameters(string connectionString)
        {
            var parameters = new Dictionary<string, string>();
            var pairs = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    parameters[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }
            
            return parameters;
        }

        /// <summary>
        /// 显示最顶层消息框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <param name="isSuccess">是否为成功消息</param>
        private void ShowTopLevelMessageBox(string message, string title, bool isSuccess)
        {
            // 获取主窗口
            var mainWindow = Application.Current.MainWindow;
            
            // 创建自定义消息框窗口
            var messageBox = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
                BorderBrush = new SolidColorBrush(isSuccess ? Color.FromRgb(0, 255, 0) : Color.FromRgb(255, 165, 0)),
                BorderThickness = new Thickness(2),
                Owner = mainWindow, // 设置主窗口为父窗口
                Topmost = true, // 确保显示在最顶层
                ShowInTaskbar = false // 不在任务栏显示
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var messageText = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20)
            };

            var okButton = new Button
            {
                Content = "确定",
                Width = 80,
                Height = 30,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(0, 229, 255)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0)
            };

            okButton.Click += (s, args) => messageBox.Close();

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            buttonPanel.Children.Add(okButton);

            Grid.SetRow(messageText, 0);
            Grid.SetRow(buttonPanel, 1);

            grid.Children.Add(messageText);
            grid.Children.Add(buttonPanel);

            messageBox.Content = grid;
            messageBox.ShowDialog();
    }

    /// <summary>
        /// 显示最顶层确认对话框
    /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <returns>用户选择结果</returns>
        private bool ShowTopLevelConfirmDialog(string message, string title)
        {
            // 获取主窗口
            var mainWindow = Application.Current.MainWindow;
            bool result = false;
            
            // 创建自定义确认对话框窗口
            var confirmDialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                BorderThickness = new Thickness(2),
                Owner = mainWindow, // 设置主窗口为父窗口
                Topmost = true, // 确保显示在最顶层
                ShowInTaskbar = false // 不在任务栏显示
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var messageText = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20)
            };

            var yesButton = new Button
            {
                Content = "是",
                Width = 80,
                Height = 30,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(0, 229, 255)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0)
            };

            var noButton = new Button
            {
                Content = "否",
                Width = 80,
                Height = 30,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0)
            };

            yesButton.Click += (s, args) => 
            {
                result = true;
                confirmDialog.Close();
            };

            noButton.Click += (s, args) => 
            {
                result = false;
                confirmDialog.Close();
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);

            Grid.SetRow(messageText, 0);
            Grid.SetRow(buttonPanel, 1);

            grid.Children.Add(messageText);
            grid.Children.Add(buttonPanel);

            confirmDialog.Content = grid;
            confirmDialog.ShowDialog();
            
            return result;
        }

        /// <summary>
        /// 显示系统原生消息框，但确保显示在最顶层
        /// </summary>
        private void ShowTopLevelSystemMessageBox(string message, string title, bool isSuccess = true)
        {
            try
            {
                // 获取主窗口
                var mainWindow = Application.Current.MainWindow;
                
                // 设置主窗口为最顶层，确保消息框能显示在最顶层
                if (mainWindow != null)
                {
                    mainWindow.Topmost = true;
                }
                
                // 使用系统原生MessageBox
                var icon = isSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning;
                MessageBox.Show(message, title, MessageBoxButton.OK, icon);
                
                // 恢复主窗口的Topmost设置
                if (mainWindow != null)
                {
                    mainWindow.Topmost = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示系统消息框失败");
                // 如果失败，回退到普通MessageBox
                MessageBox.Show(message, title, MessageBoxButton.OK, 
                    isSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
        }

        #endregion
    }
} 