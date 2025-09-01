using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Core.Interfaces;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.Text.Json;
using System.Data.Common;

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// 数据导入服务
    /// </summary>
    public class DataImportService : IDataImportService
    {
        private readonly ILogger<DataImportService> _logger;
        private readonly string _connectionString;
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ISqlDialect _sqlDialect;

        public DataImportService(ILogger<DataImportService> logger, string connectionString)
        {
            _logger = logger;
            _connectionString = connectionString;
            _connectionFactory = new ExcelProcessor.Data.Infrastructure.DefaultDbConnectionFactory(connectionString);
            _sqlDialect = new ExcelProcessor.Data.Infrastructure.SqliteDialect();
        }

        public DataImportService(ILogger<DataImportService> logger, IDbConnectionFactory connectionFactory, ISqlDialect sqlDialect)
        {
            _logger = logger;
            _connectionString = string.Empty;
            _connectionFactory = connectionFactory;
            _sqlDialect = sqlDialect;
        }

        /// <summary>
        /// 导入Excel数据到数据库
        /// </summary>
        public async Task<DataImportResult> ImportExcelDataAsync(ExcelConfig config, 
            List<FieldMapping> fieldMappings, string? targetTableName = null, IImportProgressCallback? progressCallback = null)
        {
            var result = new DataImportResult { TargetTableName = targetTableName ?? config.TargetTableName };
            var startTime = DateTime.Now;

            try
            {
                // 规范化字段映射，处理重复字段名
                var normalizedFieldMappings = NormalizeFieldMappings(fieldMappings);
                
                if (!File.Exists(config.FilePath))
                {
                    result.ErrorMessage = $"Excel文件不存在: {config.FilePath}";
                    return result;
                }

                _logger.LogInformation($"开始导入Excel数据: {config.ConfigName}");

                // 更新进度：开始读取Excel文件
                progressCallback?.SetStatus("正在读取Excel文件...");
                progressCallback?.UpdateProgress(5, "正在读取Excel文件...");

                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // 创建目标表
                progressCallback?.SetStatus("正在创建目标表...");
                progressCallback?.UpdateProgress(10, "正在创建目标表...");
                
                if (!await CreateTargetTableAsync(result.TargetTableName ?? "ImportedData", normalizedFieldMappings))
                {
                    result.ErrorMessage = "创建目标表失败";
                    return result;
                }

                // 如果配置了清除表数据，先清空目标表
                if (config.ClearTableDataBeforeImport)
                {
                    progressCallback?.SetStatus("正在清除表数据...");
                    progressCallback?.UpdateProgress(12, "正在清除表数据...");
                    
                    if (!await ClearTableDataAsync(result.TargetTableName ?? "ImportedData"))
                    {
                        result.ErrorMessage = "清除表数据失败";
                        return result;
                    }
                    
                    _logger.LogInformation($"已清除表 {result.TargetTableName} 的所有数据");
                }

                // 设置EPPlus许可证上下文
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using var package = new ExcelPackage(new FileInfo(config.FilePath));
                var worksheet = package.Workbook.Worksheets[0];
                var dimension = worksheet.Dimension;

                if (dimension == null)
                {
                    result.ErrorMessage = "Excel文件为空或无法读取";
                    return result;
                }

                // 数据开始行
                int dataStartRow = config.HeaderRow + 1;
                int totalRows = dimension.End.Row - dataStartRow + 1;
                
                // 更新总行数
                progressCallback?.UpdateStatistics(totalRows, 0, 0, 0);
                progressCallback?.SetStatus($"准备导入 {totalRows} 行数据...");

                // 开始事务
                using var transaction = connection.BeginTransaction();
                try
                {
                    // 构建INSERT语句
                    var insertSql = BuildInsertSql(result.TargetTableName ?? "ImportedData", normalizedFieldMappings);
                    _logger.LogInformation($"INSERT SQL: {insertSql}");

                    // 批量处理数据
                    const int batchSize = 100;
                    var batchData = new List<object>();
                    int currentBatch = 0;
                    int totalBatches = (int)Math.Ceiling((double)totalRows / batchSize);

                    for (int row = dataStartRow; row <= dimension.End.Row; row++)
                    {
                        try
                        {
                            var rowData = new Dictionary<string, object>();
                            bool hasData = false;

                            // 读取行数据
                            for (int col = dimension.Start.Column; col <= dimension.End.Column; col++)
                            {
                                var cellValue = GetCellValueWithMergedCells(worksheet, row, col);
                                if (cellValue != null)
                                {
                                    hasData = true;
                                }

                                // 根据字段映射获取对应的数据库字段
                                var mapping = normalizedFieldMappings.FirstOrDefault(m => 
                                    m.ExcelOriginalColumn == GetColumnLetter(col));

                                if (mapping != null)
                                {
                                    var dbValue = ConvertCellValue(cellValue, mapping.DataType);
                                    // 使用 null 而不是 DBNull.Value，Dapper 会自动处理 null 值
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
                                var splitRow = SplitMergedCellsData(rowData, worksheet, row, normalizedFieldMappings);
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
                                batchData.Add(processedRow);

                                // 达到批次大小时执行插入
                                if (batchData.Count >= batchSize)
                                {
                                    currentBatch++;
                                    await ExecuteBatchInsert(connection, insertSql, batchData);
                                    result.SuccessRows += batchData.Count;
                                    batchData.Clear();

                                    // 更新进度
                                    int processedRows = row - dataStartRow + 1;
                                    double progress = Math.Min(90, 15 + (processedRows * 75.0 / totalRows));
                                    progressCallback?.UpdateProgress(progress, $"正在导入第 {row} 行...");
                                    progressCallback?.UpdateStatistics(totalRows, processedRows, result.SuccessRows, result.FailedRows);
                                    progressCallback?.UpdateCurrentRow(row, dimension.End.Row);
                                    progressCallback?.UpdateBatchInfo(currentBatch, batchSize, totalBatches);
                                    progressCallback?.SetStatus($"已导入 {result.SuccessRows} 行，当前处理第 {row} 行");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"处理第 {row} 行数据时出错");
                            result.FailedRows++;
                            
                            // 更新失败统计
                            int processedRows = row - dataStartRow + 1;
                            progressCallback?.UpdateStatistics(totalRows, processedRows, result.SuccessRows, result.FailedRows);
                        }
                    }

                    // 处理剩余的批次数据
                    if (batchData.Count > 0)
                    {
                        currentBatch++;
                        await ExecuteBatchInsert(connection, insertSql, batchData);
                        result.SuccessRows += batchData.Count;
                        
                        // 更新最终进度
                        progressCallback?.UpdateProgress(95, "正在完成最后一批数据...");
                        progressCallback?.UpdateStatistics(totalRows, totalRows, result.SuccessRows, result.FailedRows);
                        progressCallback?.UpdateBatchInfo(currentBatch, batchData.Count, totalBatches);
                    }

                    // 提交事务
                    progressCallback?.SetStatus("正在提交事务...");
                    progressCallback?.UpdateProgress(98, "正在提交事务...");
                    transaction.Commit();
                    
                    result.IsSuccess = true;
                    result.TotalRows = result.SuccessRows + result.FailedRows;
                    result.Duration = DateTime.Now - startTime;

                    // 完成导入
                    progressCallback?.UpdateProgress(100, "导入完成");
                    progressCallback?.SetStatus($"导入完成！成功 {result.SuccessRows} 行，失败 {result.FailedRows} 行");

                    _logger.LogInformation($"Excel数据导入完成: 成功 {result.SuccessRows} 行，失败 {result.FailedRows} 行");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "导入过程中发生错误，已回滚事务");
                    result.ErrorMessage = ex.Message;
                    progressCallback?.SetStatus($"导入失败：{ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel数据导入失败");
                result.ErrorMessage = ex.Message;
                progressCallback?.SetStatus($"导入失败：{ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 创建目标表（如果不存在）
        /// </summary>
        public async Task<bool> CreateTargetTableAsync(string tableName, List<FieldMapping> fieldMappings)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // 检查表是否已存在
                var tableExists = await connection.QueryFirstOrDefaultAsync<int>(
                    _sqlDialect.GetExistsTableSql(tableName),
                    new { TableName = tableName });

                if (tableExists > 0)
                {
                    _logger.LogInformation($"表 {tableName} 已存在");
                    return true;
                }

                // 构建CREATE TABLE语句（使用方言）
                var createTableSql = BuildCreateTableSql(tableName, fieldMappings);
                _logger.LogInformation($"CREATE TABLE SQL: {createTableSql}");

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

        /// <summary>
        /// 清除表数据
        /// </summary>
        public async Task<bool> ClearTableDataAsync(string tableName)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // 检查表是否存在
                var tableExists = await connection.QueryFirstOrDefaultAsync<int>(
                    _sqlDialect.GetExistsTableSql(tableName),
                    new { TableName = tableName });

                if (tableExists == 0)
                {
                    _logger.LogWarning($"表 {tableName} 不存在，无需清除数据");
                    return true;
                }

                // 清除表数据
                var clearSql = _sqlDialect.BuildTruncateOrDeleteAll(tableName);
                await connection.ExecuteAsync(clearSql);
                
                _logger.LogInformation($"表 {tableName} 数据清除成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"清除表 {tableName} 数据失败");
                return false;
            }
        }

        /// <summary>
        /// 验证数据源连接
        /// </summary>
        public async Task<bool> ValidateDataSourceConnectionAsync(string dataSourceName)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                
                // 执行简单查询测试连接
                await connection.QueryFirstOrDefaultAsync<int>("SELECT 1");
                
                _logger.LogInformation($"数据源 {dataSourceName} 连接验证成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"数据源 {dataSourceName} 连接验证失败");
                return false;
            }
        }

        #region 私有方法

        /// <summary>
        /// 规范化字段映射，处理重复字段名
        /// </summary>
        private List<FieldMapping> NormalizeFieldMappings(List<FieldMapping> fieldMappings)
        {
            var result = new List<FieldMapping>();
            var fieldNames = new HashSet<string>();
            
            foreach (var mapping in fieldMappings)
            {
                var fieldName = mapping.DatabaseField;
                var originalFieldName = fieldName;
                int counter = 1;
                
                // 如果字段名重复，添加数字后缀
                while (fieldNames.Contains(fieldName))
                {
                    fieldName = $"{originalFieldName}_{counter}";
                    counter++;
                }
                
                fieldNames.Add(fieldName);
                
                // 创建新的映射对象
                var newMapping = new FieldMapping
                {
                    ExcelOriginalColumn = mapping.ExcelOriginalColumn,
                    ExcelColumn = mapping.ExcelColumn,
                    DatabaseField = fieldName,
                    DataType = mapping.DataType,
                    IsRequired = mapping.IsRequired
                };
                
                result.Add(newMapping);
            }
            
            return result;
        }

        /// <summary>
        /// 构建CREATE TABLE语句
        /// </summary>
        private string BuildCreateTableSql(string tableName, List<FieldMapping> fieldMappings)
        {
            // 处理重复字段名，生成列名->中立类型的映射
            var uniqueFields = new Dictionary<string, string>();
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
                uniqueFields[fieldName] = mapping.DataType;
            }
            return _sqlDialect.BuildCreateTable(tableName, uniqueFields);
        }

        /// <summary>
        /// 构建INSERT语句
        /// </summary>
        private string BuildInsertSql(string tableName, List<FieldMapping> fieldMappings)
        {
            // 处理重复字段名，保证列顺序与映射一致
            var columnNames = new List<string>();
            var parameterNames = new List<string>();
            var existing = new HashSet<string>();
            foreach (var mapping in fieldMappings)
            {
                var name = mapping.DatabaseField;
                var baseName = name;
                int i = 1;
                while (existing.Contains(name))
                {
                    name = $"{baseName}_{i}";
                    i++;
                }
                existing.Add(name);
                columnNames.Add(name);
                parameterNames.Add(SanitizeParameterName(name));
            }
            var cols = string.Join(", ", columnNames.Select(n => _sqlDialect.QuoteIdentifier(n)));
            var pars = string.Join(", ", parameterNames.Select(n => _sqlDialect.Parameterize(n)));
            return $"INSERT INTO {_sqlDialect.QuoteIdentifier(tableName)} ({cols}) VALUES ({pars})";
        }

        /// <summary>
        /// 执行批量插入
        /// </summary>
        private async Task ExecuteBatchInsert(DbConnection connection, string insertSql, List<object> batchData)
        {
            foreach (var rowData in batchData)
            {
                try
                {
                    // 将rowData转换为Dictionary<string, object>
                    if (rowData is Dictionary<string, object> dict)
                    {
                        // 检查并清理DBNull值
                        var cleanedRowData = new Dictionary<string, object>();
                        foreach (var kvp in dict)
                        {
                            if (kvp.Value == DBNull.Value)
                            {
                                cleanedRowData[kvp.Key] = null;
                                _logger.LogWarning($"发现DBNull值，字段: {kvp.Key}，已转换为null");
                            }
                            else
                            {
                                cleanedRowData[kvp.Key] = kvp.Value;
                            }
                        }
                        
                        await connection.ExecuteAsync(insertSql, cleanedRowData);
                    }
                    else
                    {
                        // 如果不是Dictionary类型，直接执行
                        await connection.ExecuteAsync(insertSql, rowData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"执行SQL时出错: {insertSql}");
                    if (rowData is Dictionary<string, object> dict)
                    {
                        _logger.LogError($"参数数据: {string.Join(", ", dict.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
                    }
                    throw;
                }
            }
        }

        /// <summary>
        /// 获取列字母
        /// </summary>
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

        /// <summary>
        /// 转换单元格值
        /// </summary>
        private object? ConvertCellValue(object? cellValue, string dataType)
        {
            if (cellValue == null || cellValue == DBNull.Value)
                return null; // 返回 null 而不是 DBNull.Value

            try
            {
                string stringValue = cellValue.ToString() ?? "";

                // 如果字符串为空，返回 null
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
                return null; // 转换失败时返回 null
            }
        }

        /// <summary>
        /// 清理参数名称，移除特殊字符
        /// </summary>
        private string SanitizeParameterName(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
                return "param";

            // 移除特殊字符，只保留字母、数字、下划线和中文
            var sanitized = System.Text.RegularExpressions.Regex.Replace(parameterName, @"[^\w\u4e00-\u9fa5]", "_");
            
            // 如果以数字开头，添加前缀
            if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
                sanitized = "col_" + sanitized;
            
            // 如果为空或只包含下划线，使用默认名称
            if (string.IsNullOrWhiteSpace(sanitized) || sanitized.All(c => c == '_'))
                sanitized = "param_" + Math.Abs(parameterName.GetHashCode());
                
            return sanitized;
        }

        /// <summary>
        /// 拆分合并单元格数据 - 将合并单元格的内容填充到每个单元格中
        /// </summary>
        private Dictionary<string, object> SplitMergedCellsData(Dictionary<string, object> originalRow, 
            ExcelWorksheet worksheet, int currentRow, List<FieldMapping> fieldMappings)
        {
            var splitRow = new Dictionary<string, object>();
            var dimension = worksheet.Dimension;
            
            // 遍历所有字段映射
            foreach (var mapping in fieldMappings)
            {
                var fieldName = SanitizeParameterName(mapping.DatabaseField);
                var columnLetter = mapping.ExcelOriginalColumn;
                var columnIndex = GetColumnIndex(columnLetter);
                
                if (columnIndex > 0)
                {
                    // 获取当前单元格的值
                    var cellValue = GetCellValueWithMergedCells(worksheet, currentRow, columnIndex);
                    
                    // 如果当前单元格为空，查找合并单元格的值
                    if (cellValue == null || (cellValue is string strValue && string.IsNullOrWhiteSpace(strValue)))
                    {
                        cellValue = FindMergedCellValue(worksheet, currentRow, columnIndex);
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
        /// 查找合并单元格的值
        /// </summary>
        private object? FindMergedCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            // 遍历所有合并单元格范围
            foreach (var mergedRangeAddress in worksheet.MergedCells)
            {
                var mergedRange = worksheet.Cells[mergedRangeAddress];
                if (IsCellInRange(row, col, mergedRange))
                {
                    // 获取合并区域的左上角单元格值
                    var topLeftCell = worksheet.Cells[mergedRange.Start.Row, mergedRange.Start.Column];
                    return topLeftCell.Value;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 获取列索引
        /// </summary>
        private int GetColumnIndex(string columnLetter)
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
        /// 获取单元格的值，包括合并单元格
        /// </summary>
        private object? GetCellValueWithMergedCells(ExcelWorksheet worksheet, int row, int col)
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
                if (IsCellInRange(row, col, mergedRange))
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
        private bool IsCellInRange(int row, int col, ExcelRange range)
        {
            return row >= range.Start.Row && row <= range.End.Row &&
                   col >= range.Start.Column && col <= range.End.Column;
        }

        #endregion
    }
} 