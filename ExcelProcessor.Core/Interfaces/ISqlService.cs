using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelProcessor.Models;
using ExcelProcessor.Core.Models;

namespace ExcelProcessor.Core.Interfaces
{
    /// <summary>
    /// SQL服务接口
    /// </summary>
    public interface ISqlService
    {
        /// <summary>
        /// 获取所有SQL配置
        /// </summary>
        /// <returns>SQL配置列表</returns>
        Task<List<SqlConfig>> GetAllSqlConfigsAsync();

        /// <summary>
        /// 根据ID获取SQL配置
        /// </summary>
        /// <param name="id">SQL配置ID</param>
        /// <returns>SQL配置</returns>
        Task<SqlConfig?> GetSqlConfigByIdAsync(string id);

        /// <summary>
        /// 根据分类获取SQL配置
        /// </summary>
        /// <param name="category">分类名称</param>
        /// <returns>SQL配置列表</returns>
        Task<List<SqlConfig>> GetSqlConfigsByCategoryAsync(string category);

        /// <summary>
        /// 搜索SQL配置
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        /// <returns>匹配的SQL配置列表</returns>
        Task<List<SqlConfig>> SearchSqlConfigsAsync(string searchText);

        /// <summary>
        /// 创建SQL配置
        /// </summary>
        /// <param name="sqlConfig">SQL配置</param>
        /// <param name="userId">创建用户ID</param>
        /// <returns>创建的SQL配置</returns>
        Task<SqlConfig> CreateSqlConfigAsync(SqlConfig sqlConfig, string? userId = null);

        /// <summary>
        /// 更新SQL配置
        /// </summary>
        /// <param name="sqlConfig">SQL配置</param>
        /// <param name="userId">修改用户ID</param>
        /// <returns>更新后的SQL配置</returns>
        Task<SqlConfig> UpdateSqlConfigAsync(SqlConfig sqlConfig, string? userId = null);

        /// <summary>
        /// 删除SQL配置
        /// </summary>
        /// <param name="id">SQL配置ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteSqlConfigAsync(string id);

        /// <summary>
        /// 测试SQL语句
        /// </summary>
        /// <param name="sqlStatement">SQL语句</param>
        /// <param name="dataSourceId">数据源ID</param>
        /// <param name="parameters">参数</param>
        /// <returns>测试结果</returns>
        Task<SqlTestResult> TestSqlStatementAsync(string sqlStatement, string? dataSourceId = null, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// 获取SQL执行历史
        /// </summary>
        /// <param name="sqlConfigId">SQL配置ID</param>
        /// <param name="limit">限制数量</param>
        /// <returns>执行历史列表</returns>
        Task<List<SqlExecutionResult>> GetSqlExecutionHistoryAsync(string sqlConfigId, int limit = 50);

        /// <summary>
        /// 获取所有SQL分类
        /// </summary>
        /// <returns>分类列表</returns>
        Task<List<string>> GetAllCategoriesAsync();

        /// <summary>
        /// 验证SQL配置
        /// </summary>
        /// <param name="sqlConfig">SQL配置</param>
        /// <returns>验证结果</returns>
        Task<ValidationResult> ValidateSqlConfigAsync(SqlConfig sqlConfig);

        /// <summary>
        /// 执行SQL并输出到数据表（支持参数）
        /// </summary>
        /// <param name="sqlStatement">SQL语句</param>
        /// <param name="queryDataSourceId">查询数据源ID</param>
        /// <param name="targetDataSourceId">目标数据源ID</param>
        /// <param name="targetTableName">目标表名</param>
        /// <param name="clearTableBeforeInsert">插入前是否清空表</param>
        /// <param name="parameters">查询参数</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns>执行结果</returns>
        Task<SqlOutputResult> ExecuteSqlToTableAsync(string sqlStatement, string? queryDataSourceId, string? targetDataSourceId, string targetTableName, bool clearTableBeforeInsert = false, Dictionary<string, object>? parameters = null, ISqlProgressCallback? progressCallback = null);

        /// <summary>
        /// 执行SQL并输出到Excel（支持参数）
        /// </summary>
        /// <param name="sqlStatement">SQL语句</param>
        /// <param name="queryDataSourceId">查询数据源ID</param>
        /// <param name="outputPath">输出路径</param>
        /// <param name="sheetName">Sheet名称</param>
        /// <param name="clearSheetBeforeOutput">输出前是否清空Sheet页</param>
        /// <param name="parameters">查询参数</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns>执行结果</returns>
        Task<SqlOutputResult> ExecuteSqlToExcelAsync(string sqlStatement, string? queryDataSourceId, string outputPath, string sheetName, bool clearSheetBeforeOutput = false, Dictionary<string, object>? parameters = null, ISqlProgressCallback? progressCallback = null);

        /// <summary>
        /// 执行SQL查询（公用方法）
        /// </summary>
        /// <param name="sqlStatement">SQL语句</param>
        /// <param name="dataSourceId">数据源ID</param>
        /// <param name="parameters">查询参数</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns>查询结果</returns>
        Task<SqlQueryResult> ExecuteSqlQueryAsync(string sqlStatement, string? dataSourceId = null, Dictionary<string, object>? parameters = null, ISqlProgressCallback? progressCallback = null);

        /// <summary>
        /// 执行已保存的SQL配置
        /// </summary>
        /// <param name="sqlConfigId">SQL配置ID</param>
        /// <param name="parameters">执行参数</param>
        /// <param name="userId">执行用户ID</param>
        /// <returns>执行结果</returns>
        Task<SqlExecutionResult> ExecuteSqlConfigAsync(string sqlConfigId, Dictionary<string, object>? parameters = null, string? userId = null);

        /// <summary>
        /// 根据数据源ID获取数据源配置
        /// </summary>
        /// <param name="dataSourceId">数据源ID</param>
        /// <returns>数据源配置</returns>
        Task<DataSourceConfig?> GetDataSourceByIdAsync(string dataSourceId);

        /// <summary>
        /// 执行SQL语句
        /// </summary>
        /// <param name="sqlStatement">SQL语句</param>
        /// <param name="connectionString">连接字符串</param>
        /// <returns>执行结果</returns>
        Task<SqlExecutionResult> ExecuteSqlAsync(string sqlStatement, string connectionString);
    }

    /// <summary>
    /// SQL测试结果
    /// </summary>
    public class SqlTestResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 预计执行时间（毫秒）
        /// </summary>
        public long EstimatedDurationMs { get; set; }

        /// <summary>
        /// 预计返回行数
        /// </summary>
        public int EstimatedRowCount { get; set; }

        /// <summary>
        /// 列信息
        /// </summary>
        public List<SqlColumnInfo>? Columns { get; set; }

        /// <summary>
        /// 测试数据（前5行）
        /// </summary>
        public List<Dictionary<string, object>>? SampleData { get; set; }
    }

    /// <summary>
    /// SQL列信息
    /// </summary>
    public class SqlColumnInfo
    {
        /// <summary>
        /// 列名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 数据类型
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// 是否可为空
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// 最大长度
        /// </summary>
        public int? MaxLength { get; set; }
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误信息列表
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// SQL输出结果
    /// </summary>
    public class SqlOutputResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 影响的行数
        /// </summary>
        public int AffectedRows { get; set; }

        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// 输出路径
        /// </summary>
        public string? OutputPath { get; set; }
    }
} 
 