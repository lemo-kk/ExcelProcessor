using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelProcessor.Models;

namespace ExcelProcessor.Core.Interfaces
{
    /// <summary>
    /// 通用的SQL输出服务，封装SQL执行并输出到数据表或Excel。
    /// </summary>
    public interface ISqlOutputService
    {
        /// <summary>
        /// 执行SQL并输出到指定数据表。
        /// </summary>
        Task<SqlOutputResult> OutputToTableAsync(
            string sqlStatement,
            string? queryDataSourceId,
            string? targetDataSourceId,
            string targetTableName,
            bool clearTableBeforeInsert = false,
            Dictionary<string, object>? parameters = null,
            ISqlProgressCallback? progressCallback = null);

        /// <summary>
        /// 执行SQL并输出到指定Excel工作表。
        /// </summary>
        Task<SqlOutputResult> OutputToExcelAsync(
            string sqlStatement,
            string? queryDataSourceId,
            string outputPath,
            string sheetName,
            bool clearSheetBeforeOutput = false,
            Dictionary<string, object>? parameters = null,
            ISqlProgressCallback? progressCallback = null);
    }
} 