using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelProcessor.Models;
using ExcelProcessor.Core.Interfaces;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 数据导入服务接口
    /// </summary>
    public interface IDataImportService
    {
        /// <summary>
        /// 导入Excel数据到数据库
        /// </summary>
        /// <param name="config">Excel配置</param>
        /// <param name="fieldMappings">字段映射</param>
        /// <param name="targetTableName">目标表名</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns>导入结果</returns>
        Task<DataImportResult> ImportExcelDataAsync(ExcelConfig config, List<FieldMapping> fieldMappings, string? targetTableName = null, IImportProgressCallback? progressCallback = null);

        /// <summary>
        /// 创建目标表（如果不存在）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fieldMappings">字段映射</param>
        /// <returns>是否成功</returns>
        Task<bool> CreateTargetTableAsync(string tableName, List<FieldMapping> fieldMappings);

        /// <summary>
        /// 清除表数据
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>是否成功</returns>
        Task<bool> ClearTableDataAsync(string tableName);

        /// <summary>
        /// 验证数据源连接
        /// </summary>
        /// <param name="dataSourceName">数据源名称</param>
        /// <returns>是否连接成功</returns>
        Task<bool> ValidateDataSourceConnectionAsync(string dataSourceName);
    }

    /// <summary>
    /// 数据导入结果
    /// </summary>
    public class DataImportResult
    {
        public bool IsSuccess { get; set; }
        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }
        public int FailedRows { get; set; }
        public int SkippedRows { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public string TargetTableName { get; set; } = string.Empty;
        public System.TimeSpan Duration { get; set; }
    }
} 