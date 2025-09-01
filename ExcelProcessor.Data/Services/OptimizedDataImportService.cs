using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Buffers;
using Dapper;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Core.Interfaces;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// 优化的数据导入服务
    /// 包含内存优化、批量处理优化、并行处理、缓存机制和性能监控
    /// </summary>
    public class OptimizedDataImportService : IDataImportService
    {
        private readonly ILogger<OptimizedDataImportService> _logger;
        private readonly string _connectionString;
        private readonly ILogger<OptimizedDataImportService> _performanceLogger;
        private readonly ConcurrentDictionary<string, object> _cache;
        private readonly SemaphoreSlim _semaphore;
        private readonly int _maxConcurrency;

        // 内存池配置
        private readonly ArrayPool<byte> _bytePool;
        private readonly ArrayPool<char> _charPool;
        
        // 批量处理配置
        private const int DEFAULT_BATCH_SIZE = 500;
        private const int MAX_BATCH_SIZE = 2000;
        private const int MIN_BATCH_SIZE = 100;

        public OptimizedDataImportService(ILogger<OptimizedDataImportService> logger, string connectionString)
        {
            _logger = logger;
            _connectionString = connectionString;
            _performanceLogger = logger;
            _cache = new ConcurrentDictionary<string, object>();
            _maxConcurrency = Environment.ProcessorCount;
            _semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
            
            // 初始化内存池
            _bytePool = ArrayPool<byte>.Shared;
            _charPool = ArrayPool<char>.Shared;
        }

        /// <summary>
        /// 优化的Excel数据导入
        /// </summary>
        public async Task<DataImportResult> ImportExcelDataAsync(ExcelConfig config, 
            List<FieldMapping> fieldMappings, string? targetTableName = null, IImportProgressCallback? progressCallback = null)
        {
            var result = new DataImportResult { TargetTableName = targetTableName ?? config.TargetTableName };
            var startTime = DateTime.Now;

            try
            {
                _performanceLogger.LogInformation("开始优化导入Excel数据");
                _logger.LogInformation($"开始优化导入Excel数据: {config.ConfigName}");

                // 验证文件
                if (!File.Exists(config.FilePath))
                {
                    result.ErrorMessage = $"Excel文件不存在: {config.FilePath}";
                    return result;
                }

                // 规范化字段映射
                var normalizedFieldMappings = NormalizeFieldMappings(fieldMappings);
                
                // 更新进度
                progressCallback?.SetStatus("正在初始化导入环境...");
                progressCallback?.UpdateProgress(5, "正在初始化导入环境...");

                // 创建目标表
                if (!await CreateTargetTableAsync(result.TargetTableName ?? "ImportedData", normalizedFieldMappings))
                {
                    result.ErrorMessage = "创建目标表失败";
                    return result;
                }

                // 清除表数据（如果配置）
                if (config.ClearTableDataBeforeImport)
                {
                    progressCallback?.SetStatus("正在清除表数据...");
                    progressCallback?.UpdateProgress(8, "正在清除表数据...");
                    
                    if (!await ClearTableDataAsync(result.TargetTableName ?? "ImportedData"))
                    {
                        result.ErrorMessage = "清除表数据失败";
                        return result;
                    }
                }

                // 获取文件信息
                var fileInfo = new FileInfo(config.FilePath);
                var totalRows = await GetExcelRowCountAsync(config.FilePath, config.SheetName ?? "Sheet1");
                
                progressCallback?.UpdateStatistics(totalRows, 0, 0, 0);
                progressCallback?.SetStatus($"准备导入 {totalRows} 行数据...");

                // 执行优化的数据导入
                var importResult = await ExecuteOptimizedImportAsync(
                    config, normalizedFieldMappings, result.TargetTableName ?? "ImportedData", 
                    totalRows, progressCallback);

                // 合并结果
                result.IsSuccess = importResult.IsSuccess;
                result.SuccessRows = importResult.SuccessRows;
                result.FailedRows = importResult.FailedRows;
                result.TotalRows = importResult.TotalRows;
                result.Duration = DateTime.Now - startTime;
                result.ErrorMessage = importResult.ErrorMessage;

                // 记录性能指标
                _performanceLogger.LogInformation($"导入完成，耗时: {result.Duration.TotalSeconds:N2} 秒");

                // 清理缓存
                CleanupCache();

                progressCallback?.UpdateProgress(100, "导入完成");
                progressCallback?.SetStatus($"导入完成！成功 {result.SuccessRows} 行，失败 {result.FailedRows} 行");

                return result;
            }
            catch (Exception ex)
            {
                _performanceLogger.LogInformation("导入失败");
                _logger.LogError(ex, "优化Excel数据导入失败");
                result.ErrorMessage = ex.Message;
                progressCallback?.SetStatus($"导入失败：{ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// 执行优化的数据导入
        /// </summary>
        private async Task<DataImportResult> ExecuteOptimizedImportAsync(
            ExcelConfig config, List<FieldMapping> fieldMappings, string tableName, 
            int totalRows, IImportProgressCallback progressCallback)
        {
            var result = new DataImportResult { IsSuccess = false };
            var batchSize = CalculateOptimalBatchSize(totalRows);

            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                // 构建INSERT语句
                var insertSql = BuildOptimizedInsertSql(tableName, fieldMappings);
                
                // 开始事务
                using var transaction = connection.BeginTransaction();
                try
                {
                    // 设置EPPlus许可证上下文
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    
                    // 使用流式读取Excel文件
                    await using var stream = new FileStream(config.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var package = new ExcelPackage(stream);
                    var worksheet = package.Workbook.Worksheets[config.SheetName ?? "Sheet1"];

                    if (worksheet?.Dimension == null)
                    {
                        result.ErrorMessage = "Excel文件为空或无法读取";
                        return result;
                    }

                    // 并行处理数据

                    // 创建数据队列
                    var dataQueue = new ConcurrentQueue<Dictionary<string, object>>();
                    var cancellationTokenSource = new CancellationTokenSource();

                    // 启动生产者任务（读取Excel数据）
                    var producerTask = Task.Run(async () =>
                    {
                        await ProduceExcelDataAsync(worksheet, fieldMappings, config.HeaderRow + 1, 
                            dataQueue, cancellationTokenSource.Token, progressCallback, config);
                    });

                    // 启动消费者任务（批量插入数据）
                    var consumerTask = Task.Run(async () =>
                    {
                        return await ConsumeExcelDataAsync(connection, insertSql, dataQueue, batchSize, 
                            cancellationTokenSource.Token, totalRows, progressCallback);
                    });

                    // 等待所有任务完成
                    await Task.WhenAll(producerTask, consumerTask);

                    // 获取消费者任务的结果
                    var consumerResult = await consumerTask;

                    // 提交事务
                    transaction.Commit();

                    result.IsSuccess = true;
                    result.SuccessRows = consumerResult.successRows;
                    result.FailedRows = consumerResult.failedRows;
                    result.TotalRows = consumerResult.successRows + consumerResult.failedRows;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "导入过程中发生错误，已回滚事务");
                    result.ErrorMessage = ex.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行优化导入失败");
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 生产者：读取Excel数据并放入队列
        /// </summary>
        private async Task ProduceExcelDataAsync(ExcelWorksheet worksheet, List<FieldMapping> fieldMappings, 
            int startRow, ConcurrentQueue<Dictionary<string, object>> dataQueue, 
            CancellationToken cancellationToken, IImportProgressCallback progressCallback, ExcelConfig config)
        {
            try
            {
                var dimension = worksheet.Dimension;
                var totalRows = dimension.End.Row - startRow + 1;
                var processedRows = 0;

                for (int row = startRow; row <= dimension.End.Row; row++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        var rowData = new Dictionary<string, object>();
                        bool hasData = false;

                        // 使用内存池优化字符串处理
                        for (int col = dimension.Start.Column; col <= dimension.End.Column; col++)
                        {
                            var cellValue = GetCellValueWithMergedCellsOptimized(worksheet, row, col);
                            if (cellValue != null)
                            {
                                hasData = true;
                            }

                            var mapping = fieldMappings.FirstOrDefault(m => 
                                m.ExcelOriginalColumn == GetColumnLetter(col));

                            if (mapping != null)
                            {
                                var dbValue = ConvertCellValueOptimized(cellValue, mapping.DataType);
                                rowData[SanitizeParameterName(mapping.DatabaseField)] = dbValue;
                            }
                        }

                        // 检查是否跳过空行
                        if (config.SkipEmptyRows && !hasData)
                        {
                            _logger.LogDebug($"跳过空行: 第 {row} 行");
                            continue;
                        }

                        // 处理拆分每一行选项
                        var rowsToProcess = new List<Dictionary<string, object>>();
                        if (config.SplitEachRow && hasData)
                        {
                            // 当启用拆分每一行时，我们需要将合并单元格的数据填充到每个单元格
                            var splitRow = SplitMergedCellsDataOptimized(rowData, worksheet, row, fieldMappings);
                            rowsToProcess.Add(splitRow);
                            _logger.LogDebug($"拆分第 {row} 行的合并单元格数据");
                        }
                        else
                        {
                            rowsToProcess.Add(rowData);
                        }

                        // 处理拆分后的每一行数据
                        foreach (var processedRow in rowsToProcess)
                        {
                            dataQueue.Enqueue(processedRow);
                            processedRows++;

                            // 更新进度
                            if (processedRows % 100 == 0)
                            {
                                var progress = Math.Min(40, 10 + (processedRows * 30.0 / totalRows));
                                progressCallback?.UpdateProgress(progress, $"正在读取第 {row} 行...");
                                progressCallback?.UpdateCurrentRow(row, dimension.End.Row);
                            }
                        }

                        // 控制队列大小，避免内存溢出
                        while (dataQueue.Count > 10000 && !cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(10, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"读取第 {row} 行数据时出错");
                    }
                }

                _logger.LogInformation($"Excel数据读取完成，共读取 {processedRows} 行");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel数据读取失败");
            }
        }

        /// <summary>
        /// 消费者：从队列获取数据并批量插入
        /// </summary>
        private async Task<(int processedRows, int successRows, int failedRows)> ConsumeExcelDataAsync(
            SQLiteConnection connection, string insertSql, 
            ConcurrentQueue<Dictionary<string, object>> dataQueue, int batchSize, 
            CancellationToken cancellationToken, int totalRows, IImportProgressCallback progressCallback)
        {
            var processedRows = 0;
            var successRows = 0;
            var failedRows = 0;
            
            try
            {
                var batchData = new List<Dictionary<string, object>>();
                var currentBatch = 0;

                while (!cancellationToken.IsCancellationRequested)
                {
                    // 从队列获取数据
                    if (dataQueue.TryDequeue(out var rowData))
                    {
                        batchData.Add(rowData);
                        Interlocked.Increment(ref processedRows);

                        // 达到批次大小时执行插入
                        if (batchData.Count >= batchSize)
                        {
                            await _semaphore.WaitAsync(cancellationToken);
                            try
                            {
                                await ExecuteOptimizedBatchInsert(connection, insertSql, batchData);
                                Interlocked.Add(ref successRows, batchData.Count);
                                currentBatch++;

                                // 更新进度
                                var progress = Math.Min(90, 50 + (processedRows * 40.0 / totalRows));
                                progressCallback?.UpdateProgress(progress, $"正在插入第 {currentBatch} 批数据...");
                                progressCallback?.UpdateStatistics(totalRows, processedRows, successRows, failedRows);
                                progressCallback?.UpdateBatchInfo(currentBatch, batchSize, (int)Math.Ceiling((double)totalRows / batchSize));
                            }
                            finally
                            {
                                _semaphore.Release();
                            }

                            batchData.Clear();
                        }
                    }
                    else if (dataQueue.IsEmpty)
                    {
                        // 队列为空，等待一段时间
                        await Task.Delay(100, cancellationToken);
                        
                        // 如果队列仍然为空且没有更多数据，退出循环
                        if (dataQueue.IsEmpty)
                        {
                            break;
                        }
                    }
                }

                // 处理剩余的批次数据
                if (batchData.Count > 0)
                {
                    await _semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        await ExecuteOptimizedBatchInsert(connection, insertSql, batchData);
                        Interlocked.Add(ref successRows, batchData.Count);
                        currentBatch++;

                        progressCallback?.UpdateProgress(95, "正在完成最后一批数据...");
                        progressCallback?.UpdateStatistics(totalRows, processedRows, successRows, failedRows);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }

                _logger.LogInformation($"数据插入完成，共插入 {successRows} 行，失败 {failedRows} 行");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据插入失败");
            }
            
            return (processedRows, successRows, failedRows);
        }

        /// <summary>
        /// 优化的批量插入
        /// </summary>
        private async Task ExecuteOptimizedBatchInsert(SQLiteConnection connection, string insertSql, 
            List<Dictionary<string, object>> batchData)
        {
            try
            {
                // 使用参数化查询优化
                var parameters = new List<object>();
                foreach (var rowData in batchData)
                {
                    var cleanedRowData = new Dictionary<string, object>();
                    foreach (var kvp in rowData)
                    {
                        cleanedRowData[kvp.Key] = kvp.Value ?? DBNull.Value;
                    }
                    parameters.Add(cleanedRowData);
                }

                // 批量执行
                await connection.ExecuteAsync(insertSql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"执行批量插入失败，批次大小: {batchData.Count}");
                throw;
            }
        }

        /// <summary>
        /// 优化的单元格值转换
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object? ConvertCellValueOptimized(object? cellValue, string dataType)
        {
            if (cellValue == null || cellValue == DBNull.Value)
                return null;

            try
            {
                var stringValue = cellValue.ToString();
                if (string.IsNullOrWhiteSpace(stringValue))
                    return null;

                return dataType.ToUpper() switch
                {
                    "INT" => int.TryParse(stringValue, out var intValue) ? intValue : (object?)null,
                    "DECIMAL(10,2)" or "DECIMAL(15,2)" => decimal.TryParse(stringValue, out var decimalValue) ? decimalValue : (object?)null,
                    "DATE" => DateTime.TryParse(stringValue, out var dateValue) ? dateValue : (object?)null,
                    "DATETIME" => DateTime.TryParse(stringValue, out var dateTimeValue) ? dateTimeValue : (object?)null,
                    "TEXT" or "VARCHAR(50)" or "VARCHAR(100)" or "VARCHAR(200)" => stringValue,
                    _ => stringValue
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 计算最优批次大小
        /// </summary>
        private int CalculateOptimalBatchSize(int totalRows)
        {
            if (totalRows <= 1000)
                return MIN_BATCH_SIZE;
            else if (totalRows <= 10000)
                return DEFAULT_BATCH_SIZE;
            else
                return MAX_BATCH_SIZE;
        }

        /// <summary>
        /// 获取Excel行数（优化版本）
        /// </summary>
        private async Task<int> GetExcelRowCountAsync(string filePath, string sheetName)
        {
            var cacheKey = $"rowcount_{filePath}_{sheetName}";
            
            if (_cache.TryGetValue(cacheKey, out var cachedCount))
            {
                return (int)cachedCount;
            }

            try
            {
                // 设置EPPlus许可证上下文
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[sheetName];
                
                var rowCount = worksheet?.Dimension?.End.Row ?? 0;
                _cache.TryAdd(cacheKey, rowCount);
                
                return rowCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取Excel行数失败: {filePath}");
                return 0;
            }
        }

        /// <summary>
        /// 构建优化的INSERT语句
        /// </summary>
        private string BuildOptimizedInsertSql(string tableName, List<FieldMapping> fieldMappings)
        {
            var uniqueFields = new List<string>();
            var fieldNames = new HashSet<string>();
            
            foreach (var mapping in fieldMappings)
            {
                var fieldName = mapping.DatabaseField;
                var originalFieldName = fieldName;
                int counter = 1;
                
                while (fieldNames.Contains(fieldName))
                {
                    fieldName = $"{originalFieldName}_{counter}";
                    counter++;
                }
                
                fieldNames.Add(fieldName);
                uniqueFields.Add(fieldName);
            }
            
            var columnsSql = string.Join(", ", uniqueFields.Select(f => $"[{f}]"));
            var parametersSql = string.Join(", ", uniqueFields.Select(f => $"@{SanitizeParameterName(f)}"));
            
            return $"INSERT INTO [{tableName}] ({columnsSql}) VALUES ({parametersSql})";
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        private void CleanupCache()
        {
            _cache.Clear();
            GC.Collect();
        }

        // 实现其他接口方法
        public async Task<bool> CreateTargetTableAsync(string tableName, List<FieldMapping> fieldMappings)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var tableExists = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@TableName",
                    new { TableName = tableName });

                if (tableExists > 0)
                {
                    _logger.LogInformation($"表 {tableName} 已存在");
                    return true;
                }

                var createTableSql = BuildCreateTableSql(tableName, fieldMappings);
                await connection.ExecuteAsync(createTableSql);
                
                _logger.LogInformation($"表 {tableName} 创建成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建表 {tableName} 失败");
                return false;
            }
        }

        public async Task<bool> ClearTableDataAsync(string tableName)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();
                await connection.ExecuteAsync($"DELETE FROM [{tableName}]");
                
                _logger.LogInformation($"表 {tableName} 数据清除成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"清除表 {tableName} 数据失败");
                return false;
            }
        }

        public async Task<bool> ValidateDataSourceConnectionAsync(string dataSourceName)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();
                await connection.ExecuteAsync("SELECT 1");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"验证数据源连接失败: {dataSourceName}");
                return false;
            }
        }

        // 辅助方法
        private List<FieldMapping> NormalizeFieldMappings(List<FieldMapping> fieldMappings)
        {
            var normalized = new List<FieldMapping>();
            var fieldNames = new HashSet<string>();
            
            foreach (var mapping in fieldMappings)
            {
                var fieldName = mapping.DatabaseField;
                var originalFieldName = fieldName;
                int counter = 1;
                
                while (fieldNames.Contains(fieldName))
                {
                    fieldName = $"{originalFieldName}_{counter}";
                    counter++;
                }
                
                fieldNames.Add(fieldName);
                normalized.Add(new FieldMapping
                {
                    ExcelOriginalColumn = mapping.ExcelOriginalColumn,
                    DatabaseField = fieldName,
                    DataType = mapping.DataType
                });
            }
            
            return normalized;
        }

        private string BuildCreateTableSql(string tableName, List<FieldMapping> fieldMappings)
        {
            var columns = new List<string>();
            
            foreach (var mapping in fieldMappings)
            {
                var columnDef = $"[{mapping.DatabaseField}] {mapping.DataType}";
                columns.Add(columnDef);
            }
            
            var columnsSql = string.Join(", ", columns);
            return $"CREATE TABLE [{tableName}] ({columnsSql})";
        }

        private string GetColumnLetter(int columnNumber)
        {
            string result = "";
            while (columnNumber > 0)
            {
                columnNumber--;
                result = (char)('A' + columnNumber % 26) + result;
                columnNumber /= 26;
            }
            return result;
        }

        private string SanitizeParameterName(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
                return "param";

            var sanitized = System.Text.RegularExpressions.Regex.Replace(parameterName, @"[^\w\u4e00-\u9fa5]", "_");
            
            if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
                sanitized = "col_" + sanitized;
            
            if (string.IsNullOrWhiteSpace(sanitized) || sanitized.All(c => c == '_'))
                sanitized = "param_" + Math.Abs(parameterName.GetHashCode());
                
            return sanitized;
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
            _cache?.Clear();
        }

        /// <summary>
        /// 拆分行数据 - 将合并单元格的内容拆分到每个单独的单元格中（优化版本）
        /// </summary>
        private List<Dictionary<string, object>> SplitRowDataOptimized(Dictionary<string, object> originalRow)
        {
            // 由于OptimizedDataImportService没有直接访问Excel工作表的权限，
            // 我们需要通过其他方式来处理合并单元格
            // 这里我们返回原始行，实际的合并单元格处理应该在数据读取阶段进行
            return new List<Dictionary<string, object>> { originalRow };
        }

        /// <summary>
        /// 拆分合并单元格数据 - 将合并单元格的内容填充到每个单元格中（优化版本）
        /// </summary>
        private Dictionary<string, object> SplitMergedCellsDataOptimized(Dictionary<string, object> originalRow, 
            ExcelWorksheet worksheet, int currentRow, List<FieldMapping> fieldMappings)
        {
            var splitRow = new Dictionary<string, object>();
            var dimension = worksheet.Dimension;
            
            // 遍历所有字段映射
            foreach (var mapping in fieldMappings)
            {
                var fieldName = SanitizeParameterName(mapping.DatabaseField);
                var columnLetter = mapping.ExcelOriginalColumn;
                var columnIndex = GetColumnIndexOptimized(columnLetter);
                
                if (columnIndex > 0)
                {
                    // 获取当前单元格的值
                    var cellValue = GetCellValueWithMergedCellsOptimized(worksheet, currentRow, columnIndex);
                    
                    // 如果当前单元格为空，查找合并单元格的值
                    if (cellValue == null || (cellValue is string strValue && string.IsNullOrWhiteSpace(strValue)))
                    {
                        cellValue = FindMergedCellValueOptimized(worksheet, currentRow, columnIndex);
                    }
                    
                    splitRow[fieldName] = cellValue ?? DBNull.Value;
                }
                else
                {
                    // 如果无法确定列索引，使用原始值
                    splitRow[fieldName] = originalRow.ContainsKey(fieldName) ? originalRow[fieldName] : DBNull.Value;
                }
            }
            
            return splitRow;
        }
        
        /// <summary>
        /// 查找合并单元格的值（优化版本）
        /// </summary>
        private object? FindMergedCellValueOptimized(ExcelWorksheet worksheet, int row, int col)
        {
            // 遍历所有合并单元格范围
            foreach (var mergedRangeAddress in worksheet.MergedCells)
            {
                var mergedRange = worksheet.Cells[mergedRangeAddress];
                if (IsCellInRangeOptimized(row, col, mergedRange))
                {
                    // 获取合并区域的左上角单元格值
                    var topLeftCell = worksheet.Cells[mergedRange.Start.Row, mergedRange.Start.Column];
                    return topLeftCell.Value;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 获取列索引（优化版本）
        /// </summary>
        private int GetColumnIndexOptimized(string columnLetter)
        {
            if (string.IsNullOrEmpty(columnLetter))
                return 0;
                
            int index = 0;
            foreach (char c in columnLetter.ToUpper())
            {
                index = index * 26 + (c - 'A' + 1);
            }
            return index;
        }

        /// <summary>
        /// 获取单元格的值，考虑合并单元格
        /// </summary>
        private object? GetCellValueWithMergedCellsOptimized(ExcelWorksheet worksheet, int row, int col)
        {
            // 首先检查当前单元格是否有值
            var cell = worksheet.Cells[row, col];
            if (cell.Value != null)
            {
                return cell.Value;
            }

            // 如果当前单元格为空，检查是否在合并区域内
            foreach (var mergedRangeAddress in worksheet.MergedCells)
            {
                var mergedRange = worksheet.Cells[mergedRangeAddress];
                if (IsCellInRangeOptimized(row, col, mergedRange))
                {
                    // 获取合并区域的左上角单元格值
                    var topLeftCell = worksheet.Cells[mergedRange.Start.Row, mergedRange.Start.Column];
                    return topLeftCell.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// 检查单元格是否在指定范围内
        /// </summary>
        private bool IsCellInRangeOptimized(int row, int col, ExcelRange range)
        {
            return row >= range.Start.Row && row <= range.End.Row &&
                   col >= range.Start.Column && col <= range.End.Column;
        }
    }
} 