using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using ExcelProcessor.WPF.Controls;
using ExcelProcessor.WPF.Views;
using ExcelProcessor.WPF.Helpers;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Models;
using OfficeOpenXml;
using System.IO;
using System.Threading.Tasks;

namespace ExcelProcessor.WPF.Pages
{
    public partial class ExcelImportPage : Page
    {
        private readonly IExcelConfigService _excelConfigService;
        private readonly IExcelService _excelService;
        private ExcelConfig _currentEditingConfig;

        public ExcelImportPage(IExcelConfigService excelConfigService)
        {
            InitializeComponent();
            _excelConfigService = excelConfigService;
            // 解析 Excel 领域服务
            _excelService = App.Services.GetService(typeof(IExcelService)) as IExcelService;
            
            // 确保DataGrid只显示定义的列
            DataGridHelper.EnsureDefinedColumnsOnly(ExcelConfigDataGrid);
            
            LoadExcelConfigs();
            LoadFieldMappings();
            SetupEventHandlers();
            
            // 添加备用的事件绑定
            SetupBackupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            try
            {
                Console.WriteLine("=== 设置事件处理器 ===");
                
                // 移除手动绑定的事件处理器，因为XAML中已经绑定了
                // 这避免了重复调用的问题
                Console.WriteLine("事件处理器已在XAML中绑定，无需手动绑定");
            }
            catch (Exception ex)
                        {
                Console.WriteLine($"设置事件处理器时出错: {ex.Message}");
                        }
        }

        private void SetupBackupEventHandlers()
                        {
            // 此方法已不再需要，因为事件处理器已在XAML中绑定
        }

        private T FindVisualChild<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            // 此方法已不再需要，因为不再手动查找和绑定按钮
            return null;
        }

