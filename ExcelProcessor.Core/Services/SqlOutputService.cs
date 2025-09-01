using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ExcelProcessor.Core.Interfaces;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// SQL输出服务实现，面向应用内复用（到表/到Excel）。
    /// </summary>
    public class SqlOutputService : ISqlOutputService
    {
        private readonly ISqlService _sqlService;
        private readonly ILogger<SqlOutputService> _logger;

        public SqlOutputService(ISqlService sqlService, ILogger<SqlOutputService> logger)
        {
            _sqlService = sqlService;
            _logger = logger;
        }

        public async Task<SqlOutputResult> OutputToTableAsync(
            string sqlStatement,
            string? queryDataSourceId,
            string? targetDataSourceId,
            string targetTableName,
            bool clearTableBeforeInsert = false,
            Dictionary<string, object>? parameters = null,
            ISqlProgressCallback? progressCallback = null)
        {
            if (string.IsNullOrWhiteSpace(sqlStatement))
                throw new ArgumentException("SQL语句不能为空", nameof(sqlStatement));
            if (string.IsNullOrWhiteSpace(targetTableName))
                throw new ArgumentException("目标表名不能为空", nameof(targetTableName));

            _logger.LogInformation("开始执行SQL输出到数据表: {Table}", targetTableName);
            var result = await _sqlService.ExecuteSqlToTableAsync(sqlStatement, queryDataSourceId, targetDataSourceId, targetTableName, clearTableBeforeInsert, parameters, progressCallback);
            _logger.LogInformation("完成SQL输出到数据表: {Table}, 成功: {Success}, 行数: {Rows}", targetTableName, result.IsSuccess, result.AffectedRows);
            return result;
        }

        public async Task<SqlOutputResult> OutputToExcelAsync(
            string sqlStatement,
            string? queryDataSourceId,
            string outputPath,
            string sheetName,
            bool clearSheetBeforeOutput = false,
            Dictionary<string, object>? parameters = null,
            ISqlProgressCallback? progressCallback = null)
        {
            if (string.IsNullOrWhiteSpace(sqlStatement))
                throw new ArgumentException("SQL语句不能为空", nameof(sqlStatement));
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("输出路径不能为空", nameof(outputPath));
            if (string.IsNullOrWhiteSpace(sheetName))
                throw new ArgumentException("Sheet名称不能为空", nameof(sheetName));

            // 确保目录存在
            try
            {
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "创建输出目录失败: {OutputPath}", outputPath);
            }

            _logger.LogInformation("开始执行SQL输出到Excel: {OutputPath}, Sheet: {Sheet}", outputPath, sheetName);
            var result = await _sqlService.ExecuteSqlToExcelAsync(sqlStatement, queryDataSourceId, outputPath, sheetName, clearSheetBeforeOutput, parameters, progressCallback);
            _logger.LogInformation("完成SQL输出到Excel: {OutputPath}, 成功: {Success}, 行数: {Rows}", outputPath, result.IsSuccess, result.AffectedRows);
            return result;
        }
    }
} 