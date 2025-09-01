using System.Collections.Generic;
using ExcelProcessor.Core.Interfaces;

namespace ExcelProcessor.Core.Models
{
    /// <summary>
    /// SQL查询执行结果
    /// </summary>
    public class SqlQueryResult
    {
        /// <summary>
        /// 是否执行成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 列信息
        /// </summary>
        public List<SqlColumnInfo> Columns { get; set; } = new List<SqlColumnInfo>();

        /// <summary>
        /// 查询结果数据
        /// </summary>
        public List<Dictionary<string, object>> Data { get; set; } = new List<Dictionary<string, object>>();

        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// 影响的行数
        /// </summary>
        public int AffectedRows { get; set; }

        /// <summary>
        /// 数据行数
        /// </summary>
        public int RowCount => Data?.Count ?? 0;

        /// <summary>
        /// 列数
        /// </summary>
        public int ColumnCount => Columns?.Count ?? 0;
    }
} 