        private async void LoadExcelConfigs()
        {
            try
            {
                Console.WriteLine("=== 开始加载Excel配置 ===");
                
                var configs = await _excelConfigService.GetAllConfigsAsync();
                Console.WriteLine($"从数据库加载到 {configs?.Count ?? 0} 个配置");
                
                if (configs != null && configs.Count > 0)
                {
                    foreach (var config in configs)
                    {
                        Console.WriteLine($"配置: {config.ConfigName}, ID: {config.Id}, 文件: {config.FilePath}");
                    }
                }
                else
                {
                    Console.WriteLine("没有找到配置数据，加载示例数据");
                    LoadSampleData();
                    return;
                }
                
                // 确保DataGrid只显示定义的列
                DataGridHelper.EnsureDefinedColumnsOnly(ExcelConfigDataGrid);
                
                ExcelConfigDataGrid.ItemsSource = configs;
                Console.WriteLine("配置数据已设置到DataGrid");
                
                // 延迟设置事件处理器，确保UI元素已创建
                await Dispatcher.InvokeAsync(() =>
                {
                    SetupEventHandlers();
                }, System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配置列表失败：{ex.Message}");
                Extensions.MessageBoxExtensions.Show($"加载配置列表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadSampleData(); // 加载示例数据作为备选
            }
        }

        private void LoadSampleData()
        {
            // 不显示示例数据，显示空列表
            // 用户可以通过"新建配置"按钮来创建真实的配置
            var configs = new List<ExcelConfig>();
            ExcelConfigDataGrid.ItemsSource = configs;
        }

        private void LoadFieldMappings()
        {
            // 字段映射数据现在在弹出窗中处理
            // 这里保留方法以备将来使用
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("=== 编辑按钮被点击 ===");
                
            var button = sender as Button;
                if (button == null)
                {
                    Console.WriteLine("按钮对象为空");
                    Extensions.MessageBoxExtensions.Show("按钮对象为空", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                Console.WriteLine($"按钮名称: {button.Name}");
                Console.WriteLine($"按钮DataContext类型: {button.DataContext?.GetType().Name ?? "null"}");
                
            var config = button.DataContext as ExcelConfig;
                if (config == null)
                {
                    Console.WriteLine("配置数据为空");
                    Console.WriteLine($"DataContext内容: {button.DataContext}");
                    Extensions.MessageBoxExtensions.Show("配置数据为空，请检查数据绑定", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                Console.WriteLine($"配置名称: {config.ConfigName}");
                Console.WriteLine($"配置ID: {config.Id}");
                Console.WriteLine($"文件路径: {config.FilePath}");
                Console.WriteLine($"目标数据源: {config.TargetDataSourceName}");
                
                Console.WriteLine($"准备编辑配置: {config.ConfigName}");
            ShowConfigDialog(config, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"编辑按钮点击时发生异常: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                Extensions.MessageBoxExtensions.Show($"编辑配置时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var config = button.DataContext as ExcelConfig;
                
                if (config != null)
                {
                    var result = Extensions.MessageBoxExtensions.Show($"确定要删除配置 '{config.ConfigName}' 吗？", "确认删除", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        // 调用服务删除配置
                        var success = await _excelConfigService.DeleteConfigAsync(config.ConfigName);
                        
                        if (success)
                        {
                            Extensions.MessageBoxExtensions.Show($"配置 '{config.ConfigName}' 已删除", "删除成功", 
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // 刷新配置列表
                            LoadExcelConfigs();
                        }
                        else
                        {
                            Extensions.MessageBoxExtensions.Show($"删除配置 '{config.ConfigName}' 失败", "删除失败", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"删除配置时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowConfigDialog(ExcelConfig config, bool isTestMode)
        {
            try
            {
                Console.WriteLine("=== 开始显示配置对话框 ===");
                Console.WriteLine($"config: {(config == null ? "null (新建)" : config.ConfigName)}");
                Console.WriteLine($"isTestMode: {isTestMode}");
                
                var content = new ExcelImportConfigContent();
            var dialog = new ConfigDetailDialog();
                
                Console.WriteLine("创建对话框和内容控件成功");
            
            // 如果是编辑模式，加载现有配置数据
            if (config != null && !isTestMode)
            {
                    Console.WriteLine("加载现有配置数据");
                content.LoadConfig(config);
            }
                else if (config == null && !isTestMode)
                {
                    Console.WriteLine("新建配置模式，初始化默认值");
                    // 新建配置时设置一些默认值
                    content.SetConfigName($"新配置_{DateTime.Now:yyyyMMdd_HHmmss}");
                    content.SetHeaderRow("1");
                    content.SetSheetName("Sheet1");
                    content.SetSkipEmptyRows(true);
                    content.SetSplitEachRow(false);
                    content.SetClearTableDataBeforeImport(false);
                    // 设置默认目标表名
                    content.SetTargetTableName($"新表_{DateTime.Now:yyyyMMdd_HHmmss}");
                }
            
            // 记录当前编辑的配置（用于保存时区分新增/编辑）
            _currentEditingConfig = isTestMode ? null : config;
            
                // 设置对话框属性
                dialog.Title = isTestMode ? "测试配置" : (config == null ? "新建配置" : "编辑配置");
                dialog.Subtitle = isTestMode ? "验证配置是否正确" : 
                    (config == null ? "创建新的Excel导入配置" : $"编辑配置：{config.ConfigName}");
            dialog.Content = content;
            dialog.ShowTestButton = isTestMode;

                // 绑定按钮事件
                dialog.SaveClicked += (s, e) => {
                        try
                        {
                        Console.WriteLine("保存按钮被点击");
                            SaveConfig(content);
                        // 关闭对话框
                        if (dialog.Parent is Window parentWindow)
                        {
                            parentWindow.DialogResult = true;
                            parentWindow.Close();
                        }
                        }
                        catch (Exception ex)
                        {
                        Console.WriteLine($"保存配置时出错: {ex.Message}");
                            Extensions.MessageBoxExtensions.Show($"保存配置时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                };
                
                dialog.TestClicked += (s, e) => {
                        try
                        {
                        Console.WriteLine("测试按钮被点击");
                            TestConfig(content);
                        }
                        catch (Exception ex)
                        {
                        Console.WriteLine($"测试配置时出错: {ex.Message}");
                            Extensions.MessageBoxExtensions.Show($"测试配置时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                };
                
                dialog.CancelClicked += (s, e) => {
                        try
                        {
                        Console.WriteLine("取消按钮被点击");
                        content.Dispose();
                        _currentEditingConfig = null;
                        // 关闭对话框
                        if (dialog.Parent is Window parentWindow)
                        {
                            parentWindow.DialogResult = false;
                            parentWindow.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"关闭对话框时出错: {ex.Message}");
                    }
                };
                
                // 创建一个窗口来包装对话框
                var dialogWindow = new Window
                {
                    Title = isTestMode ? "测试配置" : (config == null ? "新建配置" : "编辑配置"),
                    Width = 1000,
                    Height = 750,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.CanResize,
                    MinWidth = 800,
                    MinHeight = 600,
                    Content = dialog
                };
                
                // 设置窗口的Owner（如果有的话）
                if (this.Parent is Window parentWindow)
                {
                    dialogWindow.Owner = parentWindow;
                }
                else if (Application.Current?.MainWindow != null)
                {
                    dialogWindow.Owner = Application.Current.MainWindow;
                }
                
                Console.WriteLine("设置对话框属性成功");
                
                // 显示对话框
                var result = dialogWindow.ShowDialog();
                
                if (result == true)
                {
                    // 用户点击了保存
                    Console.WriteLine("对话框返回true，保存操作已完成");
                }
                else
                {
                    // 用户点击了取消或关闭
                    Console.WriteLine("对话框返回false，取消操作");
                    try
                    {
                        content.Dispose();
                            _currentEditingConfig = null;
                        }
                        catch (Exception ex)
                        {
                        Console.WriteLine($"清理资源时出错: {ex.Message}");
                    }
                }
                
                Console.WriteLine("对话框显示完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"显示配置对话框时发生异常: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                Extensions.MessageBoxExtensions.Show($"显示配置对话框时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveConfig(ExcelImportConfigContent content)
        {
            try
            {
                Console.WriteLine("=== 开始保存配置 ===");
                
                // 验证必填字段
                if (string.IsNullOrWhiteSpace(content.ConfigName))
                {
                    Console.WriteLine("配置名称为空");
                    Extensions.MessageBoxExtensions.Show("请输入配置名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(content.FilePath))
                {
                    Console.WriteLine("文件路径为空");
                    Extensions.MessageBoxExtensions.Show("请选择Excel文件", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // 验证标题行号
                if (!int.TryParse(content.HeaderRow, out int headerRowNumber) || headerRowNumber <= 0)
                {
                    Console.WriteLine($"标题行号无效: {content.HeaderRow}");
                    Extensions.MessageBoxExtensions.Show("请输入有效的标题行号（大于0的整数）", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                bool isEdit = _currentEditingConfig != null;
                
                // 创建配置对象（编辑时保留原始Id/名称用于更新）
                var config = new ExcelConfig
                {
                    ConfigName = isEdit ? _currentEditingConfig.ConfigName : content.ConfigName.Trim(),
                    FilePath = content.FilePath.Trim(),
                    TargetDataSourceName = content.TargetDataSource?.Trim() ?? "默认数据源",
                    TargetDataSourceId = GetDataSourceId(content.TargetDataSourceId), // 使用同步方法
                    TargetTableName = content.TargetTableName?.Trim() ?? "",
                    SheetName = content.SheetName?.Trim() ?? "Sheet1",
                    HeaderRow = int.Parse(content.HeaderRow.Trim()),
                    SkipEmptyRows = content.SkipEmptyRows,
                    SplitEachRow = content.SplitEachRow,
                    ClearTableDataBeforeImport = content.ClearTableDataBeforeImport,
                    Status = "正常",
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Id = isEdit ? _currentEditingConfig.Id : string.Empty
                };
                
                // 添加调试信息
                Console.WriteLine($"保存配置: {config.ConfigName}");
                Console.WriteLine($"文件路径: {config.FilePath}");
                Console.WriteLine($"目标数据源名称: {config.TargetDataSourceName}");
                Console.WriteLine($"目标数据源ID: {config.TargetDataSourceId}");
                Console.WriteLine($"目标表名: {config.TargetTableName}");
                Console.WriteLine($"工作表名: {config.SheetName}");
                Console.WriteLine($"标题行: {config.HeaderRow}");
                Console.WriteLine($"状态: {config.Status}");
                Console.WriteLine(isEdit ? "当前为编辑模式，将执行更新操作" : "当前为新增模式，将执行插入操作");
                
                // 检查服务是否可用
                if (_excelConfigService == null)
                {
                    Console.WriteLine("错误: _excelConfigService 为 null");
                    Extensions.MessageBoxExtensions.Show("配置服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Console.WriteLine("调用保存服务...");
                
                // 新增或更新到数据库
                var success = isEdit
                    ? await _excelConfigService.UpdateConfigAsync(config)
                    : await _excelConfigService.SaveConfigAsync(config);
                
                Console.WriteLine($"保存结果: {success}");
                
                if (success)
                {
                    // 保存字段映射
                    if (_excelService != null && content.FieldMappings != null)
                    {
                        string configId = string.Empty;
                        if (isEdit && _currentEditingConfig != null && !string.IsNullOrEmpty(_currentEditingConfig.Id))
                        {
                            configId = _currentEditingConfig.Id;
                        }
                        else
                        {
                            var savedConfig = await _excelConfigService.GetConfigByNameAsync(config.ConfigName);
                            if (savedConfig != null) configId = savedConfig.Id;
                        }
                        
                        if (!string.IsNullOrEmpty(configId))
                        {
                            var mappings = content.FieldMappings.Select((m, idx) => new ExcelFieldMapping
                            {
                                ExcelConfigId = configId,
                                ExcelColumnName = m.ExcelColumn,
                                ExcelColumnIndex = idx,
                                TargetFieldName = m.DatabaseField,
                                TargetFieldType = m.DataType,
                                IsRequired = m.IsRequired,
                                SortOrder = idx,
                                CreatedAt = DateTime.Now
                            });
                            await _excelService.SaveFieldMappingsAsync(configId, mappings);
                        }
                    }

                    Console.WriteLine("保存成功，显示成功消息");
                    Extensions.MessageBoxExtensions.Show($"配置 '{config.ConfigName}' 保存成功！", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    Console.WriteLine("刷新配置列表...");
                    // 刷新配置列表
                    LoadExcelConfigs();
                    
                    Console.WriteLine("=== 保存配置完成 ===");
                }
                else
                {
                    Console.WriteLine("保存失败，显示错误消息");
                    Extensions.MessageBoxExtensions.Show("保存配置失败，请重试", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存配置异常: {ex.Message}");
                Console.WriteLine($"异常堆栈: {ex.StackTrace}");
                Extensions.MessageBoxExtensions.Show($"保存配置时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TestConfig(ExcelImportConfigContent content)
        {
            try
            {
                Console.WriteLine("=== 开始测试配置 ===");
                
                // 验证基本配置
                if (string.IsNullOrWhiteSpace(content.ConfigName))
                {
                    Extensions.MessageBoxExtensions.Show("请输入配置名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(content.FilePath))
                {
                    Extensions.MessageBoxExtensions.Show("请选择Excel文件", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!File.Exists(content.FilePath))
                {
                    Extensions.MessageBoxExtensions.Show("选择的Excel文件不存在", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 验证标题行号
                if (!int.TryParse(content.HeaderRow, out int headerRowNumber) || headerRowNumber <= 0)
                {
                    Extensions.MessageBoxExtensions.Show("请输入有效的标题行号（大于0的整数）", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 验证字段映射
                var fieldMappings = content.FieldMappings;
                if (fieldMappings == null || fieldMappings.Count == 0)
                {
                    Extensions.MessageBoxExtensions.Show("请至少添加一个字段映射", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 执行简单的配置验证
                var errors = new List<string>();
                var warnings = new List<string>();
                
                // 验证字段映射的完整性
                foreach (var mapping in fieldMappings)
                {
                    if (string.IsNullOrWhiteSpace(mapping.ExcelColumn))
                    {
                        errors.Add("Excel列名不能为空");
                    }

                    if (string.IsNullOrWhiteSpace(mapping.DatabaseField))
                    {
                        errors.Add("数据库字段名不能为空");
                    }

                    if (string.IsNullOrWhiteSpace(mapping.DataType))
                    {
                        errors.Add("数据类型不能为空");
                    }
                }
                
                if (errors.Count > 0)
                {
                    var errorMessage = $"配置测试失败：{content.ConfigName}\n\n错误：\n" + string.Join("\n", errors);
                    Extensions.MessageBoxExtensions.Show(errorMessage, "测试失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    Extensions.MessageBoxExtensions.Show($"配置测试完成：{content.ConfigName}\n\n配置验证通过，可以正常使用。", "测试成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                Console.WriteLine("=== 测试配置完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试配置异常: {ex.Message}");
                Extensions.MessageBoxExtensions.Show($"测试配置时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TestConfigDirectly(ExcelConfig config)
        {
            try
            {
                // 验证配置
                if (string.IsNullOrWhiteSpace(config.ConfigName))
                {
                    Extensions.MessageBoxExtensions.Show("配置名称不能为空", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(config.FilePath))
                {
                    Extensions.MessageBoxExtensions.Show("文件路径不能为空", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                if (!System.IO.File.Exists(config.FilePath))
                {
                    Extensions.MessageBoxExtensions.Show($"Excel文件不存在：{config.FilePath}", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // 测试Excel文件读取
                using (var package = new OfficeOpenXml.ExcelPackage(new System.IO.FileInfo(config.FilePath)))
                {
                    var worksheet = package.Workbook.Worksheets[config.SheetName];
                    if (worksheet == null)
                    {
                        Extensions.MessageBoxExtensions.Show($"工作表 '{config.SheetName}' 不存在", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    var dimension = worksheet.Dimension;
                    if (dimension == null)
                    {
                        Extensions.MessageBoxExtensions.Show("Excel文件为空或无法读取", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    // 检查标题行是否存在
                    if (config.HeaderRow > dimension.End.Row)
                    {
                        Extensions.MessageBoxExtensions.Show($"标题行 {config.HeaderRow} 超出文件范围（最大行数：{dimension.End.Row}）", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    // 读取标题行数据
                    var headers = new List<string>();
                    for (int col = dimension.Start.Column; col <= dimension.End.Column; col++)
                    {
                        var cellValue = worksheet.Cells[config.HeaderRow, col].Value;
                        headers.Add(cellValue?.ToString() ?? $"列{col}");
                    }
                    
                    // 读取前几行数据作为预览
                    var previewData = new List<Dictionary<string, object>>();
                    int previewRowCount = Math.Min(5, dimension.End.Row - config.HeaderRow);
                    
                    for (int row = config.HeaderRow + 1; row <= config.HeaderRow + previewRowCount; row++)
                    {
                        var rowData = new Dictionary<string, object>();
                        for (int col = dimension.Start.Column; col <= dimension.End.Column; col++)
                        {
                            var cellValue = worksheet.Cells[row, col].Value;
                            rowData[headers[col - dimension.Start.Column]] = cellValue;
                        }
                        previewData.Add(rowData);
                    }
                    
                    // 显示测试结果
                    var resultMessage = $"配置测试通过！\n\n" +
                        $"配置名称: {config.ConfigName}\n" +
                        $"文件路径: {config.FilePath}\n" +
                        $"工作表: {config.SheetName}\n" +
                        $"标题行: {config.HeaderRow}\n" +
                        $"数据列数: {headers.Count}\n" +
                        $"数据行数: {dimension.End.Row - config.HeaderRow}\n\n" +
                        $"前{previewRowCount}行数据预览已生成。";
                    
                    Extensions.MessageBoxExtensions.Show(resultMessage, "测试成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // 显示数据预览对话框
                    var dialog = new Controls.DataPreviewDialog(previewData, config.HeaderRow);
                    dialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"测试配置时出错：{ex.Message}", "测试失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NewConfigButton_Click(object sender, RoutedEventArgs e)
        {
            ShowConfigDialog(null, false);
        }

        /// <summary>
        /// 批量删除按钮点击事件
        /// </summary>
        private void BatchDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            BatchDeleteSelectedConfigs();
        }

        /// <summary>
        /// 全选复选框点击事件
        /// </summary>
        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox selectAllCheckBox && ExcelConfigDataGrid.ItemsSource is IEnumerable<ExcelConfig> configs)
            {
                bool isChecked = selectAllCheckBox.IsChecked ?? false;
                
                // 更新所有项的选中状态
                foreach (var config in configs)
                {
                    config.IsSelected = isChecked;
                }
                
                // 刷新DataGrid显示
                ExcelConfigDataGrid.Items.Refresh();
            }
        }

        /// <summary>
        /// 获取当前选中的配置项
        /// </summary>
        public List<ExcelConfig> GetSelectedConfigs()
        {
            if (ExcelConfigDataGrid.ItemsSource is IEnumerable<ExcelConfig> configs)
            {
                return configs.Where(c => c.IsSelected).ToList();
            }
            return new List<ExcelConfig>();
        }

        /// <summary>
        /// 批量删除选中的配置
        /// </summary>
        private async void BatchDeleteSelectedConfigs()
        {
            var selectedConfigs = GetSelectedConfigs();
            if (selectedConfigs.Count == 0)
            {
                Extensions.MessageBoxExtensions.Show("请先选择要删除的配置项", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = Extensions.MessageBoxExtensions.Show($"确定要删除选中的 {selectedConfigs.Count} 个配置项吗？", 
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (var config in selectedConfigs)
                    {
                        await _excelConfigService.DeleteConfigAsync(config.ConfigName);
                    }
                    
                    Extensions.MessageBoxExtensions.Show($"成功删除 {selectedConfigs.Count} 个配置项", "删除成功", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // 重新加载配置列表
                    LoadExcelConfigs();
                }
                catch (Exception ex)
                {
                    Extensions.MessageBoxExtensions.Show($"删除配置时出错：{ex.Message}", "删除失败", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 获取数据源ID（现在直接返回string类型）
        /// </summary>
        private string GetDataSourceId(string dataSourceId)
        {
            try
            {
                // 如果已经有值，直接返回
                if (!string.IsNullOrWhiteSpace(dataSourceId))
                {
                    return dataSourceId;
                }

                // 如果没有值，返回默认数据源ID
                return "default";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取数据源ID失败: {ex.Message}");
                return "default"; // 返回默认数据源ID
            }
        }
    }
    
    public class FieldMapping
    {
        public string ExcelOriginalColumn { get; set; }
        public string ExcelColumn { get; set; }
        public string DatabaseField { get; set; }
        public string DataType { get; set; }
        public bool IsRequired { get; set; }
    }
} 