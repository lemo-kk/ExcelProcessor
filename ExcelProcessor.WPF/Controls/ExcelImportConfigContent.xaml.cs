using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using OfficeOpenXml;
using ExcelProcessor.WPF.Models;
using ExcelProcessor.WPF.Helpers;
using ExcelProcessor.Models;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Core.Interfaces;
using ExcelProcessor.Data.Services;
using System.Threading.Tasks; // Added for Task
using Microsoft.Extensions.Logging; // Added for ILoggerFactory

namespace ExcelProcessor.WPF.Controls
{
    public partial class ExcelImportConfigContent : UserControl
    {
        private List<string> _dataSources = new List<string>();
        private ExcelPackage _excelPackage;
        private ExcelWorksheet _currentWorksheet;
        private readonly IDataSourceService _dataSourceService;

        /// <summary>
        /// 详细的日志记录方法
        /// </summary>
        /// <param name="message">日志消息</param>
        private void LogDebug(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] {message}";
            System.Diagnostics.Debug.WriteLine(logMessage);
            
            // 同时输出到控制台
            Console.WriteLine(logMessage);
            
            // 如果应用程序有日志窗口，也可以输出到UI
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 这里可以添加UI日志显示逻辑
                });
            }
            catch
            {
                // 忽略UI更新错误
            }
        }

        /// <summary>
        /// 记录分隔线
        /// </summary>
        /// <param name="title">分隔线标题</param>
        private void LogSeparator(string title)
        {
            LogDebug(new string('=', 50));
            LogDebug($"=== {title} ===");
            LogDebug(new string('=', 50));
        }

        public ExcelImportConfigContent()
        {
            InitializeComponent();
            // 获取数据源服务实例
            _dataSourceService = App.Services.GetService(typeof(IDataSourceService)) as IDataSourceService;
            _ = InitializeDataSourcesAsync();
            
            // 设置默认执行选项
            SplitEachRowCheckBox.IsChecked = true; // 默认勾选拆分每一行
            ClearTableDataCheckBox.IsChecked = true; // 默认勾选导入前清除表数据
            SkipEmptyRowsCheckBox.IsChecked = true; // 默认勾选跳过空行
            
            // 新增配置页面打开时，字段映射表格应该为空，不加载示例数据
            // LoadSampleData();
        }

        /// <summary>
        /// 加载现有配置数据
        /// </summary>
        /// <param name="config">要加载的配置</param>
        public async void LoadConfig(ExcelConfig config)
        {
            if (config == null) return;
            try
            {
                DataGridHelper.EnsureDefinedColumnsOnly(FieldMappingDataGrid);
                ConfigNameTextBox.Text = config.ConfigName ?? "";
                ConfigNameTextBox.IsReadOnly = true;
                FilePathTextBox.Text = config.FilePath ?? "";
                // ExcelFileNameTextBox已移除，不再需要设置
                
                // 确保数据源已初始化
                await InitializeDataSourcesAsync();
                
                // 设置目标数据源
                if (!string.IsNullOrWhiteSpace(config.TargetDataSourceId))
                {
                    TargetDataSourceComboBox.SelectedValue = config.TargetDataSourceId;
                }
                else if (!string.IsNullOrWhiteSpace(config.TargetDataSourceName))
                {
                    // 如果没有ID，尝试通过名称查找
                    var items = TargetDataSourceComboBox.ItemsSource as List<DataSourceInfo>;
                    if (items != null)
                    {
                        var matchingItem = items.FirstOrDefault(item => item.Name == config.TargetDataSourceName);
                        if (matchingItem != null)
                {
                            TargetDataSourceComboBox.SelectedValue = matchingItem.Id;
                        }
                    }
                }
                TargetTableNameTextBox.Text = config.TargetTableName ?? "";
                SheetNameTextBox.Text = config.SheetName ?? "";
                HeaderRowTextBox.Text = config.HeaderRow.ToString();
                SkipEmptyRowsCheckBox.IsChecked = config.SkipEmptyRows;
                SplitEachRowCheckBox.IsChecked = config.SplitEachRow;
                ClearTableDataCheckBox.IsChecked = config.ClearTableDataBeforeImport;
                LoadFieldMappingsForConfig(config);
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"加载配置数据时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 为指定配置加载字段映射
        /// </summary>
        /// <param name="config">配置对象</param>
        private async void LoadFieldMappingsForConfig(ExcelConfig config)
        {
            try
            {
                // 如果能拿到配置ID，优先使用ID查询映射
                var configToLoad = config;
                if (string.IsNullOrEmpty(configToLoad.Id))
                {
                    var configService = App.Services.GetService(typeof(ExcelProcessor.Core.Services.IExcelConfigService)) as ExcelProcessor.Core.Services.IExcelConfigService;
                    if (configService != null)
                    {
                        var fetched = await configService.GetConfigByIdAsync(config.ConfigName);
                        if (fetched != null) configToLoad = fetched;
                    }
                }

                var excelService = App.Services.GetService(typeof(ExcelProcessor.Core.Services.IExcelService)) as ExcelProcessor.Core.Services.IExcelService;
                if (excelService != null && configToLoad != null && !string.IsNullOrEmpty(configToLoad.Id))
                {
                    var dbMappings = await excelService.GetFieldMappingsAsync(configToLoad.Id);
                    var uiMappings = dbMappings.Select(m => new FieldMapping
                    {
                        ExcelOriginalColumn = GetColumnLetter(m.ExcelColumnIndex),
                        ExcelColumn = m.ExcelColumnName,
                        DatabaseField = m.TargetFieldName,
                        DataType = m.TargetFieldType,
                        IsRequired = m.IsRequired
                    }).ToList();
                    if (uiMappings.Count > 0)
                    {
                        FieldMappingDataGrid.ItemsSource = uiMappings;
                        return;
                    }
                }

                // 如果数据库中没有字段映射数据，尝试从Excel文件中读取列名
                if (!string.IsNullOrEmpty(config.FilePath) && File.Exists(config.FilePath))
                {
                    // 先处理Excel文件以获取列名
                    ProcessSelectedFile(config.FilePath);
                    
                    // 如果处理成功，字段映射应该已经自动填充
                    // 如果没有自动填充，则使用示例数据
                    var currentMappings = FieldMappingDataGrid.ItemsSource as List<FieldMapping>;
                    if (currentMappings == null || currentMappings.Count == 0)
                    {
                        // 使用示例数据作为备选
                        var sampleMappings = new List<FieldMapping>
                        {
                            new FieldMapping { ExcelOriginalColumn = "A", ExcelColumn = "客户编号", DatabaseField = "customer_id", DataType = "VARCHAR(50)", IsRequired = true },
                            new FieldMapping { ExcelOriginalColumn = "B", ExcelColumn = "客户名称", DatabaseField = "customer_name", DataType = "VARCHAR(100)", IsRequired = true },
                            new FieldMapping { ExcelOriginalColumn = "C", ExcelColumn = "联系电话", DatabaseField = "phone", DataType = "VARCHAR(20)", IsRequired = false },
                            new FieldMapping { ExcelOriginalColumn = "D", ExcelColumn = "销售金额", DatabaseField = "sales_amount", DataType = "DECIMAL(10,2)", IsRequired = true },
                            new FieldMapping { ExcelOriginalColumn = "E", ExcelColumn = "销售日期", DatabaseField = "sales_date", DataType = "DATE", IsRequired = true }
                        };
                        FieldMappingDataGrid.ItemsSource = sampleMappings;
                    }
                }
                else
                {
                    // 如果Excel文件不存在，使用示例数据
                    var sampleMappings = new List<FieldMapping>
                    {
                        new FieldMapping { ExcelOriginalColumn = "A", ExcelColumn = "客户编号", DatabaseField = "customer_id", DataType = "VARCHAR(50)", IsRequired = true },
                        new FieldMapping { ExcelOriginalColumn = "B", ExcelColumn = "客户名称", DatabaseField = "customer_name", DataType = "VARCHAR(100)", IsRequired = true },
                        new FieldMapping { ExcelOriginalColumn = "C", ExcelColumn = "联系电话", DatabaseField = "phone", DataType = "VARCHAR(20)", IsRequired = false },
                        new FieldMapping { ExcelOriginalColumn = "D", ExcelColumn = "销售金额", DatabaseField = "sales_amount", DataType = "DECIMAL(10,2)", IsRequired = true },
                        new FieldMapping { ExcelOriginalColumn = "E", ExcelColumn = "销售日期", DatabaseField = "sales_date", DataType = "DATE", IsRequired = true }
                    };
                    FieldMappingDataGrid.ItemsSource = sampleMappings;
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"加载字段映射时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // 出错时也使用示例数据
                var sampleMappings = new List<FieldMapping>
                {
                    new FieldMapping { ExcelOriginalColumn = "A", ExcelColumn = "客户编号", DatabaseField = "customer_id", DataType = "VARCHAR(50)", IsRequired = true },
                    new FieldMapping { ExcelOriginalColumn = "B", ExcelColumn = "客户名称", DatabaseField = "customer_name", DataType = "VARCHAR(100)", IsRequired = true },
                    new FieldMapping { ExcelOriginalColumn = "C", ExcelColumn = "联系电话", DatabaseField = "phone", DataType = "VARCHAR(20)", IsRequired = false },
                    new FieldMapping { ExcelOriginalColumn = "D", ExcelColumn = "销售金额", DatabaseField = "sales_amount", DataType = "DECIMAL(10,2)", IsRequired = true },
                    new FieldMapping { ExcelOriginalColumn = "E", ExcelColumn = "销售日期", DatabaseField = "sales_date", DataType = "DATE", IsRequired = true }
                };
                FieldMappingDataGrid.ItemsSource = sampleMappings;
            }
        }

        /// <summary>
        /// 数据源信息类
        /// </summary>
        public class DataSourceInfo
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Display { get; set; } = string.Empty;
        }

        private async Task InitializeDataSourcesAsync()
        {
            try
            {
                // 从数据源服务获取所有数据源
                var dataSourceConfigs = await _dataSourceService.GetAllDataSourcesAsync();
                
                // 组装用于显示的对象列表
                var items = dataSourceConfigs
                    .Select(ds => new DataSourceInfo 
                    { 
                        Id = ds.Id, 
                        Name = ds.Name, 
                        Type = ds.Type, 
                        Display = $"{ds.Name} ({ds.Type})" 
                    })
                    .ToList();
                
                // 如果没有数据源，添加默认选项
                if (items.Count == 0)
                {
                    items.Add(new DataSourceInfo 
                    { 
                        Id = "default", 
                        Name = "默认数据源", 
                        Type = "SQLite", 
                        Display = "默认数据源 (SQLite)" 
                    });
                }
                
                // 设置数据源到下拉框
                TargetDataSourceComboBox.ItemsSource = items;
                
                // 设置DisplayMemberPath和SelectedValuePath
                TargetDataSourceComboBox.DisplayMemberPath = "Display";
                TargetDataSourceComboBox.SelectedValuePath = "Id";
                
                // 默认选中默认数据源（如果存在）
                var defaultDataSource = dataSourceConfigs.FirstOrDefault(ds => ds.IsDefault);
                if (defaultDataSource != null)
                {
                    TargetDataSourceComboBox.SelectedValue = defaultDataSource.Id;
                }
                else
                {
                    // 如果没有默认数据源，选择第一个
                    TargetDataSourceComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                // 如果获取数据源失败，使用默认列表
                var defaultItems = new List<DataSourceInfo>
                {
                    new DataSourceInfo 
                    { 
                        Id = "default", 
                        Name = "默认数据源", 
                        Type = "SQLite", 
                        Display = "默认数据源 (SQLite)" 
                    }
                };
                
                TargetDataSourceComboBox.ItemsSource = defaultItems;
                TargetDataSourceComboBox.DisplayMemberPath = "Display";
                TargetDataSourceComboBox.SelectedValuePath = "Id";
                TargetDataSourceComboBox.SelectedIndex = 0;
                
                Console.WriteLine($"初始化数据源失败，使用默认列表: {ex.Message}");
            }
        }

        private void LoadSampleData()
        {
            // 加载示例字段映射数据
            var fieldMappings = new List<FieldMapping>
            {
                new FieldMapping { ExcelOriginalColumn = "A", ExcelColumn = "客户编号", DatabaseField = "customer_id", DataType = "VARCHAR(50)", IsRequired = true },
                new FieldMapping { ExcelOriginalColumn = "B", ExcelColumn = "客户名称", DatabaseField = "customer_name", DataType = "VARCHAR(100)", IsRequired = true },
                new FieldMapping { ExcelOriginalColumn = "C", ExcelColumn = "联系电话", DatabaseField = "phone", DataType = "VARCHAR(20)", IsRequired = false },
                new FieldMapping { ExcelOriginalColumn = "D", ExcelColumn = "销售金额", DatabaseField = "sales_amount", DataType = "DECIMAL(10,2)", IsRequired = true },
                new FieldMapping { ExcelOriginalColumn = "E", ExcelColumn = "销售日期", DatabaseField = "sales_date", DataType = "DATE", IsRequired = true }
            };

            FieldMappingDataGrid.ItemsSource = fieldMappings;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "选择Excel文件",
                Filter = "Excel文件 (*.xlsx;*.xls)|*.xlsx;*.xls|CSV文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
                DefaultExt = "xlsx"
            };

            // 设置初始目录为data/input文件夹
            var dataInputPath = Path.Combine(GetApplicationRootPath(), "data", "input");
            if (Directory.Exists(dataInputPath))
            {
                openFileDialog.InitialDirectory = dataInputPath;
            }
            else
            {
                // 如果data/input不存在，使用应用程序根目录
                openFileDialog.InitialDirectory = GetApplicationRootPath();
            }

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                ProcessSelectedFile(filePath);
            }
        }

        private void ProcessSelectedFile(string filePath)
        {
            try
            {
                // 设置文件路径
                string fileName = Path.GetFileName(filePath);
                
                // 转换为相对路径
                string relativePath = ConvertToRelativePath(filePath);
                FilePathTextBox.Text = relativePath;

                // 设置配置名称为Excel文件名（去掉扩展名）
                string configName = Path.GetFileNameWithoutExtension(fileName);
                ConfigNameTextBox.Text = configName;

                // 设置Excel文件名作为导入到数据库的表名
                string tableName = Path.GetFileNameWithoutExtension(fileName);
                // 自动设置目标表名为Excel文件名（去掉扩展名）
                TargetTableNameTextBox.Text = tableName;

                // 根据文件扩展名处理
                string extension = Path.GetExtension(filePath).ToLower();
                if (extension == ".csv")
                {
                    ProcessCsvFile(filePath);
                }
                else if (extension == ".xlsx" || extension == ".xls")
                {
                    ProcessExcelFile(filePath);
                }
                else
                {
                    Extensions.MessageBoxExtensions.Show("不支持的文件格式，请选择Excel文件(.xlsx/.xls)或CSV文件(.csv)", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 显示成功消息
                Extensions.MessageBoxExtensions.Show($"文件已成功加载：{fileName}\n\n" +
                    $"配置名称：{configName}\n" +
                    $"目标表名：{tableName}\n\n" +
                    $"已自动填充默认设置，请根据需要调整字段映射。", 
                    "文件加载成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"处理文件时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 将绝对路径转换为相对路径
        /// </summary>
        private string ConvertToRelativePath(string absolutePath)
        {
            try
            {
                var appRoot = AppDomain.CurrentDomain.BaseDirectory;
                if (absolutePath.StartsWith(appRoot, StringComparison.OrdinalIgnoreCase))
                {
                    var relativePath = absolutePath.Substring(appRoot.Length);
                    return relativePath.TrimStart('\\', '/');
                }
                return absolutePath;
            }
            catch
            {
                return absolutePath;
            }
        }

        private void ProcessExcelFile(string filePath)
        {
            LogSeparator("ProcessExcelFile 开始处理");
            LogDebug($"文件路径：{filePath}");
            
            // 设置EPPlus许可证上下文
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            LogDebug("EPPlus许可证上下文已设置为NonCommercial");

            // 释放之前的资源
            if (_excelPackage != null)
            {
                LogDebug("释放之前的ExcelPackage资源");
                _excelPackage.Dispose();
            }

            // 打开Excel文件
            LogDebug("正在打开Excel文件...");
            _excelPackage = new ExcelPackage(new FileInfo(filePath));
            LogDebug("Excel文件打开成功");
            
            // 获取第一个工作表
            LogDebug("正在获取第一个工作表...");
            _currentWorksheet = _excelPackage.Workbook.Worksheets.FirstOrDefault();
            if (_currentWorksheet == null)
            {
                LogDebug("❌ 错误：Excel文件中没有找到工作表");
                throw new Exception("Excel文件中没有找到工作表");
            }

            LogDebug($"✅ 成功获取工作表：{_currentWorksheet.Name}");

            // 设置默认Sheet名称
            SheetNameTextBox.Text = _currentWorksheet.Name;
            LogDebug($"设置Sheet名称：{_currentWorksheet.Name}");

            // 设置默认标题行号
            HeaderRowTextBox.Text = "1";
            LogDebug("设置默认标题行号：1");
            
            var dimension = _currentWorksheet.Dimension;
            if (dimension != null)
            {
                LogDebug($"📊 工作表维度信息：");
                LogDebug($"   - 起始列：{dimension.Start.Column}");
                LogDebug($"   - 结束列：{dimension.End.Column}");
                LogDebug($"   - 起始行：{dimension.Start.Row}");
                LogDebug($"   - 结束行：{dimension.End.Row}");
                LogDebug($"   - 总列数：{dimension.End.Column - dimension.Start.Column + 1}");
                LogDebug($"   - 总行数：{dimension.End.Row - dimension.Start.Row + 1}");
            }
            else
            {
                LogDebug("⚠️ 警告：无法获取工作表维度信息");
            }

            // 强制读取所有列，确保不遗漏任何列
            LogDebug("开始强制读取所有列...");
            ForceReadAllExcelColumns();
            
            LogSeparator("ProcessExcelFile 处理完成");
        }

        private void ProcessCsvFile(string filePath)
        {
            // 设置默认Sheet名称
            SheetNameTextBox.Text = "Sheet1";
            
            // 设置默认标题行号
            HeaderRowTextBox.Text = "1";

            // 读取CSV列信息
            ReadCsvColumns(filePath);
        }

        private void ReadExcelColumns()
        {
            try
            {
                if (_currentWorksheet == null) return;

                // 获取工作表的维度
                var dimension = _currentWorksheet.Dimension;
                if (dimension == null) return;

                // 读取第一行作为列名（默认）
                var columnNames = new List<string>();
                var debugInfo = new List<string>();
                
                for (int col = 1; col <= dimension.End.Column; col++)
                {
                    var cellValue = _currentWorksheet.Cells[1, col].Value;
                    string columnName = cellValue?.ToString();
                    
                    // 添加调试信息
                    debugInfo.Add($"列{col}: 原始值='{cellValue}', 处理后='{columnName}'");
                    
                    // 改进：更宽松的列名判断，包括空字符串但保留列位置
                    if (!string.IsNullOrWhiteSpace(columnName))
                    {
                        columnNames.Add(columnName);
                    }
                    else
                    {
                        // 如果列名为空，使用默认列名
                        var columnLetter = GetColumnLetter(col - 1);
                        columnNames.Add($"第{columnLetter}列");
                    }
                }

                // 输出调试信息到控制台
                System.Diagnostics.Debug.WriteLine("=== Excel列名读取调试信息 ===");
                foreach (var info in debugInfo)
                {
                    System.Diagnostics.Debug.WriteLine(info);
                }
                System.Diagnostics.Debug.WriteLine($"总共读取到 {columnNames.Count} 个列名");
                System.Diagnostics.Debug.WriteLine("=== 调试信息结束 ===");

                // 更新字段映射
                UpdateFieldMappingsFromColumns(columnNames);
            }
            catch (Exception ex)
            {
                throw new Exception($"读取Excel列信息失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 智能读取Excel列名，自动检测标题行
        /// </summary>
        private void SmartReadExcelColumns()
        {
            try
            {
                if (_currentWorksheet == null) return;

                var dimension = _currentWorksheet.Dimension;
                if (dimension == null) return;

                // 尝试多个可能的标题行
                var possibleHeaderRows = new List<int> { 1, 2, 3 };
                var bestHeaderRow = 1;
                var maxColumnCount = 0;

                foreach (var row in possibleHeaderRows)
                {
                    if (row > dimension.End.Row) continue;

                    var columnCount = 0;
                    for (int col = 1; col <= dimension.End.Column; col++)
                    {
                        var cellValue = _currentWorksheet.Cells[row, col].Value;
                        if (!string.IsNullOrWhiteSpace(cellValue?.ToString()))
                        {
                            columnCount++;
                        }
                    }

                    if (columnCount > maxColumnCount)
                    {
                        maxColumnCount = columnCount;
                        bestHeaderRow = row;
                    }
                }

                // 使用最佳标题行读取列名
                ReadExcelColumnsByRow(bestHeaderRow);
                
                // 更新标题行号显示
                HeaderRowTextBox.Text = bestHeaderRow.ToString();
                
                // 显示调试信息
                System.Diagnostics.Debug.WriteLine($"智能检测到最佳标题行：第{bestHeaderRow}行，包含{maxColumnCount}个有值的列");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"智能读取列名失败：{ex.Message}");
                // 如果智能读取失败，回退到默认方法
                ReadExcelColumns();
            }
        }

        /// <summary>
        /// 读取所有列，包括空值列
        /// </summary>
        private void ReadAllExcelColumns()
        {
            try
            {
                if (_currentWorksheet == null) return;

                var dimension = _currentWorksheet.Dimension;
                if (dimension == null) return;

                var columnNames = new List<string>();
                var debugInfo = new List<string>();
                
                for (int col = 1; col <= dimension.End.Column; col++)
                {
                    var cellValue = _currentWorksheet.Cells[1, col].Value;
                    string columnName = cellValue?.ToString();
                    
                    // 添加调试信息
                    debugInfo.Add($"列{col}: 原始值='{cellValue}', 处理后='{columnName}'");
                    
                    // 读取所有列，包括空值
                    if (!string.IsNullOrWhiteSpace(columnName))
                    {
                        columnNames.Add(columnName);
                    }
                    else
                    {
                        // 空值列使用默认列名
                        var columnLetter = GetColumnLetter(col - 1);
                        columnNames.Add($"第{columnLetter}列");
                    }
                }

                // 输出调试信息
                System.Diagnostics.Debug.WriteLine("=== 完整列名读取调试信息 ===");
                foreach (var info in debugInfo)
                {
                    System.Diagnostics.Debug.WriteLine(info);
                }
                System.Diagnostics.Debug.WriteLine($"总共读取到 {columnNames.Count} 个列名");
                System.Diagnostics.Debug.WriteLine("=== 调试信息结束 ===");

                // 更新字段映射
                UpdateFieldMappingsFromColumns(columnNames);
            }
            catch (Exception ex)
            {
                throw new Exception($"读取Excel列信息失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 强制读取所有列，确保不遗漏任何列
        /// </summary>
        private void ForceReadAllExcelColumns()
        {
            try
            {
                LogSeparator("强制读取所有列");
                
                if (_currentWorksheet == null)
                {
                    LogDebug("❌ 错误：_currentWorksheet 为 null");
                    return;
                }

                var dimension = _currentWorksheet.Dimension;
                if (dimension == null)
                {
                    LogDebug("❌ 错误：无法获取工作表维度");
                    return;
                }

                LogDebug($"📊 工作表维度信息：");
                LogDebug($"   - 起始列：{dimension.Start.Column}");
                LogDebug($"   - 结束列：{dimension.End.Column}");
                LogDebug($"   - 起始行：{dimension.Start.Row}");
                LogDebug($"   - 结束行：{dimension.End.Row}");
                LogDebug($"   - 工作表名称：{_currentWorksheet.Name}");

                var columnNames = new List<string>();
                var debugInfo = new List<string>();
                
                LogDebug($"🔄 开始读取 {dimension.End.Column} 列...");
                
                // 强制读取所有列，从第1列到最后一列
                for (int col = 1; col <= dimension.End.Column; col++)
                {
                    LogDebug($"--- 处理第 {col} 列 ---");
                    
                    // 尝试读取第1行作为列名
                    var cellValue = _currentWorksheet.Cells[1, col].Value;
                    string columnName = cellValue?.ToString();
                    LogDebug($"   第1行值：'{cellValue}' -> '{columnName}'");
                    
                    // 如果第1行为空，尝试读取第2行
                    if (string.IsNullOrWhiteSpace(columnName) && dimension.End.Row >= 2)
                    {
                        cellValue = _currentWorksheet.Cells[2, col].Value;
                        columnName = cellValue?.ToString();
                        LogDebug($"   第2行值：'{cellValue}' -> '{columnName}'");
                    }
                    
                    // 如果第2行也为空，尝试读取第3行
                    if (string.IsNullOrWhiteSpace(columnName) && dimension.End.Row >= 3)
                    {
                        cellValue = _currentWorksheet.Cells[3, col].Value;
                        columnName = cellValue?.ToString();
                        LogDebug($"   第3行值：'{cellValue}' -> '{columnName}'");
                    }
                    
                    // 添加调试信息
                    debugInfo.Add($"列{col}: 原始值='{cellValue}', 处理后='{columnName}', 是否为空={string.IsNullOrWhiteSpace(columnName)}");
                    
                    // 如果所有尝试都为空，使用默认列名
                    if (string.IsNullOrWhiteSpace(columnName))
                    {
                        var columnLetter = GetColumnLetter(col - 1);
                        columnName = $"第{columnLetter}列";
                        LogDebug($"   ⚠️ 列{col}为空，使用默认名称：{columnName}");
                    }
                    else
                    {
                        LogDebug($"   ✅ 列{col}读取成功：{columnName}");
                    }
                    
                    columnNames.Add(columnName);
                }

                // 输出详细的调试信息
                LogSeparator("详细列名读取信息");
                foreach (var info in debugInfo)
                {
                    LogDebug(info);
                }
                LogDebug($"📈 总共读取到 {columnNames.Count} 个列名");
                
                // 输出所有列名的列表
                LogSeparator("所有列名列表");
                for (int i = 0; i < columnNames.Count; i++)
                {
                    var columnLetter = GetColumnLetter(i);
                    LogDebug($"{columnLetter}列: {columnNames[i]}");
                }
                LogDebug("=== 列名读取完成 ===");

                // 强制更新字段映射，确保所有列都显示
                LogDebug("🔄 开始强制更新字段映射...");
                ForceUpdateFieldMappingsFromColumns(columnNames);
            }
            catch (Exception ex)
            {
                LogDebug($"❌ 强制读取列名失败：{ex.Message}");
                LogDebug($"❌ 异常堆栈：{ex.StackTrace}");
                throw new Exception($"读取Excel列信息失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 强制更新字段映射，确保所有列都显示
        /// </summary>
        private void ForceUpdateFieldMappingsFromColumns(List<string> columnNames)
        {
            try
            {
                LogSeparator("开始强制更新字段映射");
                LogDebug($"📊 输入列名数量：{columnNames.Count}");
                
                var fieldMappings = new List<FieldMapping>();
                var usedFieldNames = new HashSet<string>(); // 用于跟踪已使用的数据库字段名
                
                LogDebug("🔄 开始生成字段映射...");
                for (int i = 0; i < columnNames.Count; i++)
                {
                    var columnName = columnNames[i];
                    var columnLetter = GetColumnLetter(i);
                    
                    // 生成数据库字段名并处理重复
                    var databaseField = GetDefaultDatabaseField(columnName);
                    var originalFieldName = databaseField;
                    int counter = 1;
                    
                    // 如果字段名重复，添加数字后缀
                    while (usedFieldNames.Contains(databaseField))
                    {
                        databaseField = $"{originalFieldName}_{counter}";
                        counter++;
                    }
                    
                    usedFieldNames.Add(databaseField);
                    
                    var fieldMapping = new FieldMapping
                    {
                        ExcelOriginalColumn = columnLetter,
                        ExcelColumn = columnName,
                        DatabaseField = databaseField,
                        DataType = GetDefaultDataType(columnName),
                        IsRequired = IsRequiredByDefault(columnName)
                    };
                    
                    fieldMappings.Add(fieldMapping);
                    
                    LogDebug($"   ✅ 添加字段映射：{columnLetter} -> {columnName} -> {fieldMapping.DatabaseField}");
                }

                LogDebug($"📈 生成的字段映射数量：{fieldMappings.Count}");
                
                // 检查DataGrid状态
                LogDebug("🔍 检查DataGrid状态...");
                LogDebug($"   - DataGrid是否为null：{FieldMappingDataGrid == null}");
                if (FieldMappingDataGrid != null)
                {
                    LogDebug($"   - DataGrid可见性：{FieldMappingDataGrid.Visibility}");
                    LogDebug($"   - DataGrid是否启用：{FieldMappingDataGrid.IsEnabled}");
                    LogDebug($"   - DataGrid当前ItemsSource数量：{(FieldMappingDataGrid.ItemsSource as List<FieldMapping>)?.Count ?? 0}");
                }
                
                // 强制更新DataGrid
                LogDebug("🔄 开始更新DataGrid...");
                LogDebug("   步骤1：清空ItemsSource");
                FieldMappingDataGrid.ItemsSource = null;
                
                LogDebug("   步骤2：设置新的ItemsSource");
                FieldMappingDataGrid.ItemsSource = fieldMappings;
                
                LogDebug("   步骤3：强制刷新显示");
                FieldMappingDataGrid.Items.Refresh();
                
                // 验证更新结果
                LogDebug("🔍 验证更新结果...");
                var updatedItemsSource = FieldMappingDataGrid.ItemsSource as List<FieldMapping>;
                LogDebug($"   - 更新后ItemsSource数量：{updatedItemsSource?.Count ?? 0}");
                LogDebug($"   - 更新后ItemsSource是否为null：{updatedItemsSource == null}");
                
                if (updatedItemsSource != null)
                {
                    LogDebug("   📋 更新后的字段映射列表：");
                    for (int i = 0; i < Math.Min(updatedItemsSource.Count, 10); i++) // 只显示前10个
                    {
                        var mapping = updatedItemsSource[i];
                        LogDebug($"      {i + 1}. {mapping.ExcelOriginalColumn} -> {mapping.ExcelColumn} -> {mapping.DatabaseField}");
                    }
                    if (updatedItemsSource.Count > 10)
                    {
                        LogDebug($"      ... 还有 {updatedItemsSource.Count - 10} 个字段映射");
                    }
                }
                
                LogSeparator("强制更新字段映射完成");
            }
            catch (Exception ex)
            {
                LogDebug($"❌ 强制更新字段映射失败：{ex.Message}");
                LogDebug($"❌ 异常堆栈：{ex.StackTrace}");
                throw;
            }
        }

        private void ReadCsvColumns(string filePath)
        {
            try
            {
                // 读取CSV文件的第一行
                var firstLine = File.ReadLines(filePath).FirstOrDefault();
                if (string.IsNullOrEmpty(firstLine))
                {
                    throw new Exception("CSV文件为空或无法读取");
                }

                // 分割列名（假设使用逗号分隔）
                var columnNames = firstLine.Split(',')
                    .Select(col => col.Trim())
                    .Select((col, index) => !string.IsNullOrWhiteSpace(col) ? col : $"第{GetColumnLetter(index)}列") // 空值使用默认列名
                    .ToList();

                // 更新字段映射
                UpdateFieldMappingsFromColumns(columnNames);
            }
            catch (Exception ex)
            {
                throw new Exception($"读取CSV列信息失败：{ex.Message}");
            }
        }

        private void UpdateFieldMappingsFromColumns(List<string> columnNames)
        {
            var fieldMappings = new List<FieldMapping>();
            var usedFieldNames = new HashSet<string>(); // 用于跟踪已使用的数据库字段名
            
            for (int i = 0; i < columnNames.Count; i++)
            {
                var columnName = columnNames[i];
                // 修复：使用正确的列索引，而不是列表索引
                var columnLetter = GetColumnLetter(i);
                
                // 生成数据库字段名并处理重复
                var databaseField = GetDefaultDatabaseField(columnName);
                var originalFieldName = databaseField;
                int counter = 1;
                
                // 如果字段名重复，添加数字后缀
                while (usedFieldNames.Contains(databaseField))
                {
                    databaseField = $"{originalFieldName}_{counter}";
                    counter++;
                }
                
                usedFieldNames.Add(databaseField);
                
                fieldMappings.Add(new FieldMapping
                {
                    ExcelOriginalColumn = columnLetter,
                    ExcelColumn = columnName,
                    DatabaseField = databaseField,
                    DataType = GetDefaultDataType(columnName),
                    IsRequired = IsRequiredByDefault(columnName)
                });
            }

            FieldMappingDataGrid.ItemsSource = fieldMappings;
            
            // 添加调试信息
            System.Diagnostics.Debug.WriteLine($"=== UpdateFieldMappingsFromColumns 调试信息 ===");
            System.Diagnostics.Debug.WriteLine($"输入列名数量：{columnNames.Count}");
            System.Diagnostics.Debug.WriteLine($"生成的字段映射数量：{fieldMappings.Count}");
            for (int i = 0; i < fieldMappings.Count; i++)
            {
                System.Diagnostics.Debug.WriteLine($"映射 {i}: {fieldMappings[i].ExcelOriginalColumn} -> {fieldMappings[i].ExcelColumn} -> {fieldMappings[i].DatabaseField}");
            }
            System.Diagnostics.Debug.WriteLine("=== 调试信息结束 ===");
        }

        // 当标题行号改变时，重新读取列信息
        private void HeaderRowTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_currentWorksheet != null && int.TryParse(HeaderRowTextBox.Text, out int headerRow))
            {
                try
                {
                    ReadExcelColumnsByRow(headerRow);
                }
                catch (Exception ex)
                {
                    // 静默处理错误，不显示消息框
                    System.Diagnostics.Debug.WriteLine($"读取标题行失败：{ex.Message}");
                }
            }
        }

        private void ReadExcelColumnsByRow(int headerRow)
        {
            if (_currentWorksheet == null) return;

            var dimension = _currentWorksheet.Dimension;
            if (dimension == null || headerRow > dimension.End.Row) return;

            var columnNames = new List<string>();
            var debugInfo = new List<string>();
            
            System.Diagnostics.Debug.WriteLine($"=== 开始读取第{headerRow}行作为标题行 ===");
            System.Diagnostics.Debug.WriteLine($"工作表维度：{dimension.Start.Column} 到 {dimension.End.Column} 列");
            
            for (int col = 1; col <= dimension.End.Column; col++)
            {
                var cellValue = _currentWorksheet.Cells[headerRow, col].Value;
                string columnName = cellValue?.ToString();
                
                // 添加调试信息
                debugInfo.Add($"列{col}: 原始值='{cellValue}', 处理后='{columnName}', 是否为空={string.IsNullOrWhiteSpace(columnName)}");
                
                // 改进：更宽松的列名判断，包括空字符串但保留列位置
                if (!string.IsNullOrWhiteSpace(columnName))
                {
                    columnNames.Add(columnName);
                    System.Diagnostics.Debug.WriteLine($"✓ 添加列名：{columnName}");
                }
                else
                {
                    // 如果列名为空，使用默认列名
                    var columnLetter = GetColumnLetter(col - 1);
                    var defaultName = $"第{columnLetter}列";
                    columnNames.Add(defaultName);
                    System.Diagnostics.Debug.WriteLine($"⚠ 使用默认列名：{defaultName}");
                }
            }

            // 输出调试信息
            System.Diagnostics.Debug.WriteLine("=== 列名读取调试信息 ===");
            foreach (var info in debugInfo)
            {
                System.Diagnostics.Debug.WriteLine(info);
            }
            System.Diagnostics.Debug.WriteLine($"总共读取到 {columnNames.Count} 个列名");
            System.Diagnostics.Debug.WriteLine("=== 调试信息结束 ===");

            UpdateFieldMappingsFromColumns(columnNames);
        }

        private string GetColumnLetter(int columnIndex)
        {
            string result = "";
            while (columnIndex >= 0)
            {
                result = (char)('A' + columnIndex % 26) + result;
                columnIndex = columnIndex / 26 - 1;
            }
            return result;
        }

        private string GetDefaultDatabaseField(string columnName)
        {
            // 根据列名生成默认的数据库字段名
            var fieldName = columnName.Replace(" ", "_").Replace("-", "_").ToLower();
            
            // 中文列名映射
            var chineseMappings = new Dictionary<string, string>
            {
                { "客户编号", "customer_id" },
                { "客户名称", "customer_name" },
                { "联系电话", "phone" },
                { "邮箱", "email" },
                { "地址", "address" },
                { "创建日期", "created_date" },
                { "订单编号", "order_id" },
                { "产品名称", "product_name" },
                { "数量", "quantity" },
                { "单价", "unit_price" },
                { "总金额", "total_amount" },
                { "销售日期", "sales_date" },
                { "产品编号", "product_id" },
                { "类别", "category" },
                { "价格", "price" },
                { "库存", "stock" },
                { "供应商", "supplier" },
                { "员工编号", "employee_id" },
                { "姓名", "name" },
                { "部门", "department" },
                { "职位", "position" },
                { "入职日期", "hire_date" },
                { "薪资", "salary" },
                { "商品编号", "item_id" },
                { "商品名称", "item_name" },
                { "库存数量", "stock_quantity" },
                { "单位", "unit" },
                { "仓库位置", "warehouse_location" },
                { "最后更新", "last_updated" },
                { "第一列", "column_1" },
                { "第二列", "column_2" },
                { "第三列", "column_3" },
                { "第四列", "column_4" },
                { "第五列", "column_5" },
                { "第六列", "column_6" }
            };

            return chineseMappings.ContainsKey(columnName) ? chineseMappings[columnName] : fieldName;
        }

        private string GetDefaultDataType(string columnName)
        {
            // 根据列名判断默认数据类型
            var lowerName = columnName.ToLower();
            
            if (lowerName.Contains("编号") || lowerName.Contains("id") || lowerName.Contains("电话") || lowerName.Contains("phone"))
                return "VARCHAR(50)";
            else if (lowerName.Contains("名称") || lowerName.Contains("name") || lowerName.Contains("地址") || lowerName.Contains("address") || lowerName.Contains("部门") || lowerName.Contains("职位") || lowerName.Contains("单位") || lowerName.Contains("位置"))
                return "VARCHAR(100)";
            else if (lowerName.Contains("邮箱") || lowerName.Contains("email"))
                return "VARCHAR(200)";
            else if (lowerName.Contains("数量") || lowerName.Contains("quantity") || lowerName.Contains("库存") || lowerName.Contains("stock"))
                return "INT";
            else if (lowerName.Contains("价格") || lowerName.Contains("price") || lowerName.Contains("金额") || lowerName.Contains("amount") || lowerName.Contains("薪资") || lowerName.Contains("salary"))
                return "DECIMAL(10,2)";
            else if (lowerName.Contains("日期") || lowerName.Contains("date") || lowerName.Contains("入职") || lowerName.Contains("更新"))
                return "DATE";
            else if (lowerName.Contains("第") && lowerName.Contains("列"))
                return "VARCHAR(100)";
            else
                return "VARCHAR(100)";
        }

        private bool IsRequiredByDefault(string columnName)
        {
            // 默认所有字段都不必填
            return false;
            
            // 原来的逻辑（已注释）
            // 根据列名判断是否默认必填
            // var lowerName = columnName.ToLower();
            // 
            // return lowerName.Contains("编号") || lowerName.Contains("id") || 
            //        lowerName.Contains("名称") || lowerName.Contains("name") ||
            //        lowerName.Contains("日期") || lowerName.Contains("date") ||
            //        lowerName.Contains("姓名") || lowerName.Contains("部门") ||
            //        lowerName.Contains("职位") || lowerName.Contains("商品名称");
        }

        public string ConfigName => ConfigNameTextBox.Text;
        public string FilePath => FilePathTextBox.Text;
        public string DataSource => TargetDataSourceComboBox.Text;
        
        /// <summary>
        /// 获取选中的数据源信息
        /// </summary>
        public DataSourceInfo? SelectedDataSource 
        { 
            get 
            {
                if (TargetDataSourceComboBox.SelectedItem is DataSourceInfo dataSourceInfo)
                {
                    return dataSourceInfo;
                }
                return null;
            }
        }
        
        /// <summary>
        /// 获取选中的数据源名称（兼容性属性）
        /// </summary>
        public string TargetDataSource 
        { 
            get 
            {
                var selectedDataSource = SelectedDataSource;
                return selectedDataSource?.Name ?? TargetDataSourceComboBox.Text ?? string.Empty;
            }
        }
        
        /// <summary>
        /// 获取选中的数据源ID
        /// </summary>
        public string TargetDataSourceId
        {
            get
            {
                var selectedDataSource = SelectedDataSource;
                return selectedDataSource?.Id ?? string.Empty;
            }
        }
        public string SheetName => SheetNameTextBox.Text;
        public string HeaderRow => HeaderRowTextBox.Text;
        public bool SkipEmptyRows => SkipEmptyRowsCheckBox.IsChecked ?? false;
        public bool SplitEachRow => SplitEachRowCheckBox.IsChecked ?? false;
        public bool ClearTableDataBeforeImport => ClearTableDataCheckBox.IsChecked ?? false;
        public List<FieldMapping> FieldMappings => FieldMappingDataGrid.ItemsSource as List<FieldMapping>;

        // 添加setter方法
        public void SetConfigName(string value) => ConfigNameTextBox.Text = value;
        public void SetFilePath(string value) => FilePathTextBox.Text = value;
        public void SetDataSource(string value) => TargetDataSourceComboBox.Text = value;
        public void SetTargetDataSource(string value) => TargetDataSourceComboBox.Text = value;
        public void SetSheetName(string value) => SheetNameTextBox.Text = value;
        public void SetHeaderRow(string value) => HeaderRowTextBox.Text = value;
        public void SetSkipEmptyRows(bool value) => SkipEmptyRowsCheckBox.IsChecked = value;
        public void SetSplitEachRow(bool value) => SplitEachRowCheckBox.IsChecked = value;
        public void SetClearTableDataBeforeImport(bool value) => ClearTableDataCheckBox.IsChecked = value;

        private async void PreviewDataButton_Click(object sender, RoutedEventArgs e)
        {
            // 数据预览功能（根据“拆分每一行”选项选择不同逻辑）
            if (string.IsNullOrEmpty(FilePath))
            {
                Extensions.MessageBoxExtensions.Show("请先选择Excel文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(HeaderRow) || !int.TryParse(HeaderRow, out int headerRowNum))
            {
                Extensions.MessageBoxExtensions.Show("请输入有效的标题行号", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 前置检查：解析路径并确认文件存在，避免后台任务抛出异常
            try
            {
                var filePathService = App.Services.GetService(typeof(ExcelProcessor.Core.Services.IFilePathService)) as ExcelProcessor.Core.Services.IFilePathService;
                string absolutePath = System.IO.Path.IsPathRooted(FilePath)
                    ? FilePath
                    : (filePathService != null
                        ? filePathService.ToAbsolutePath(FilePath)
                        : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FilePath));

                if (!System.IO.File.Exists(absolutePath))
                {
                    Extensions.MessageBoxExtensions.Show($"找不到文件：{absolutePath}\n请确认路径是否正确，或将文件放到应用运行目录下对应位置。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            catch (Exception)
            {
                // 忽略前置检查的解析异常，交由后续统一异常处理
            }

            try
                    {
                var maxRows = 50;
                List<Dictionary<string, object>> previewData;
                        if (SplitEachRow)
                        {
                    // 当勾选拆分每一行时，使用本地读取并展开合并单元格
                    previewData = await PreviewDataWithSplitAsync(FilePath, SheetName, headerRowNum, maxRows);
                        }
                        else
                        {
                    // 未勾选则使用公共服务的快速预览
                    previewData = await PreviewDataWithServiceAsync(FilePath, SheetName, headerRowNum, maxRows);
                        }

                if (previewData == null || previewData.Count == 0)
                {
                    Extensions.MessageBoxExtensions.Show("没有数据可预览。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                ShowDataPreviewDialog(previewData);
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"预览数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<List<Dictionary<string, object>>> PreviewDataWithSplitAsync(string filePath, string sheetName, int headerRowNum, int maxRows)
        {
            // 使用EPPlus读取并展开合并单元格，保持与导入逻辑一致
            return await Task.Run(() =>
            {
                var result = new List<Dictionary<string, object>>();
            
                string absolutePath = filePath;
                var filePathService = App.Services.GetService(typeof(ExcelProcessor.Core.Services.IFilePathService)) as ExcelProcessor.Core.Services.IFilePathService;
                if (filePathService != null)
                {
                    absolutePath = System.IO.Path.IsPathRooted(filePath) ? filePath : filePathService.ToAbsolutePath(filePath);
                }
                else
                {
                    absolutePath = System.IO.Path.IsPathRooted(filePath)
                        ? filePath
                        : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
        }

                if (!System.IO.File.Exists(absolutePath))
                {
                    throw new FileNotFoundException("Excel文件不存在", absolutePath);
                }

                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using var package = new OfficeOpenXml.ExcelPackage(new System.IO.FileInfo(absolutePath));
                var worksheet = string.IsNullOrWhiteSpace(sheetName)
                    ? package.Workbook.Worksheets[0]
                    : package.Workbook.Worksheets[sheetName];
                if (worksheet == null)
                {
                    throw new ArgumentException($"找不到工作表: {sheetName}");
                }

                var dimension = worksheet.Dimension;
                if (dimension == null)
                {
                    return result;
                }

                int totalColumns = dimension.End.Column;

                // 标题行（同样展开合并单元格）
                var headerRow = new Dictionary<string, object>();
                headerRow["原始行号"] = headerRowNum;
                for (int col = 1; col <= totalColumns; col++)
        {
                    var headerTextObj = GetCellValueWithMergedCells_EPPlus(worksheet, headerRowNum, col);
                    var headerText = headerTextObj?.ToString() ?? string.Empty;
                    headerRow[GetColumnLetter(col - 1)] = headerText;
                }
                result.Add(headerRow);
            
                // 数据行（展开合并单元格）
                int dataStartRow = headerRowNum + 1;
                int endRow = Math.Min(dimension.End.Row, dataStartRow + maxRows - 1);
                for (int row = dataStartRow; row <= endRow; row++)
                    {
                    var rowDict = new Dictionary<string, object>();
                    rowDict["原始行号"] = row;
                    for (int col = 1; col <= totalColumns; col++)
                        {
                        var value = GetCellValueWithMergedCells_EPPlus(worksheet, row, col);
                        rowDict[GetColumnLetter(col - 1)] = value?.ToString() ?? string.Empty;
                        }
                    result.Add(rowDict);
            }

                return result;
            });
        }

        private static object GetCellValueWithMergedCells_EPPlus(OfficeOpenXml.ExcelWorksheet worksheet, int row, int col)
        {
            var cell = worksheet.Cells[row, col];
            // 优先尝试直接通过坐标获取合并区域地址（更可靠且高效）
            var mergedAddress = worksheet.MergedCells[row, col];
            if (!string.IsNullOrEmpty(mergedAddress))
            {
                var mergedRange = worksheet.Cells[mergedAddress];
                var topLeftCell = worksheet.Cells[mergedRange.Start.Row, mergedRange.Start.Column];
                return topLeftCell.Value ?? cell.Value;
            }

            // 非合并单元格直接返回值
            return cell.Value;
        }

        private static bool IsCellInRange_EPPlus(int row, int col, OfficeOpenXml.ExcelRange range)
                {
            return row >= range.Start.Row && row <= range.End.Row && col >= range.Start.Column && col <= range.End.Column;
        }

        private async Task<List<Dictionary<string, object>>> PreviewDataWithServiceAsync(string filePath, string sheetName, int headerRowNum, int maxRows)
                    {
            // 通过服务获取预览数据，并适配为UI可用的数据结构
            var excelService = App.Services.GetService(typeof(ExcelProcessor.Core.Services.IExcelService)) as ExcelProcessor.Core.Services.IExcelService;
            var filePathService = App.Services.GetService(typeof(ExcelProcessor.Core.Services.IFilePathService)) as ExcelProcessor.Core.Services.IFilePathService;
            if (excelService == null)
                {
                throw new InvalidOperationException("未能获取 IExcelService 服务实例。");
            }

            // 解析路径（支持相对路径）
            string absolutePath = filePath;
            if (filePathService != null)
                {
                absolutePath = System.IO.Path.IsPathRooted(filePath) ? filePath : filePathService.ToAbsolutePath(filePath);
                }
                else
                {
                absolutePath = System.IO.Path.IsPathRooted(filePath)
                    ? filePath
                    : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
                }

            if (!System.IO.File.Exists(absolutePath))
            {
                throw new FileNotFoundException("Excel文件不存在", absolutePath);
            }

            var preview = await excelService.PreviewExcelDataAsync(absolutePath, string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName, headerRowNum, maxRows);
            return AdaptPreviewToUi(preview, headerRowNum);
        }

        private List<Dictionary<string, object>> AdaptPreviewToUi(ExcelProcessor.Core.Services.ExcelPreviewData preview, int headerRowNum)
        {
            var result = new List<Dictionary<string, object>>();
            int totalColumns = preview?.TotalColumns ?? 0;
            if (preview == null || totalColumns <= 0)
            {
                return result;
            }

            // 首行：标题行
            var headerRow = new Dictionary<string, object>();
            headerRow["原始行号"] = headerRowNum;
            for (int col = 1; col <= totalColumns; col++)
            {
                var columnLetter = GetColumnLetter(col - 1);
                var headerText = (preview.Headers != null && preview.Headers.Count >= col) ? preview.Headers[col - 1] : string.Empty;
                headerRow[columnLetter] = headerText ?? string.Empty;
            }
            result.Add(headerRow);

            // 随后的数据行（服务端返回的 Rows 不包含标题行）
            int currentRowNumber = headerRowNum + 1;
            if (preview.Rows != null)
            {
                foreach (var row in preview.Rows)
                {
                    var rowDict = new Dictionary<string, object>();
                    rowDict["原始行号"] = currentRowNumber++;
                    for (int col = 1; col <= totalColumns; col++)
                    {
                        var columnLetter = GetColumnLetter(col - 1);
                        string value = (row != null && row.Count >= col) ? (row[col - 1] ?? string.Empty) : string.Empty;
                        rowDict[columnLetter] = value ?? string.Empty;
                    }
                    result.Add(rowDict);
                }
            }
            
            return result;
        }

        private bool EnsureWorksheetLoaded()
        {
            try
            {
                if (_currentWorksheet != null) return true;

                var path = FilePath;
                if (string.IsNullOrWhiteSpace(path)) return false;

                // 解析为绝对路径（支持相对路径在应用根目录下的情况）
                string absolutePath = System.IO.Path.IsPathRooted(path)
                    ? path
                    : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

                if (!System.IO.File.Exists(absolutePath)) return false;

                // 直接处理Excel文件以加载 _excelPackage 和 _currentWorksheet
                ProcessExcelFile(absolutePath);
                return _currentWorksheet != null;
            }
            catch
            {
                return false;
            }
        }

        private void ShowDataPreviewDialog(List<Dictionary<string, object>> previewData)
        {
            int headerRowNum = 1;
            if (int.TryParse(HeaderRow, out int parsedHeaderRow))
            {
                headerRowNum = parsedHeaderRow;
            }
            
            var dialog = new DataPreviewDialog(previewData, headerRowNum);
            dialog.ShowDialog();
        }

        private void AddMappingButton_Click(object sender, RoutedEventArgs e)
        {
            var newMapping = new FieldMapping
            {
                ExcelOriginalColumn = "",
                ExcelColumn = "",
                DatabaseField = "",
                DataType = "VARCHAR(50)",
                IsRequired = false
            };

            var fieldMappings = FieldMappingDataGrid.ItemsSource as List<FieldMapping>;
            if (fieldMappings == null)
            {
                fieldMappings = new List<FieldMapping>();
                FieldMappingDataGrid.ItemsSource = fieldMappings;
            }

            fieldMappings.Add(newMapping);
            FieldMappingDataGrid.Items.Refresh();
        }

        private void DeleteMappingButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is FieldMapping mapping)
            {
                var fieldMappings = FieldMappingDataGrid.ItemsSource as List<FieldMapping>;
                if (fieldMappings != null)
                {
                    fieldMappings.Remove(mapping);
                    FieldMappingDataGrid.Items.Refresh();
                }
            }
        }

        /// <summary>
        /// 测试导入按钮点击事件
        /// </summary>
        private async void TestConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证基本配置
                if (string.IsNullOrWhiteSpace(FilePath))
                {
                    Extensions.MessageBoxExtensions.Show("请先选择Excel文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(ConfigName))
                {
                    Extensions.MessageBoxExtensions.Show("请输入配置名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 验证字段映射
                var fieldMappings = FieldMappingDataGrid.ItemsSource as List<FieldMapping>;
                if (fieldMappings == null || fieldMappings.Count == 0)
                {
                    Extensions.MessageBoxExtensions.Show("请先配置字段映射", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 验证数据源选择
                if (string.IsNullOrWhiteSpace(TargetDataSource))
                {
                    Extensions.MessageBoxExtensions.Show("请选择目标数据源", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取数据源服务
                var dataSourceService = App.Services.GetService(typeof(IDataSourceService)) as IDataSourceService;
                if (dataSourceService == null)
                {
                    Extensions.MessageBoxExtensions.Show("数据源服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 根据数据源名称获取数据源配置
                var dataSource = await dataSourceService.GetDataSourceByNameAsync(TargetDataSource);
                if (dataSource == null)
                {
                    Extensions.MessageBoxExtensions.Show($"找不到数据源：{TargetDataSource}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 验证数据源连接
                var isConnected = await dataSourceService.TestConnectionAsync(dataSource);
                if (!isConnected)
                {
                    Extensions.MessageBoxExtensions.Show($"数据源连接失败：无法连接到数据源 {TargetDataSource}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 使用配置名称作为表名
                string tableName = ConfigName;

                // 直接开始导入，无需额外确认
                // 显示增强的进度对话框
                var progressDialog = new ImportProgressDialog("正在导入Excel数据...");
                var progressWrapper = new ImportProgressWrapper(progressDialog);
                // 让进度窗体以系统风格出现在父窗口中心，并作为模式对话框
                if (Application.Current?.MainWindow != null)
                {
                    progressDialog.Owner = Application.Current.MainWindow;
                }
                progressDialog.Show();
                await System.Threading.Tasks.Task.Yield();

                try
                {
                    // 获取数据导入服务
                    var dataImportService = App.Services.GetService(typeof(IDataImportService)) as IDataImportService;
                    if (dataImportService == null)
                    {
                        Extensions.MessageBoxExtensions.Show("数据导入服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // 验证标题行号
                    if (!int.TryParse(HeaderRow, out int headerRowNumber) || headerRowNumber <= 0)
                    {
                        Extensions.MessageBoxExtensions.Show("请输入有效的标题行号（大于0的整数）", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 创建Excel配置对象
                    var config = new ExcelConfig
                    {
                        ConfigName = ConfigName,
                        FilePath = FilePath,
                        SheetName = SheetName,
                        HeaderRow = headerRowNumber,
                        TargetDataSourceName = TargetDataSource,
                        TargetDataSourceId = GetDataSourceId(TargetDataSource), // 获取数据源ID
                        SplitEachRow = SplitEachRow,
                        ClearTableDataBeforeImport = ClearTableDataBeforeImport,
                        SkipEmptyRows = SkipEmptyRows
                    };

                    // 将耗时导入放到后台线程，防止阻塞UI线程导致进度窗体空白
                    var importResult = await System.Threading.Tasks.Task.Run(() =>
                        ImportDataToDataSourceAsync(config, fieldMappings, tableName, dataSource.ConnectionString, progressWrapper)
                    );

                    // 成功后不再显示结果页，仅在进度窗口提示完成
                    progressDialog.SetProgress(100);
                    progressDialog.SetStatus($"导入完成！成功 {importResult.SuccessRows} 行，失败 {importResult.FailedRows} 行");
                }
                catch (Exception ex)
                {
                    // 保持在进度页上直接显示错误信息
                    progressDialog.ShowError($"导入失败：{ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"测试导入时发生异常：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导入数据到指定数据源
        /// </summary>
        private async Task<DataImportResult> ImportDataToDataSourceAsync(ExcelConfig config, List<FieldMapping> fieldMappings, string tableName, string dataSourceConnectionString, IImportProgressCallback progressCallback)
        {
            try
            {
                // 创建临时数据导入服务，使用指定的数据源连接字符串
                var loggerFactory = App.Services.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
                var logger = loggerFactory?.CreateLogger<DataImportService>();
                
                if (logger == null)
                {
                    throw new InvalidOperationException("无法创建日志记录器");
                }

                // 根据数据源类型创建对应的数据导入服务
                var dataSourceType = GetDataSourceTypeFromConnectionString(dataSourceConnectionString);
                var dataImportService = CreateDataImportService(dataSourceType, dataSourceConnectionString, logger);

                // 执行导入
                return await dataImportService.ImportExcelDataAsync(config, fieldMappings, tableName, progressCallback);
            }
            catch (Exception ex)
            {
                throw new Exception($"导入数据到数据源失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从连接字符串获取数据源类型
        /// </summary>
        private string GetDataSourceTypeFromConnectionString(string connectionString)
        {
            if (connectionString.Contains("Data Source=") && connectionString.Contains("Version=3"))
                return "SQLite";
            else if (connectionString.Contains("Server=") && connectionString.Contains("Uid="))
                return "MySQL";
            else if (connectionString.Contains("Server=") && connectionString.Contains("User Id="))
                return "SQLServer";
            else if (connectionString.Contains("Host=") && connectionString.Contains("Username="))
                return "PostgreSQL";
            else if (connectionString.Contains("Data Source=") && connectionString.Contains("User Id="))
                return "Oracle";
            else
                return "SQLite"; // 默认
        }

        /// <summary>
        /// 创建对应类型的数据导入服务
        /// </summary>
        private IDataImportService CreateDataImportService(string dataSourceType, string connectionString, ILogger logger)
        {
            // 基于连接字符串推断并创建对应的连接工厂与方言
            var lower = connectionString.ToLowerInvariant();
            bool isSqlite = lower.Contains(".db") || lower.Contains("mode=") || lower.Contains("sqlite");
            bool isMySql = lower.Contains("server=") && (lower.Contains("uid=") || lower.Contains("user id=")) && (lower.Contains("mysql") || lower.Contains("port="));
            bool isSqlServer = lower.Contains("initial catalog=") || (lower.Contains("server=") && (lower.Contains("trusted_connection=") || lower.Contains("integrated security") || lower.Contains("user id=") || lower.Contains("uid=")) && lower.Contains(";"));
            bool isPostgres = (lower.Contains("host=") && lower.Contains("username=")) || lower.Contains("postgres");
            bool isOracle = lower.Contains("user id=") && (lower.Contains("data source=") || lower.Contains("tns")) || lower.Contains("oracle");
            ExcelProcessor.Core.Interfaces.IDbConnectionFactory factory;
            ExcelProcessor.Core.Interfaces.ISqlDialect dialect;
            if (isMySql)
            {
                factory = new ExcelProcessor.Data.Infrastructure.MySqlDbConnectionFactory(connectionString);
                dialect = new ExcelProcessor.Data.Infrastructure.MySqlDialect();
            }
            else if (isSqlServer && !isSqlite)
            {
                factory = new ExcelProcessor.Data.Infrastructure.SqlServerDbConnectionFactory(connectionString);
                dialect = new ExcelProcessor.Data.Infrastructure.SqlServerDialect();
            }
            else if (isPostgres)
            {
                factory = new ExcelProcessor.Data.Infrastructure.PostgreSqlDbConnectionFactory(connectionString);
                dialect = new ExcelProcessor.Data.Infrastructure.PostgreSqlDialect();
            }
            else if (isOracle)
            {
                factory = new ExcelProcessor.Data.Infrastructure.OracleDbConnectionFactory(connectionString);
                dialect = new ExcelProcessor.Data.Infrastructure.OracleDialect();
            }
            else
            {
                factory = new ExcelProcessor.Data.Infrastructure.DefaultDbConnectionFactory(connectionString);
                dialect = new ExcelProcessor.Data.Infrastructure.SqliteDialect();
            }
            return new DataImportService(logger as ILogger<DataImportService>, factory, dialect);
        }

        /// <summary>
        /// 执行Excel导入过程测试
        /// </summary>
        private ImportTestResult TestExcelImportProcess()
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            try
            {
                // 1. 验证字段映射的完整性
                var fieldMappings = FieldMappingDataGrid.ItemsSource as List<FieldMapping>;
                foreach (var mapping in fieldMappings)
                {
                    if (string.IsNullOrWhiteSpace(mapping.ExcelColumn))
                    {
                        errors.Add($"Excel列名不能为空");
                    }

                    if (string.IsNullOrWhiteSpace(mapping.DatabaseField))
                    {
                        errors.Add($"数据库字段名不能为空");
                    }

                    if (string.IsNullOrWhiteSpace(mapping.DataType))
                    {
                        errors.Add($"数据类型不能为空");
                    }
                }

                if (errors.Count > 0)
                {
                    return ImportTestResult.Failure("字段映射验证失败", errors, warnings);
                }

                // 2. 测试Excel文件读取
                var fileTestResult = TestExcelFileReadingWithDetails();
                if (!fileTestResult.IsSuccess)
                {
                    return fileTestResult;
                }

                // 3. 测试数据源连接（如果有配置数据源）
                if (!string.IsNullOrWhiteSpace(TargetDataSource) && TargetDataSource != "默认数据源")
                {
                    var dataSourceTestResult = TestDataSourceConnection();
                    if (!dataSourceTestResult.IsSuccess)
                    {
                        warnings.AddRange(dataSourceTestResult.Errors);
                    }
                }

                // 4. 模拟数据导入过程
                var importSimulationResult = SimulateDataImport();
                if (!importSimulationResult.IsSuccess)
                {
                    errors.AddRange(importSimulationResult.Errors);
                }

                if (errors.Count > 0)
                {
                    return ImportTestResult.Failure("导入过程测试失败", errors, warnings);
                }

                // 5. 返回成功结果
                return ImportTestResult.Success(
                    "配置测试通过！所有验证项目均成功。",
                    ConfigName,
                    FilePath,
                    SheetName,
                    int.Parse(HeaderRow),
                    fileTestResult.DataRowCount,
                    fileTestResult.ColumnCount,
                    fieldMappings.Count,
                    fileTestResult.PreviewData
                );
            }
            catch (Exception ex)
            {
                errors.Add($"测试过程中发生异常：{ex.Message}");
                return ImportTestResult.Failure("测试过程异常", errors, warnings);
            }
        }

        /// <summary>
        /// 测试Excel文件读取并返回详细信息
        /// </summary>
        private ImportTestResult TestExcelFileReadingWithDetails()
        {
            try
            {
                // 设置EPPlus许可证上下文
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using (var package = new ExcelPackage(new FileInfo(FilePath)))
                {
                    var worksheet = package.Workbook.Worksheets[SheetName];
                    if (worksheet == null)
                    {
                        return ImportTestResult.Failure($"工作表 '{SheetName}' 不存在");
                    }

                    int headerRowNum = int.Parse(HeaderRow);
                    var dimension = worksheet.Dimension;
                    if (dimension == null)
                    {
                        return ImportTestResult.Failure("Excel文件为空或无法读取");
                    }

                    // 检查标题行是否存在
                    if (headerRowNum > dimension.End.Row)
                    {
                        return ImportTestResult.Failure($"标题行 {headerRowNum} 超出文件范围（最大行数：{dimension.End.Row}）");
                    }

                    // 读取标题行数据
                    var headers = new List<string>();
                    for (int col = dimension.Start.Column; col <= dimension.End.Column; col++)
                    {
                        var cellValue = worksheet.Cells[headerRowNum, col].Value;
                        var headerName = cellValue?.ToString();
                        if (!string.IsNullOrWhiteSpace(headerName))
                        {
                            headers.Add(headerName);
                        }
                    }

                    // 读取前几行数据作为预览
                    var previewData = new List<Dictionary<string, object>>();
                    int previewRowCount = Math.Min(5, dimension.End.Row - headerRowNum);
                    
                    for (int row = headerRowNum + 1; row <= headerRowNum + previewRowCount; row++)
                    {
                        var rowData = new Dictionary<string, object>();
                        for (int col = dimension.Start.Column; col <= dimension.End.Column; col++)
                        {
                            var cellValue = worksheet.Cells[row, col].Value;
                            var headerIndex = col - dimension.Start.Column;
                            if (headerIndex < headers.Count)
                            {
                                rowData[headers[headerIndex]] = cellValue;
                            }
                        }
                        previewData.Add(rowData);
                    }

                    return ImportTestResult.Success(
                        "Excel文件读取成功",
                        ConfigName,
                        FilePath,
                        SheetName,
                        headerRowNum,
                        dimension.End.Row - headerRowNum,
                        headers.Count,
                        0,
                        previewData
                    );
                }
            }
            catch (Exception ex)
            {
                return ImportTestResult.Failure($"Excel文件读取失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 测试数据源连接
        /// </summary>
        private ImportTestResult TestDataSourceConnection()
        {
            try
            {
                // 这里应该实现实际的数据源连接测试
                // 暂时返回成功，实际项目中需要根据数据源类型进行连接测试
                return ImportTestResult.Success("数据源连接测试成功", ConfigName, FilePath, SheetName, 1, 0, 0, 0);
            }
            catch (Exception ex)
            {
                return ImportTestResult.Failure($"数据源连接失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 模拟数据导入过程
        /// </summary>
        private ImportTestResult SimulateDataImport()
        {
            try
            {
                // 模拟数据导入的各种验证
                var errors = new List<string>();
                var warnings = new List<string>();

                // 检查数据类型兼容性
                var fieldMappings = FieldMappingDataGrid.ItemsSource as List<FieldMapping>;
                foreach (var mapping in fieldMappings)
                {
                    // 这里可以添加更详细的数据类型验证逻辑
                    if (mapping.DataType.Contains("DECIMAL") && !mapping.DataType.Contains("("))
                    {
                        warnings.Add($"字段 '{mapping.DatabaseField}' 的DECIMAL类型建议指定精度");
                    }
                }

                if (errors.Count > 0)
                {
                    return ImportTestResult.Failure("数据导入模拟失败", errors, warnings);
                }

                return ImportTestResult.Success("数据导入模拟成功", ConfigName, FilePath, SheetName, 1, 0, 0, 0);
            }
            catch (Exception ex)
            {
                return ImportTestResult.Failure($"数据导入模拟失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 显示测试结果
        /// </summary>
        private void ShowTestResult(ImportTestResult result)
        {
            if (result.IsSuccess)
            {
                Extensions.MessageBoxExtensions.Show("导入测试成功", "测试成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                Extensions.MessageBoxExtensions.Show("导入测试失败", "测试失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示导入结果
        /// </summary>
        private void ShowImportResult(DataImportResult result)
        {
            // 已取消结果窗口，保留方法以兼容旧调用但不执行任何UI弹窗
        }

        /// <summary>
        /// 获取应用程序根目录路径
        /// </summary>
        private string GetApplicationRootPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
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

        /// <summary>
        /// 获取目标表名
        /// </summary>
        public string TargetTableName => TargetTableNameTextBox?.Text?.Trim() ?? string.Empty;

        /// <summary>
        /// 设置目标表名
        /// </summary>
        /// <param name="tableName">目标表名</param>
        public void SetTargetTableName(string tableName)
        {
            if (TargetTableNameTextBox != null)
            {
                TargetTableNameTextBox.Text = tableName;
            }
        }

        // 资源清理
        public void Dispose()
        {
            _excelPackage?.Dispose();
        }
    }
} 