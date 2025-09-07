using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ExcelProcessor.Core.Interfaces;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Models;
using ExcelProcessor.Core.Models;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Microsoft.Data.SqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// SQL服务实现
    /// </summary>
    public class SqlService : ISqlService
    {
        private readonly ILogger<SqlService> _logger;
        private readonly string _connectionString;
        private readonly IDataSourceService _dataSourceService;

        public SqlService(ILogger<SqlService> logger, string connectionString, IDataSourceService dataSourceService)
        {
            _logger = logger;
            _connectionString = connectionString;
            _dataSourceService = dataSourceService;
        }

        /// <summary>
        /// 获取所有SQL配置
        /// </summary>
        public async Task<List<SqlConfig>> GetAllSqlConfigsAsync()
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT Id, Name, Category, OutputType, OutputTarget, Description, 
                           SqlStatement, CreatedDate, LastModified,
                           DataSourceId, OutputDataSourceId, IsEnabled, Parameters, TimeoutSeconds, MaxRows, 
                           AllowDeleteTarget, ClearTargetBeforeImport, ClearSheetBeforeOutput
                    FROM SqlConfigs 
                    ORDER BY LastModified DESC";

                var result = await connection.QueryAsync<SqlConfig>(sql);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取SQL配置列表失败: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 根据ID获取SQL配置
        /// </summary>
        public async Task<SqlConfig?> GetSqlConfigByIdAsync(string id)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT Id, Name, Category, OutputType, OutputTarget, Description, 
                           SqlStatement, CreatedDate, LastModified,
                           DataSourceId, OutputDataSourceId, IsEnabled, Parameters, TimeoutSeconds, MaxRows, 
                           AllowDeleteTarget, ClearTargetBeforeImport, ClearSheetBeforeOutput
                    FROM SqlConfigs 
                    WHERE Id = @Id";

                var result = await connection.QueryFirstOrDefaultAsync<SqlConfig>(sql, new { Id = id });
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取SQL配置失败: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根据分类获取SQL配置
        /// </summary>
        public async Task<List<SqlConfig>> GetSqlConfigsByCategoryAsync(string category)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT Id, Name, Category, OutputType, OutputTarget, Description, 
                           SqlStatement, CreatedDate, LastModified,
                           DataSourceId, OutputDataSourceId, IsEnabled, Parameters, TimeoutSeconds, MaxRows, 
                           AllowDeleteTarget, ClearTargetBeforeImport, ClearSheetBeforeOutput
                    FROM SqlConfigs 
                    WHERE Category = @Category
                    ORDER BY LastModified DESC";

                var result = await connection.QueryAsync<SqlConfig>(sql, new { Category = category });
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据分类获取SQL配置失败: {Category}", category);
                throw;
            }
        }

        /// <summary>
        /// 搜索SQL配置
        /// </summary>
        public async Task<List<SqlConfig>> SearchSqlConfigsAsync(string searchText)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT Id, Name, Category, OutputType, OutputTarget, Description, 
                           SqlStatement, CreatedDate, LastModified,
                           DataSourceId, OutputDataSourceId, IsEnabled, Parameters, TimeoutSeconds, MaxRows, 
                           AllowDeleteTarget, ClearTargetBeforeImport, ClearSheetBeforeOutput
                    FROM SqlConfigs 
                    WHERE Name LIKE @SearchText OR Description LIKE @SearchText OR SqlStatement LIKE @SearchText
                    ORDER BY LastModified DESC";

                var searchPattern = $"%{searchText}%";
                var result = await connection.QueryAsync<SqlConfig>(sql, new { SearchText = searchPattern });
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索SQL配置失败: {SearchText}", searchText);
                throw;
            }
        }

        /// <summary>
        /// 创建SQL配置
        /// </summary>
        public async Task<SqlConfig> CreateSqlConfigAsync(SqlConfig sqlConfig, string? userId = null)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                // 只有在ID为空时才生成新的ID，保持导入时的原始ID
                if (string.IsNullOrEmpty(sqlConfig.Id))
                {
                    sqlConfig.Id = Guid.NewGuid().ToString();
                }
                sqlConfig.CreatedDate = DateTime.Now;
                sqlConfig.LastModified = DateTime.Now;

                var sql = @"
                    INSERT INTO SqlConfigs (Id, Name, Category, OutputType, OutputTarget, Description, 
                                          SqlStatement, CreatedDate, LastModified, DataSourceId, OutputDataSourceId, IsEnabled, Parameters,
                                          TimeoutSeconds, MaxRows, AllowDeleteTarget, ClearTargetBeforeImport, ClearSheetBeforeOutput)
                    VALUES (@Id, @Name, @Category, @OutputType, @OutputTarget, @Description, 
                            @SqlStatement, @CreatedDate, @LastModified, @DataSourceId, @OutputDataSourceId, @IsEnabled, @Parameters,
                            @TimeoutSeconds, @MaxRows, @AllowDeleteTarget, @ClearTargetBeforeImport, @ClearSheetBeforeOutput)";

                await connection.ExecuteAsync(sql, new
                {
                    sqlConfig.Id,
                    sqlConfig.Name,
                    sqlConfig.Category,
                    sqlConfig.OutputType,
                    sqlConfig.OutputTarget,
                    sqlConfig.Description,
                    sqlConfig.SqlStatement,
                    CreatedDate = sqlConfig.CreatedDate,
                    LastModified = sqlConfig.LastModified,
                    sqlConfig.DataSourceId,
                    sqlConfig.OutputDataSourceId,
                    sqlConfig.IsEnabled,
                    sqlConfig.Parameters,
                    sqlConfig.TimeoutSeconds,
                    sqlConfig.MaxRows,
                    sqlConfig.AllowDeleteTarget,
                    sqlConfig.ClearTargetBeforeImport,
                    sqlConfig.ClearSheetBeforeOutput
                });

                _logger.LogInformation("创建SQL配置成功: {Id}, {Name}", sqlConfig.Id, sqlConfig.Name);
                return sqlConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建SQL配置失败: {Name}", sqlConfig.Name);
                throw;
            }
        }

        /// <summary>
        /// 更新SQL配置
        /// </summary>
        public async Task<SqlConfig> UpdateSqlConfigAsync(SqlConfig sqlConfig, string? userId = null)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                sqlConfig.LastModified = DateTime.Now;

                var sql = @"
                    UPDATE SqlConfigs 
                    SET Name = @Name, Category = @Category, OutputType = @OutputType, 
                        OutputTarget = @OutputTarget, Description = @Description, 
                        SqlStatement = @SqlStatement, LastModified = @LastModified, 
                        DataSourceId = @DataSourceId, OutputDataSourceId = @OutputDataSourceId, IsEnabled = @IsEnabled, Parameters = @Parameters,
                        TimeoutSeconds = @TimeoutSeconds, MaxRows = @MaxRows,
                        AllowDeleteTarget = @AllowDeleteTarget, ClearTargetBeforeImport = @ClearTargetBeforeImport, ClearSheetBeforeOutput = @ClearSheetBeforeOutput
                    WHERE Id = @Id";

                var affectedRows = await connection.ExecuteAsync(sql, new
                {
                    sqlConfig.Id,
                    sqlConfig.Name,
                    sqlConfig.Category,
                    sqlConfig.OutputType,
                    sqlConfig.OutputTarget,
                    sqlConfig.Description,
                    sqlConfig.SqlStatement,
                    LastModified = sqlConfig.LastModified,
                    sqlConfig.DataSourceId,
                    sqlConfig.OutputDataSourceId,
                    sqlConfig.IsEnabled,
                    sqlConfig.Parameters,
                    sqlConfig.TimeoutSeconds,
                    sqlConfig.MaxRows,
                    sqlConfig.AllowDeleteTarget,
                    sqlConfig.ClearTargetBeforeImport,
                    sqlConfig.ClearSheetBeforeOutput
                });

                if (affectedRows == 0)
                {
                    throw new InvalidOperationException($"SQL配置不存在: {sqlConfig.Id}");
                }

                _logger.LogInformation("更新SQL配置成功: {Id}, {Name}", sqlConfig.Id, sqlConfig.Name);
                return sqlConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新SQL配置失败: {Id}", sqlConfig.Id);
                throw;
            }
        }

        /// <summary>
        /// 删除SQL配置
        /// </summary>
        public async Task<bool> DeleteSqlConfigAsync(string id)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "DELETE FROM SqlConfigs WHERE Id = @Id";
                var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });

                _logger.LogInformation("删除SQL配置成功: {Id}, 影响行数: {AffectedRows}", id, affectedRows);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除SQL配置失败: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 测试SQL语句
        /// </summary>
        public async Task<SqlTestResult> TestSqlStatementAsync(string sqlStatement, string? dataSourceId = null, Dictionary<string, object>? parameters = null)
        {
            var result = new SqlTestResult
            {
                IsSuccess = false,
                ErrorMessage = string.Empty,
                EstimatedRowCount = 0,
                EstimatedDurationMs = 0,
                Columns = new List<SqlColumnInfo>()
            };

            try
            {
                _logger.LogInformation("开始测试SQL语句: {SqlStatement}, 数据源ID: {DataSourceId}, 参数数量: {ParameterCount}", 
                    sqlStatement, dataSourceId, parameters?.Count ?? 0);
                
                // 获取数据源连接字符串
                string connectionString = _connectionString; // 默认使用系统数据库
                if (!string.IsNullOrEmpty(dataSourceId))
                {
                    var dataSource = await _dataSourceService.GetDataSourceByIdAsync(dataSourceId);
                    if (dataSource != null && !string.IsNullOrEmpty(dataSource.ConnectionString))
                    {
                        connectionString = dataSource.ConnectionString;
                        _logger.LogInformation("使用指定数据源: {DataSourceName}", dataSource.Name);
                    }
                }

                // 不对SQL进行任何改写，直接使用用户输入的SQL
                var testSql = sqlStatement.Trim();

                var startTime = DateTime.Now;
                
                // 使用公用方法执行查询
                var queryResult = await ExecuteSqlQueryAsync(testSql, dataSourceId, parameters);
                if (!queryResult.IsSuccess)
                {
                    result.ErrorMessage = queryResult.ErrorMessage;
                    return result;
                }

                var endTime = DateTime.Now;
                var duration = (int)(endTime - startTime).TotalMilliseconds;

                // 设置测试结果
                result.IsSuccess = true;
                result.EstimatedRowCount = queryResult.RowCount;
                result.EstimatedDurationMs = duration;
                result.Columns = queryResult.Columns;
                result.SampleData = queryResult.Data.Take(10).ToList(); // 限制最多10行样本数据

                _logger.LogInformation("SQL测试成功: {SqlStatement}, 预估行数: {EstimatedRows}, 耗时: {Duration}ms", 
                    sqlStatement, result.EstimatedRowCount, duration);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "SQL测试失败: {SqlStatement}", sqlStatement);
            }

            return result;
        }

        /// <summary>
        /// 根据连接字符串判断数据源类型
        /// </summary>
        private string GetDataSourceType(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "sqlite";

            var lowerConnectionString = connectionString.ToLower();
            
            // 检查MySQL连接字符串格式
            if (lowerConnectionString.Contains("server=") || lowerConnectionString.Contains("data source="))
            {
                if (lowerConnectionString.Contains("mysql"))
                    return "mysql";
                else if (lowerConnectionString.Contains("sql server") || lowerConnectionString.Contains("mssql"))
                    return "sqlserver";
                else if (lowerConnectionString.Contains("postgresql") || lowerConnectionString.Contains("postgres"))
                    return "postgresql";
                else if (lowerConnectionString.Contains("oracle"))
                    return "oracle";
            }
            
            // 检查其他可能的连接字符串格式
            if (lowerConnectionString.Contains("uid=") || lowerConnectionString.Contains("user id="))
            {
                if (lowerConnectionString.Contains("port=") && lowerConnectionString.Contains("database="))
                    return "mysql";
                else if (lowerConnectionString.Contains("initial catalog="))
                    return "sqlserver";
                else if (lowerConnectionString.Contains("host=") && lowerConnectionString.Contains("username="))
                    return "postgresql";
                else if (lowerConnectionString.Contains("data source=") && lowerConnectionString.Contains("user id="))
                    return "oracle";
            }
            
            return "sqlite"; // 默认为SQLite
        }

        /// <summary>
        /// 根据数据源配置获取数据库类型（优先使用配置中的类型）
        /// </summary>
        private async Task<string> GetDataSourceTypeFromConfigAsync(string? dataSourceId)
        {
            if (string.IsNullOrEmpty(dataSourceId))
                return "sqlite";

            try
            {
                var dataSource = await _dataSourceService.GetDataSourceByIdAsync(dataSourceId);
                if (dataSource != null && !string.IsNullOrEmpty(dataSource.Type))
                {
                    // 标准化类型名称
                    var normalizedType = dataSource.Type.ToLower().Trim();
                    switch (normalizedType)
                    {
                        case "sqlite":
                        case "sqlite3":
                            return "sqlite";
                        case "mysql":
                        case "mariadb":
                            return "mysql";
                        case "sqlserver":
                        case "mssql":
                        case "sql server":
                            return "sqlserver";
                        case "postgresql":
                        case "postgres":
                            return "postgresql";
                        case "oracle":
                            return "oracle";
                        default:
                            _logger.LogWarning("未知的数据源类型: {Type}，将使用连接字符串解析", dataSource.Type);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取数据源配置失败: {DataSourceId}", dataSourceId);
            }

            return "sqlite"; // 默认返回SQLite
        }

        /// <summary>
        /// 执行已保存的SQL配置
        /// </summary>
        public async Task<SqlExecutionResult> ExecuteSqlConfigAsync(string sqlConfigId, Dictionary<string, object>? parameters = null, string? userId = null)
        {
            var result = new SqlExecutionResult
            {
                Id = Guid.NewGuid().ToString(),
                SqlConfigId = sqlConfigId,
                StartTime = DateTime.Now,
                Status = "执行中",
                AffectedRows = 0
            };

            try
            {
                // 获取SQL配置
                var sqlConfig = await GetSqlConfigByIdAsync(sqlConfigId);
                if (sqlConfig == null)
                {
                    result.Status = "失败";
                    result.ErrorMessage = "SQL配置不存在";
                    return result;
                }

                // 执行SQL查询
                var startTime = DateTime.Now;
                
                // 使用公用方法执行查询
                var queryResult = await ExecuteSqlQueryAsync(sqlConfig.SqlStatement, sqlConfig.DataSourceId, parameters);
                if (!queryResult.IsSuccess)
                {
                    result.Status = "失败";
                    result.ErrorMessage = queryResult.ErrorMessage;
                    return result;
                }

                var endTime = DateTime.Now;
                var duration = (long)(endTime - startTime).TotalMilliseconds;
                var affectedRows = queryResult.AffectedRows;

                result.Status = "成功";
                result.EndTime = endTime;
                result.Duration = duration;
                result.AffectedRows = affectedRows;
                result.ExecutedBy = userId ?? "system";

                // 保存执行历史
                await SaveExecutionHistoryAsync(result, userId);

                _logger.LogInformation("SQL执行成功: {SqlConfigId}, 影响行数: {AffectedRows}, 耗时: {Duration}ms", 
                    sqlConfigId, affectedRows, duration);
            }
            catch (Exception ex)
            {
                result.Status = "失败";
                result.EndTime = DateTime.Now;
                result.Duration = (long)(result.EndTime.Value - result.StartTime).TotalMilliseconds;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "SQL执行失败: {SqlConfigId}", sqlConfigId);
            }

            return result;
        }

        /// <summary>
        /// 获取SQL执行历史
        /// </summary>
        public async Task<List<SqlExecutionResult>> GetSqlExecutionHistoryAsync(string sqlConfigId, int limit = 50)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT Id, SqlConfigId, Status, StartTime, EndTime, Duration, 
                           AffectedRows, ErrorMessage, ResultData, ExecutedBy, ExecutionParameters
                    FROM SqlExecutionHistory 
                    WHERE SqlConfigId = @SqlConfigId
                    ORDER BY StartTime DESC
                    LIMIT @Limit";

                var result = await connection.QueryAsync<SqlExecutionResult>(sql, new { SqlConfigId = sqlConfigId, Limit = limit });
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取SQL执行历史失败: {SqlConfigId}", sqlConfigId);
                throw;
            }
        }

        /// <summary>
        /// 获取所有SQL分类
        /// </summary>
        public async Task<List<string>> GetAllCategoriesAsync()
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "SELECT DISTINCT Category FROM SqlConfigs WHERE Category IS NOT NULL AND Category != '' ORDER BY Category";
                var result = await connection.QueryAsync<string>(sql);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取SQL分类失败");
                throw;
            }
        }

        /// <summary>
        /// 验证SQL配置
        /// </summary>
        public async Task<Core.Interfaces.ValidationResult> ValidateSqlConfigAsync(SqlConfig sqlConfig)
        {
            var result = new Core.Interfaces.ValidationResult
            {
                IsValid = true,
                Errors = new List<string>()
            };

            try
            {
                if (string.IsNullOrWhiteSpace(sqlConfig.Name))
                {
                    result.IsValid = false;
                    result.Errors.Add("SQL名称不能为空");
                }

                if (string.IsNullOrWhiteSpace(sqlConfig.Category))
                {
                    result.IsValid = false;
                    result.Errors.Add("分类不能为空");
                }

                if (string.IsNullOrWhiteSpace(sqlConfig.OutputType))
                {
                    result.IsValid = false;
                    result.Errors.Add("输出类型不能为空");
                }

                if (string.IsNullOrWhiteSpace(sqlConfig.OutputTarget))
                {
                    result.IsValid = false;
                    result.Errors.Add("输出目标不能为空");
                }

                if (string.IsNullOrWhiteSpace(sqlConfig.SqlStatement))
                {
                    result.IsValid = false;
                    result.Errors.Add("SQL语句不能为空");
                }

                // 验证输出类型
                if (!new[] { "数据表", "Excel工作表" }.Contains(sqlConfig.OutputType))
                {
                    result.IsValid = false;
                    result.Errors.Add("输出类型必须是'数据表'或'Excel工作表'");
                }

                // 验证超时时间
                if (sqlConfig.TimeoutSeconds <= 0 || sqlConfig.TimeoutSeconds > 3600)
                {
                    result.IsValid = false;
                    result.Errors.Add("超时时间必须在1-3600秒之间");
                }

                // 验证最大行数
                if (sqlConfig.MaxRows <= 0 || sqlConfig.MaxRows > 1000000)
                {
                    result.IsValid = false;
                    result.Errors.Add("最大行数必须在1-1000000之间");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证SQL配置失败");
                result.IsValid = false;
                result.Errors.Add($"验证过程发生错误: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 保存执行历史
        /// </summary>
        private async Task SaveExecutionHistoryAsync(SqlExecutionResult result, string? userId = null)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO SqlExecutionHistory (Id, SqlConfigId, Status, StartTime, EndTime, Duration, 
                                                    AffectedRows, ErrorMessage, ResultData, ExecutedBy, ExecutionParameters)
                    VALUES (@Id, @SqlConfigId, @Status, @StartTime, @EndTime, @Duration, 
                            @AffectedRows, @ErrorMessage, @ResultData, @ExecutedBy, @ExecutionParameters)";

                result.ExecutedBy = userId ?? "system";
                await connection.ExecuteAsync(sql, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存SQL执行历史失败");
                // 不抛出异常，避免影响主流程
            }
        }

        /// <summary>
        /// 执行SQL查询（公用方法）
        /// </summary>
        public async Task<SqlQueryResult> ExecuteSqlQueryAsync(string sqlStatement, string? dataSourceId = null, Dictionary<string, object>? parameters = null, ISqlProgressCallback? progressCallback = null)
        {
            var result = new SqlQueryResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("开始执行SQL查询: {SqlStatement}, 数据源ID: {DataSourceId}, 参数数量: {ParameterCount}", 
                    sqlStatement, dataSourceId, parameters?.Count ?? 0);

                // 1. 验证输入参数
                if (string.IsNullOrWhiteSpace(sqlStatement))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "SQL语句不能为空";
                    return result;
                }

                // 2. 获取数据源连接字符串
                string connectionString = _connectionString; // 默认使用系统数据库
                if (!string.IsNullOrEmpty(dataSourceId))
                {
                    var dataSource = await _dataSourceService.GetDataSourceByIdAsync(dataSourceId);
                    if (dataSource != null && !string.IsNullOrEmpty(dataSource.ConnectionString))
                    {
                        connectionString = dataSource.ConnectionString;
                        _logger.LogInformation("使用指定数据源: {DataSourceName}", dataSource.Name);
                    }
                }

                // 3. 执行SQL查询
                progressCallback?.UpdateOperation("正在执行SQL查询...");
                progressCallback?.UpdateDetailMessage("正在从数据源获取数据...");
                
                if (progressCallback?.IsCancelled() == true)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "操作已取消";
                    return result;
                }

                var queryResult = await ExecuteQueryInternalAsync(sqlStatement, connectionString, parameters, progressCallback, dataSourceId);
                if (!queryResult.IsSuccess)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = queryResult.ErrorMessage;
                    return result;
                }

                // 4. 转换结果
                result.Columns = queryResult.Columns;
                result.Data = queryResult.Data;
                result.IsSuccess = true;
                result.AffectedRows = queryResult.Data.Count;

                var endTime = DateTime.UtcNow;
                result.ExecutionTimeMs = (long)(endTime - startTime).TotalMilliseconds;

                _logger.LogInformation("SQL查询执行成功: {SqlStatement}, 获取到 {RowCount} 行数据, 耗时: {Duration}ms", 
                    sqlStatement, result.RowCount, result.ExecutionTimeMs);

                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.ExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

                _logger.LogError(ex, "执行SQL查询失败: {SqlStatement}", sqlStatement);
                return result;
            }
        }

        /// <summary>
        /// 执行SQL并输出到数据表（支持参数）
        /// </summary>
        public async Task<SqlOutputResult> ExecuteSqlToTableAsync(string sqlStatement, string? queryDataSourceId, string? targetDataSourceId, string targetTableName, bool clearTableBeforeInsert = false, Dictionary<string, object>? parameters = null, ISqlProgressCallback? progressCallback = null)
        {
            var result = new SqlOutputResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("开始执行SQL输出到数据表: {SqlStatement}, 目标表: {TargetTable}, 清空表: {ClearTable}, 参数数量: {ParameterCount}", 
                    sqlStatement, targetTableName, clearTableBeforeInsert, parameters?.Count ?? 0);

                // 1. 验证输入参数
                if (string.IsNullOrWhiteSpace(sqlStatement))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "SQL语句不能为空";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(targetTableName))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "目标表名不能为空";
                    return result;
                }

                // 2. 获取查询数据源连接
                string queryConnectionString = _connectionString; // 默认使用系统数据库
                if (!string.IsNullOrEmpty(queryDataSourceId))
                {
                    var queryDataSource = await _dataSourceService.GetDataSourceByIdAsync(queryDataSourceId);
                    if (queryDataSource != null && !string.IsNullOrEmpty(queryDataSource.ConnectionString))
                    {
                        queryConnectionString = queryDataSource.ConnectionString;
                        _logger.LogInformation("使用查询数据源: {DataSourceName}", queryDataSource.Name);
                    }
                }

                // 3. 获取目标数据源连接
                string targetConnectionString = _connectionString; // 默认使用系统数据库
                if (!string.IsNullOrEmpty(targetDataSourceId))
                {
                    var targetDataSource = await _dataSourceService.GetDataSourceByIdAsync(targetDataSourceId);
                    if (targetDataSource != null && !string.IsNullOrEmpty(targetDataSource.ConnectionString))
                    {
                        targetConnectionString = targetDataSource.ConnectionString;
                        _logger.LogInformation("使用目标数据源: {DataSourceName}", targetDataSource.Name);
                    }
                }

                // 4. 执行查询获取数据
                progressCallback?.UpdateOperation("正在执行SQL查询...");
                progressCallback?.UpdateDetailMessage("正在从数据源获取数据...");
                
                if (progressCallback?.IsCancelled() == true)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "操作已取消";
                    return result;
                }
                
                var queryResult = await ExecuteQueryInternalAsync(sqlStatement, queryConnectionString, parameters, progressCallback, queryDataSourceId);
                if (!queryResult.IsSuccess)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"查询执行失败: {queryResult.ErrorMessage}";
                    return result;
                }

                // 5. 检查目标表是否存在（不再自动创建）
                progressCallback?.UpdateOperation("正在检查目标表...");
                progressCallback?.UpdateDetailMessage($"检查表 {targetTableName} 是否存在...");
                
                if (progressCallback?.IsCancelled() == true)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "操作已取消";
                    return result;
                }
                
                var tableExists = await CheckTableExistsAsync(targetTableName, targetConnectionString);
                if (!tableExists)
                    {
                        result.IsSuccess = false;
                    result.ErrorMessage = $"目标表 {targetTableName} 不存在，请先在数据库中创建该表";
                        return result;
                    }
                
                if (clearTableBeforeInsert)
                {
                    // 6. 如果表存在且需要清空表，则先清空表
                    progressCallback?.UpdateOperation("正在清空目标表...");
                    progressCallback?.UpdateDetailMessage($"清空表 {targetTableName} 中的数据...");
                    
                    _logger.LogInformation("开始清空目标表 {TargetTable}", targetTableName);
                    var clearSuccess = await ClearTableAsync(targetTableName, targetConnectionString);
                    if (!clearSuccess)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = $"清空表 {targetTableName} 失败";
                        return result;
                    }
                    _logger.LogInformation("目标表 {TargetTable} 清空成功", targetTableName);
                }

                // 6. 将数据插入到目标表
                progressCallback?.UpdateOperation("正在插入数据...");
                progressCallback?.UpdateDetailMessage($"将 {queryResult.Data.Count:N0} 行数据插入到表 {targetTableName}...");
                progressCallback?.UpdateStatistics(0, queryResult.Data.Count);
                
                if (progressCallback?.IsCancelled() == true)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "操作已取消";
                    return result;
                }
                
                var insertResult = await InsertDataToTableAsync(targetTableName, queryResult.Data, targetConnectionString, progressCallback);
                if (!insertResult.IsSuccess)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"数据插入失败: {insertResult.ErrorMessage}";
                    return result;
                }

                // 7. 设置成功结果
                result.IsSuccess = true;
                result.AffectedRows = insertResult.AffectedRows;
                result.ExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                result.OutputPath = $"数据表: {targetTableName}";

                _logger.LogInformation("SQL输出到数据表执行成功: {SqlStatement}, 目标表: {TargetTable}, 影响行数: {AffectedRows}", 
                    sqlStatement, targetTableName, result.AffectedRows);

                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.ExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

                _logger.LogError(ex, "执行SQL输出到数据表失败: {SqlStatement}, 目标表: {TargetTable}", 
                    sqlStatement, targetTableName);

                return result;
            }
        }

        /// <summary>
        /// 执行SQL并输出到Excel（支持参数）
        /// </summary>
        public async Task<SqlOutputResult> ExecuteSqlToExcelAsync(string sqlStatement, string? queryDataSourceId, string outputPath, string sheetName, bool clearSheetBeforeOutput = false, Dictionary<string, object>? parameters = null, ISqlProgressCallback? progressCallback = null)
        {
            var result = new SqlOutputResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("开始执行SQL输出到Excel: {SqlStatement}, 输出路径: {OutputPath}, Sheet: {SheetName}, 清空Sheet: {ClearSheet}, 参数数量: {ParameterCount}", 
                    sqlStatement, outputPath, sheetName, clearSheetBeforeOutput, parameters?.Count ?? 0);

                // 1. 验证输入参数
                if (string.IsNullOrWhiteSpace(sqlStatement))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "SQL语句不能为空";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "输出路径不能为空";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(sheetName))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Sheet名称不能为空";
                    return result;
                }

                // 2. 获取数据源连接字符串
                string connectionString = _connectionString; // 默认使用系统数据库
                if (!string.IsNullOrEmpty(queryDataSourceId))
                {
                    var dataSource = await _dataSourceService.GetDataSourceByIdAsync(queryDataSourceId);
                    if (dataSource != null && !string.IsNullOrEmpty(dataSource.ConnectionString))
                    {
                        connectionString = dataSource.ConnectionString;
                    }
                }

                // 3. 确保输出目录存在
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 4. 执行SQL查询获取数据
                progressCallback?.UpdateOperation("正在执行SQL查询...");
                progressCallback?.UpdateDetailMessage("正在从数据源获取数据...");
                
                if (progressCallback?.IsCancelled() == true)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "操作已取消";
                    return result;
                }
                
                _logger.LogInformation("开始执行SQL查询...");
                var queryResult = await ExecuteQueryInternalAsync(sqlStatement, connectionString, parameters, progressCallback, queryDataSourceId);
                if (!queryResult.IsSuccess)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = queryResult.ErrorMessage;
                    result.ExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    return result;
                }

                _logger.LogInformation("SQL查询完成，获取到 {RowCount} 行数据，开始导出到Excel...", queryResult.Data.Count);

                // 5. 输出数据到Excel
                progressCallback?.UpdateOperation("正在导出到Excel...");
                progressCallback?.UpdateDetailMessage($"将 {queryResult.Data.Count:N0} 行数据写入Excel文件...");
                progressCallback?.UpdateStatistics(0, queryResult.Data.Count);
                
                if (progressCallback?.IsCancelled() == true)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "操作已取消";
                    return result;
                }
                
                var excelResult = await ExportDataToExcelAsync(outputPath, sheetName, queryResult.Columns, queryResult.Data, clearSheetBeforeOutput, progressCallback);
                if (!excelResult.IsSuccess)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = excelResult.ErrorMessage;
                    result.ExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    return result;
                }

                // 6. 设置成功结果
                result.IsSuccess = true;
                result.AffectedRows = queryResult.Data.Count;
                result.ExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                result.OutputPath = outputPath;

                _logger.LogInformation("SQL输出到Excel执行成功: {SqlStatement}, 输出文件: {OutputPath}, Sheet: {SheetName}, 影响行数: {AffectedRows}, 执行时间: {ExecutionTime}ms", 
                    sqlStatement, outputPath, sheetName, result.AffectedRows, result.ExecutionTimeMs);

                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.ExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

                _logger.LogError(ex, "执行SQL输出到Excel失败: {SqlStatement}, 输出路径: {OutputPath}", 
                    sqlStatement, outputPath);

                return result;
            }
        }

        #region 私有方法 - 表操作支持

        /// <summary>
        /// 查询结果数据
        /// </summary>
        private class QueryResult
        {
            public bool IsSuccess { get; set; }
            public string? ErrorMessage { get; set; }
            public List<SqlColumnInfo> Columns { get; set; } = new List<SqlColumnInfo>();
            public List<Dictionary<string, object>> Data { get; set; } = new List<Dictionary<string, object>>();
        }

        /// <summary>
        /// 插入结果数据
        /// </summary>
        private class InsertResult
        {
            public bool IsSuccess { get; set; }
            public string? ErrorMessage { get; set; }
            public int AffectedRows { get; set; }
        }

        /// <summary>
        /// 导出数据到Excel
        /// </summary>
        private async Task<InsertResult> ExportDataToExcelAsync(string outputPath, string sheetName, List<SqlColumnInfo> columns, List<Dictionary<string, object>> data, bool clearSheetBeforeOutput, ISqlProgressCallback? progressCallback = null)
        {
            var result = new InsertResult();

            try
            {
                _logger.LogInformation("开始导出数据到Excel: {OutputPath}, Sheet: {SheetName}, 数据行数: {RowCount}, 清空Sheet: {ClearSheet}", 
                    outputPath, sheetName, data.Count, clearSheetBeforeOutput);

                // 设置EPPlus许可证上下文
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage package;
                bool isNewFile = false;

                // 检查目标Excel文件是否存在
                if (File.Exists(outputPath))
                {
                    _logger.LogInformation("目标Excel文件已存在，将读取现有文件: {OutputPath}", outputPath);
                    
                    try
                    {
                        // 读取现有Excel文件
                        using var fileStream = new FileStream(outputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        package = new ExcelPackage(fileStream);
                        
                        // 检查是否包含目标工作表
                        var existingWorksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == sheetName);
                        if (existingWorksheet != null)
                        {
                            _logger.LogInformation("目标工作表已存在: {SheetName}", sheetName);
                            
                            if (clearSheetBeforeOutput)
                            {
                                // 清空工作表内容
                                _logger.LogInformation("根据配置清空工作表内容: {SheetName}", sheetName);
                                existingWorksheet.Cells.Clear();
                            }
                            else
                            {
                                // 不清空，获取现有数据的行数，用于追加
                                var lastRow = existingWorksheet.Dimension?.End?.Row ?? 0;
                                _logger.LogInformation("工作表 {SheetName} 现有数据行数: {LastRow}, 将在第 {NextRow} 行开始追加数据", 
                                    sheetName, lastRow, lastRow + 1);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("目标工作表不存在，将创建新工作表: {SheetName}", sheetName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "读取现有Excel文件失败，将创建新文件: {OutputPath}", outputPath);
                        package = new ExcelPackage();
                        isNewFile = true;
                    }
                }
                else
                {
                    _logger.LogInformation("目标Excel文件不存在，将创建新文件: {OutputPath}", outputPath);
                    package = new ExcelPackage();
                    isNewFile = true;
                }
                
                // 获取或创建工作表
                var worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == sheetName);
                if (worksheet == null)
                {
                    worksheet = package.Workbook.Worksheets.Add(sheetName);
                    _logger.LogInformation("创建新工作表: {SheetName}", sheetName);
                }

                // 确定数据写入的起始行和是否需要添加表头
                int startRow;
                bool needHeader = false;
                
                if (isNewFile || clearSheetBeforeOutput)
                {
                    // 新文件或清空后，从第1行开始写入标题
                    startRow = 1;
                    needHeader = true;
                    
                    _logger.LogInformation("新文件或清空模式，将在第1行添加表头");
                }
                else
                {
                    // 不清空，检查现有工作表是否有表头
                    var lastRow = worksheet.Dimension?.End?.Row ?? 0;
                    
                    if (lastRow == 0)
                    {
                        // 工作表为空，需要添加表头
                        startRow = 1;
                        needHeader = true;
                        _logger.LogInformation("现有工作表为空，将在第1行添加表头");
                    }
                    else
                    {
                        // 检查第1行是否已经是表头（通过检查是否有样式或内容）
                        var firstRowHasContent = false;
                        for (int i = 1; i <= columns.Count; i++)
                        {
                            var cell = worksheet.Cells[1, i];
                            if (cell.Value != null && !string.IsNullOrEmpty(cell.Value.ToString()))
                            {
                                firstRowHasContent = true;
                                break;
                            }
                        }
                        
                        if (!firstRowHasContent)
                        {
                            // 第1行没有内容，需要添加表头
                            startRow = 1;
                            needHeader = true;
                            _logger.LogInformation("第1行没有内容，将在第1行添加表头");
                        }
                        else
                        {
                            // 第1行已有内容，假设是表头，在现有数据后追加
                            startRow = lastRow + 1;
                            _logger.LogInformation("第1行已有内容，将在现有数据后追加，起始行: {StartRow}", startRow);
                        }
                    }
                }

                // 如果需要添加表头，则写入列标题
                if (needHeader)
                {
                for (int i = 0; i < columns.Count; i++)
                {
                        worksheet.Cells[startRow, i + 1].Value = columns[i].Name;
                        worksheet.Cells[startRow, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[startRow, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[startRow, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }
                    
                    // 数据从表头下一行开始写入
                    startRow++;
                    _logger.LogInformation("表头已添加，数据将从第 {StartRow} 行开始写入", startRow);
                }

                // 批量写入数据行以提高性能
                const int batchSize = 1000; // 每批处理1000行
                var totalRows = data.Count;
                
                for (int batchStart = 0; batchStart < totalRows; batchStart += batchSize)
                {
                    var batchEnd = Math.Min(batchStart + batchSize, totalRows);
                    var batchData = new List<object[]>();
                    
                    // 准备批次数据
                    for (int rowIndex = batchStart; rowIndex < batchEnd; rowIndex++)
                    {
                        var row = data[rowIndex];
                        var rowData = new object[columns.Count];
                        
                        for (int colIndex = 0; colIndex < columns.Count; colIndex++)
                        {
                            var columnName = columns[colIndex].Name;
                            rowData[colIndex] = row.ContainsKey(columnName) ? row[columnName] ?? DBNull.Value : DBNull.Value;
                        }
                        
                        batchData.Add(rowData);
                    }
                    
                    // 批量写入数据
                    var currentStartRow = startRow + batchStart;
                    worksheet.Cells[currentStartRow, 1].LoadFromArrays(batchData);
                    
                    // 更新进度
                    progressCallback?.UpdateStatistics(batchEnd, totalRows);
                    progressCallback?.UpdateSubDetailMessage($"已写入 {batchEnd:N0} / {totalRows:N0} 行");
                    
                    // 检查是否取消
                    if (progressCallback?.IsCancelled() == true)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = "操作已取消";
                        return result;
                    }
                    
                    // 记录进度
                    var progress = (double)(batchEnd) / totalRows * 100;
                    _logger.LogDebug("Excel导出进度: {Progress:F1}% ({Current}/{Total})", progress, batchEnd, totalRows);
                }

                // 自动调整列宽
                worksheet.Cells.AutoFitColumns();

                // 保存Excel文件
                await package.SaveAsAsync(new FileInfo(outputPath));

                result.IsSuccess = true;
                result.AffectedRows = data.Count;

                _logger.LogInformation("数据导出到Excel成功: {OutputPath}, Sheet: {SheetName}, 影响行数: {AffectedRows}, 起始行: {StartRow}", 
                    outputPath, sheetName, result.AffectedRows, startRow);

                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;

                _logger.LogError(ex, "导出数据到Excel失败: {OutputPath}, Sheet: {SheetName}", 
                    outputPath, sheetName);

                return result;
            }
        }

        /// <summary>
        /// 执行查询获取数据（内部方法，支持参数）
        /// </summary>
        private async Task<QueryResult> ExecuteQueryInternalAsync(string sqlStatement, string connectionString, Dictionary<string, object>? parameters = null, ISqlProgressCallback? progressCallback = null, string? dataSourceId = null)
        {
            var result = new QueryResult();

            try
            {
                _logger.LogInformation("开始执行查询，连接字符串: {ConnectionString}", connectionString);
                
                // 优先使用数据源配置中的类型，如果没有则使用连接字符串解析
                string dataSourceType;
                if (!string.IsNullOrEmpty(dataSourceId))
                {
                    dataSourceType = await GetDataSourceTypeFromConfigAsync(dataSourceId);
                    _logger.LogInformation("从数据源配置识别的类型: {DataSourceType}", dataSourceType);
                }
                else
                {
                    dataSourceType = GetDataSourceType(connectionString);
                    _logger.LogInformation("从连接字符串识别的类型: {DataSourceType}", dataSourceType);
                }
                
                switch (dataSourceType)
                {
                    case "sqlite":
                        await ExecuteQuerySqliteAsync(sqlStatement, connectionString, parameters, result);
                        break;
                    case "mysql":
                        await ExecuteQueryMySqlAsync(sqlStatement, connectionString, parameters, result);
                        break;
                    case "sqlserver":
                        await ExecuteQuerySqlServerAsync(sqlStatement, connectionString, parameters, result);
                        break;
                    case "postgresql":
                        await ExecuteQueryPostgreSqlAsync(sqlStatement, connectionString, parameters, result);
                        break;
                    case "oracle":
                        await ExecuteQueryOracleAsync(sqlStatement, connectionString, parameters, result);
                        break;
                    default:
                        await ExecuteQuerySqliteAsync(sqlStatement, connectionString, parameters, result);
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "执行查询失败: {SqlStatement}", sqlStatement);
                return result;
            }
        }

        private async Task ExecuteQuerySqliteAsync(string sqlStatement, string connectionString, Dictionary<string, object>? parameters, QueryResult result)
        {
            try
        {
            using var connection = new SQLiteConnection(connectionString);
                
                // SQLite不支持ConnectionTimeout，但可以设置其他超时
            await connection.OpenAsync();

            using var command = new SQLiteCommand(sqlStatement, connection);
                
                // SQLite不支持CommandTimeout，但可以设置其他超时
                
                // 添加参数
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }
                
                _logger.LogInformation("开始执行SQLite查询");
                
            using var reader = await command.ExecuteReaderAsync();

            // 获取列信息
            for (int i = 0; i < reader.FieldCount; i++)
            {
                result.Columns.Add(new SqlColumnInfo
                {
                    Name = reader.GetName(i),
                    DataType = reader.GetDataTypeName(i)
                });
            }

            // 读取数据
                var rowCount = 0;
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[reader.GetName(i)] = value;
                }
                result.Data.Add(row);
                    rowCount++;
                    
                    // 限制最大行数，防止内存溢出
                    if (rowCount >= 10000)
                    {
                        _logger.LogWarning("SQLite查询结果超过10000行，已截断");
                        break;
                    }
            }

            result.IsSuccess = true;
                _logger.LogInformation("SQLite查询执行完成，返回 {RowCount} 行数据", rowCount);
            }
            catch (SQLiteException ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"SQLite错误: {ex.Message} (错误代码: {ex.ErrorCode})";
                _logger.LogError(ex, "SQLite查询执行失败: {SqlStatement}", sqlStatement);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"执行查询时发生错误: {ex.Message}";
                _logger.LogError(ex, "SQLite查询执行异常: {SqlStatement}", sqlStatement);
            }
        }

        private async Task ExecuteQueryMySqlAsync(string sqlStatement, string connectionString, Dictionary<string, object>? parameters, QueryResult result)
        {
            try
            {
                // 为MySQL连接字符串添加超时参数
                var connectionStringBuilder = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder(connectionString);
                connectionStringBuilder.ConnectionTimeout = 30; // 30秒连接超时
                connectionStringBuilder.DefaultCommandTimeout = 60; // 60秒命令超时
                
                using var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionStringBuilder.ConnectionString);
            await connection.OpenAsync();

            using var command = new MySql.Data.MySqlClient.MySqlCommand(sqlStatement, connection);
                
                // 设置命令超时
                command.CommandTimeout = 60; // 60秒命令超时
                
                // 添加参数
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }
                
                _logger.LogInformation("开始执行MySQL查询，超时设置: {Timeout}s", command.CommandTimeout);
                
            using var reader = await command.ExecuteReaderAsync();

            // 获取列信息
            for (int i = 0; i < reader.FieldCount; i++)
            {
                result.Columns.Add(new SqlColumnInfo
                {
                    Name = reader.GetName(i),
                    DataType = reader.GetDataTypeName(i)
                });
            }

            // 读取数据
                var rowCount = 0;
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[reader.GetName(i)] = value;
                }
                result.Data.Add(row);
                    rowCount++;
                    
                    // 限制最大行数，防止内存溢出
                    if (rowCount >= 10000)
                    {
                        _logger.LogWarning("MySQL查询结果超过10000行，已截断");
                        break;
                    }
            }

            result.IsSuccess = true;
                _logger.LogInformation("MySQL查询执行完成，返回 {RowCount} 行数据", rowCount);
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"MySQL错误: {ex.Message} (错误代码: {ex.Number})";
                _logger.LogError(ex, "MySQL查询执行失败: {SqlStatement}", sqlStatement);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"执行查询时发生错误: {ex.Message}";
                _logger.LogError(ex, "MySQL查询执行异常: {SqlStatement}", sqlStatement);
            }
        }

        private async Task ExecuteQuerySqlServerAsync(string sqlStatement, string connectionString, Dictionary<string, object>? parameters, QueryResult result)
        {
            try
            {
                // 为SQL Server连接字符串添加超时参数
                var connectionStringBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                connectionStringBuilder.ConnectTimeout = 30; // 30秒连接超时
                
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionStringBuilder.ConnectionString);
            await connection.OpenAsync();

            using var command = new Microsoft.Data.SqlClient.SqlCommand(sqlStatement, connection);
                
                // 设置命令超时
                command.CommandTimeout = 60; // 60秒命令超时
                
                // 添加参数
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }
                
                _logger.LogInformation("开始执行SQL Server查询，超时设置: {Timeout}s", command.CommandTimeout);
                
            using var reader = await command.ExecuteReaderAsync();

            // 获取列信息
            for (int i = 0; i < reader.FieldCount; i++)
            {
                result.Columns.Add(new SqlColumnInfo
                {
                    Name = reader.GetName(i),
                    DataType = reader.GetDataTypeName(i)
                });
            }

            // 读取数据
                var rowCount = 0;
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[reader.GetName(i)] = value;
                }
                result.Data.Add(row);
                    rowCount++;
                    
                    // 限制最大行数，防止内存溢出
                    if (rowCount >= 10000)
                    {
                        _logger.LogWarning("SQL Server查询结果超过10000行，已截断");
                        break;
                    }
            }

            result.IsSuccess = true;
                _logger.LogInformation("SQL Server查询执行完成，返回 {RowCount} 行数据", rowCount);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"SQL Server错误: {ex.Message} (错误代码: {ex.Number})";
                _logger.LogError(ex, "SQL Server查询执行失败: {SqlStatement}", sqlStatement);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"执行查询时发生错误: {ex.Message}";
                _logger.LogError(ex, "SQL Server查询执行异常: {SqlStatement}", sqlStatement);
            }
        }

        private async Task ExecuteQueryPostgreSqlAsync(string sqlStatement, string connectionString, Dictionary<string, object>? parameters, QueryResult result)
        {
            try
            {
                // 为PostgreSQL连接字符串添加超时参数
                var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
                connectionStringBuilder.Timeout = 30; // 30秒连接超时
                
                using var connection = new Npgsql.NpgsqlConnection(connectionStringBuilder.ConnectionString);
            await connection.OpenAsync();

            using var command = new Npgsql.NpgsqlCommand(sqlStatement, connection);
                
                // 设置命令超时
                command.CommandTimeout = 60; // 60秒命令超时
                
                // 添加参数
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }
                }
                
                _logger.LogInformation("开始执行PostgreSQL查询，超时设置: {Timeout}s", command.CommandTimeout);
                
            using var reader = await command.ExecuteReaderAsync();

            // 获取列信息
            for (int i = 0; i < reader.FieldCount; i++)
            {
                result.Columns.Add(new SqlColumnInfo
                {
                    Name = reader.GetName(i),
                    DataType = reader.GetDataTypeName(i)
                });
            }

            // 读取数据
                var rowCount = 0;
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[reader.GetName(i)] = value;
                }
                result.Data.Add(row);
                    rowCount++;
                    
                    // 限制最大行数，防止内存溢出
                    if (rowCount >= 10000)
                    {
                        _logger.LogWarning("PostgreSQL查询结果超过10000行，已截断");
                        break;
                    }
            }

            result.IsSuccess = true;
                _logger.LogInformation("PostgreSQL查询执行完成，返回 {RowCount} 行数据", rowCount);
            }
            catch (Npgsql.NpgsqlException ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"PostgreSQL错误: {ex.Message} (错误代码: {ex.ErrorCode})";
                _logger.LogError(ex, "PostgreSQL查询执行失败: {SqlStatement}", sqlStatement);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"执行查询时发生错误: {ex.Message}";
                _logger.LogError(ex, "PostgreSQL查询执行异常: {SqlStatement}", sqlStatement);
            }
        }

        private async Task ExecuteQueryOracleAsync(string sqlStatement, string connectionString, Dictionary<string, object>? parameters, QueryResult result)
        {
            try
            {
                // 为Oracle连接字符串添加超时参数
                var connectionStringBuilder = new Oracle.ManagedDataAccess.Client.OracleConnectionStringBuilder(connectionString);
                connectionStringBuilder.ConnectionTimeout = 30; // 30秒连接超时
                
                using var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(connectionStringBuilder.ConnectionString);
            await connection.OpenAsync();

            using var command = new Oracle.ManagedDataAccess.Client.OracleCommand(sqlStatement, connection);
                
                // 设置命令超时
                command.CommandTimeout = 60; // 60秒命令超时
                
                // 添加参数
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = param.Key;
                        parameter.Value = param.Value;
                        command.Parameters.Add(parameter);
                    }
                }
                
                _logger.LogInformation("开始执行Oracle查询，超时设置: {Timeout}s", command.CommandTimeout);
                
            using var reader = await command.ExecuteReaderAsync();

            // 获取列信息
            for (int i = 0; i < reader.FieldCount; i++)
            {
                result.Columns.Add(new SqlColumnInfo
                {
                    Name = reader.GetName(i),
                    DataType = reader.GetDataTypeName(i)
                });
            }

            // 读取数据
                var rowCount = 0;
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[reader.GetName(i)] = value;
                }
                result.Data.Add(row);
                    rowCount++;
                    
                    // 限制最大行数，防止内存溢出
                    if (rowCount >= 10000)
                    {
                        _logger.LogWarning("Oracle查询结果超过10000行，已截断");
                        break;
                    }
            }

            result.IsSuccess = true;
                _logger.LogInformation("Oracle查询执行完成，返回 {RowCount} 行数据", rowCount);
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Oracle错误: {ex.Message} (错误代码: {ex.Number})";
                _logger.LogError(ex, "Oracle查询执行失败: {SqlStatement}", sqlStatement);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"执行查询时发生错误: {ex.Message}";
                _logger.LogError(ex, "Oracle查询执行异常: {SqlStatement}", sqlStatement);
            }
        }

        /// <summary>
        /// 检查表是否存在
        /// </summary>
        private async Task<bool> CheckTableExistsAsync(string tableName, string connectionString)
        {
            try
            {
                var dataSourceType = GetDataSourceType(connectionString);
                
                switch (dataSourceType)
                {
                    case "sqlite":
                        return await CheckTableExistsSqliteAsync(tableName, connectionString);
                    case "mysql":
                        return await CheckTableExistsMySqlAsync(tableName, connectionString);
                    case "sqlserver":
                        return await CheckTableExistsSqlServerAsync(tableName, connectionString);
                    case "postgresql":
                        return await CheckTableExistsPostgreSqlAsync(tableName, connectionString);
                    case "oracle":
                        return await CheckTableExistsOracleAsync(tableName, connectionString);
                    default:
                        return await CheckTableExistsSqliteAsync(tableName, connectionString);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查表是否存在失败: {TableName}", tableName);
                return false;
            }
        }

        private async Task<bool> CheckTableExistsSqliteAsync(string tableName, string connectionString)
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var checkSql = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@TableName";
            using var command = new SQLiteCommand(checkSql, connection);
            command.Parameters.AddWithValue("@TableName", tableName);
            
            var count = await command.ExecuteScalarAsync();
            return Convert.ToInt32(count) > 0;
        }

        private async Task<bool> CheckTableExistsMySqlAsync(string tableName, string connectionString)
        {
            using var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            await connection.OpenAsync();

            var checkSql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @TableName";
            using var command = new MySql.Data.MySqlClient.MySqlCommand(checkSql, connection);
            command.Parameters.AddWithValue("@TableName", tableName);
            
            var count = await command.ExecuteScalarAsync();
            return Convert.ToInt32(count) > 0;
        }

        private async Task<bool> CheckTableExistsSqlServerAsync(string tableName, string connectionString)
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            var checkSql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = @TableName";
            using var command = new Microsoft.Data.SqlClient.SqlCommand(checkSql, connection);
            command.Parameters.AddWithValue("@TableName", tableName);
            
            var count = await command.ExecuteScalarAsync();
            return Convert.ToInt32(count) > 0;
        }

        private async Task<bool> CheckTableExistsPostgreSqlAsync(string tableName, string connectionString)
        {
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            var checkSql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = @TableName";
            using var command = new Npgsql.NpgsqlCommand(checkSql, connection);
            command.Parameters.AddWithValue("@TableName", tableName);
            
            var count = await command.ExecuteScalarAsync();
            return Convert.ToInt32(count) > 0;
        }

        private async Task<bool> CheckTableExistsOracleAsync(string tableName, string connectionString)
        {
            using var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString);
            await connection.OpenAsync();

            var checkSql = "SELECT COUNT(*) FROM user_tables WHERE table_name = :TableName";
            using var command = new Oracle.ManagedDataAccess.Client.OracleCommand(checkSql, connection);
            command.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter(":TableName", tableName));
            
            var count = await command.ExecuteScalarAsync();
            return Convert.ToInt32(count) > 0;
        }

        /// <summary>
        /// 根据查询结果创建表
        /// </summary>
        private async Task<bool> CreateTableFromQueryResultAsync(string tableName, List<SqlColumnInfo> columns, string connectionString)
        {
            try
            {
                var dataSourceType = GetDataSourceType(connectionString);
                
                switch (dataSourceType)
                {
                    case "sqlite":
                        return await CreateTableSqliteAsync(tableName, columns, connectionString);
                    case "mysql":
                        return await CreateTableMySqlAsync(tableName, columns, connectionString);
                    case "sqlserver":
                        return await CreateTableSqlServerAsync(tableName, columns, connectionString);
                    case "postgresql":
                        return await CreateTablePostgreSqlAsync(tableName, columns, connectionString);
                    case "oracle":
                        return await CreateTableOracleAsync(tableName, columns, connectionString);
                    default:
                        return await CreateTableSqliteAsync(tableName, columns, connectionString);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建表失败: {TableName}", tableName);
                return false;
            }
        }

        private async Task<bool> CreateTableSqliteAsync(string tableName, List<SqlColumnInfo> columns, string connectionString)
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            var createTableSql = BuildCreateTableSql(tableName, columns, connectionString);
            _logger.LogInformation("创建表SQL: {CreateTableSql}", createTableSql);

            using var command = new SQLiteCommand(createTableSql, connection);
            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("表 {TableName} 创建成功", tableName);
            return true;
        }

        private async Task<bool> CreateTableMySqlAsync(string tableName, List<SqlColumnInfo> columns, string connectionString)
        {
            using var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            await connection.OpenAsync();

            var createTableSql = BuildCreateTableSql(tableName, columns, connectionString);
            _logger.LogInformation("创建表SQL: {CreateTableSql}", createTableSql);

            using var command = new MySql.Data.MySqlClient.MySqlCommand(createTableSql, connection);
            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("表 {TableName} 创建成功", tableName);
            return true;
        }

        private async Task<bool> CreateTableSqlServerAsync(string tableName, List<SqlColumnInfo> columns, string connectionString)
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            var createTableSql = BuildCreateTableSql(tableName, columns, connectionString);
            _logger.LogInformation("创建表SQL: {CreateTableSql}", createTableSql);

            using var command = new Microsoft.Data.SqlClient.SqlCommand(createTableSql, connection);
            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("表 {TableName} 创建成功", tableName);
            return true;
        }

        private async Task<bool> CreateTablePostgreSqlAsync(string tableName, List<SqlColumnInfo> columns, string connectionString)
        {
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            var createTableSql = BuildCreateTableSql(tableName, columns, connectionString);
            _logger.LogInformation("创建表SQL: {CreateTableSql}", createTableSql);

            using var command = new Npgsql.NpgsqlCommand(createTableSql, connection);
            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("表 {TableName} 创建成功", tableName);
            return true;
        }

        private async Task<bool> CreateTableOracleAsync(string tableName, List<SqlColumnInfo> columns, string connectionString)
        {
            using var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString);
            await connection.OpenAsync();

            var createTableSql = BuildCreateTableSql(tableName, columns, connectionString);
            _logger.LogInformation("创建表SQL: {CreateTableSql}", createTableSql);

            using var command = new Oracle.ManagedDataAccess.Client.OracleCommand(createTableSql, connection);
            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("表 {TableName} 创建成功", tableName);
            return true;
        }

        /// <summary>
        /// 构建CREATE TABLE语句
        /// </summary>
        private string BuildCreateTableSql(string tableName, List<SqlColumnInfo> columns, string connectionString)
        {
            var dataSourceType = GetDataSourceType(connectionString);
            var columnDefinitions = new List<string>();

            foreach (var column in columns)
            {
                var sqlType = ConvertToSqlType(column.DataType, dataSourceType);
                var columnDef = $"[{column.Name}] {sqlType}";
                columnDefinitions.Add(columnDef);
            }

            var columnsSql = string.Join(", ", columnDefinitions);
            
            switch (dataSourceType)
            {
                case "sqlite":
                    return $"CREATE TABLE [{tableName}] (Id INTEGER PRIMARY KEY AUTOINCREMENT, {columnsSql})";
                case "mysql":
                    return $"CREATE TABLE `{tableName}` (Id INT AUTO_INCREMENT PRIMARY KEY, {columnsSql})";
                case "sqlserver":
                    return $"CREATE TABLE [{tableName}] (Id INT IDENTITY(1,1) PRIMARY KEY, {columnsSql})";
                case "postgresql":
                    return $"CREATE TABLE \"{tableName}\" (Id SERIAL PRIMARY KEY, {columnsSql})";
                case "oracle":
                    return $"CREATE TABLE {tableName} (Id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY, {columnsSql})";
                default:
                    return $"CREATE TABLE [{tableName}] (Id INTEGER PRIMARY KEY AUTOINCREMENT, {columnsSql})";
            }
        }

        /// <summary>
        /// 将.NET数据类型转换为SQL数据类型
        /// </summary>
        private string ConvertToSqlType(string dataType, string dataSourceType)
        {
            var type = dataType.ToLower();
            
            switch (dataSourceType)
            {
                case "sqlite":
                    return ConvertToSqliteType(type);
                case "mysql":
                    return ConvertToMySqlType(type);
                case "sqlserver":
                    return ConvertToSqlServerType(type);
                case "postgresql":
                    return ConvertToPostgreSqlType(type);
                case "oracle":
                    return ConvertToOracleType(type);
                default:
                    return ConvertToSqliteType(type);
            }
        }

        /// <summary>
        /// 转换为SQLite数据类型
        /// </summary>
        private string ConvertToSqliteType(string type)
        {
            return type switch
            {
                "integer" or "int" or "bigint" => "INTEGER",
                "real" or "float" or "double" => "REAL",
                "text" or "varchar" or "string" => "TEXT",
                "blob" => "BLOB",
                _ => "TEXT"
            };
        }

        /// <summary>
        /// 转换为MySQL数据类型
        /// </summary>
        private string ConvertToMySqlType(string type)
        {
            return type switch
            {
                "integer" or "int" => "INT",
                "bigint" => "BIGINT",
                "real" or "float" => "FLOAT",
                "double" => "DOUBLE",
                "text" or "varchar" or "string" => "TEXT",
                "blob" => "LONGBLOB",
                _ => "TEXT"
            };
        }

        /// <summary>
        /// 转换为SQL Server数据类型
        /// </summary>
        private string ConvertToSqlServerType(string type)
        {
            return type switch
            {
                "integer" or "int" => "INT",
                "bigint" => "BIGINT",
                "real" or "float" => "FLOAT",
                "double" => "FLOAT",
                "text" or "varchar" or "string" => "NVARCHAR(MAX)",
                "blob" => "VARBINARY(MAX)",
                _ => "NVARCHAR(MAX)"
            };
        }

        /// <summary>
        /// 转换为PostgreSQL数据类型
        /// </summary>
        private string ConvertToPostgreSqlType(string type)
        {
            return type switch
            {
                "integer" or "int" => "INTEGER",
                "bigint" => "BIGINT",
                "real" or "float" => "REAL",
                "double" => "DOUBLE PRECISION",
                "text" or "varchar" or "string" => "TEXT",
                "blob" => "BYTEA",
                _ => "TEXT"
            };
        }

        /// <summary>
        /// 转换为Oracle数据类型
        /// </summary>
        private string ConvertToOracleType(string type)
        {
            return type switch
            {
                "integer" or "int" => "NUMBER(10)",
                "bigint" => "NUMBER(19)",
                "real" or "float" => "BINARY_FLOAT",
                "double" => "BINARY_DOUBLE",
                "text" or "varchar" or "string" => "CLOB",
                "blob" => "BLOB",
                _ => "CLOB"
            };
        }

        /// <summary>
        /// 将数据插入到目标表
        /// </summary>
        private async Task<InsertResult> InsertDataToTableAsync(string tableName, List<Dictionary<string, object>> data, string connectionString, ISqlProgressCallback? progressCallback = null)
        {
            var result = new InsertResult();

            try
            {
                if (data.Count == 0)
                {
                    result.IsSuccess = true;
                    result.AffectedRows = 0;
                    return result;
                }

                var dataSourceType = GetDataSourceType(connectionString);
                
                switch (dataSourceType)
                {
                    case "sqlite":
                        return await InsertDataSqliteAsync(tableName, data, connectionString, progressCallback);
                    case "mysql":
                        return await InsertDataMySqlAsync(tableName, data, connectionString, progressCallback);
                    case "sqlserver":
                        return await InsertDataSqlServerAsync(tableName, data, connectionString, progressCallback);
                    case "postgresql":
                        return await InsertDataPostgreSqlAsync(tableName, data, connectionString, progressCallback);
                    case "oracle":
                        return await InsertDataOracleAsync(tableName, data, connectionString, progressCallback);
                    default:
                        return await InsertDataSqliteAsync(tableName, data, connectionString, progressCallback);
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "插入数据失败: {TableName}", tableName);
                return result;
            }
        }

        private async Task<InsertResult> InsertDataSqliteAsync(string tableName, List<Dictionary<string, object>> data, string connectionString, ISqlProgressCallback? progressCallback = null)
        {
            var result = new InsertResult();

            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            // 获取列名
            var columns = data[0].Keys.ToList();
            var columnsSql = string.Join(", ", columns.Select(c => $"[{c}]"));
            var parametersSql = string.Join(", ", columns.Select(c => $"@{c}"));

            var insertSql = $"INSERT INTO [{tableName}] ({columnsSql}) VALUES ({parametersSql})";
            _logger.LogInformation("插入SQL: {InsertSql}", insertSql);

            using var command = new SQLiteCommand(insertSql, connection);

            for (int i = 0; i < data.Count; i++)
            {
                var row = data[i];
                command.Parameters.Clear();
                foreach (var column in columns)
                {
                    var value = row.ContainsKey(column) ? row[column] : null;
                    command.Parameters.AddWithValue($"@{column}", value ?? DBNull.Value);
                }
                await command.ExecuteNonQueryAsync();
                result.AffectedRows++;
                
                // 更新进度
                UpdateInsertProgress(progressCallback, result.AffectedRows, data.Count);
                
                // 检查是否取消
                if (IsCancelled(progressCallback))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "操作已取消";
                    return result;
                }
            }

            result.IsSuccess = true;
            return result;
        }

        private async Task<InsertResult> InsertDataMySqlAsync(string tableName, List<Dictionary<string, object>> data, string connectionString, ISqlProgressCallback? progressCallback = null)
        {
            var result = new InsertResult();

            using var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            await connection.OpenAsync();

            // 获取列名
            var columns = data[0].Keys.ToList();
            var columnsSql = string.Join(", ", columns.Select(c => $"`{c}`"));
            var parametersSql = string.Join(", ", columns.Select(c => $"@{c}"));

            var insertSql = $"INSERT INTO `{tableName}` ({columnsSql}) VALUES ({parametersSql})";
            _logger.LogInformation("插入SQL: {InsertSql}", insertSql);

            using var command = new MySql.Data.MySqlClient.MySqlCommand(insertSql, connection);

            for (int i = 0; i < data.Count; i++)
            {
                var row = data[i];
                command.Parameters.Clear();
                foreach (var column in columns)
                {
                    var value = row.ContainsKey(column) ? row[column] : null;
                    command.Parameters.AddWithValue($"@{column}", value ?? DBNull.Value);
                }
                await command.ExecuteNonQueryAsync();
                result.AffectedRows++;
                
                // 更新进度
                UpdateInsertProgress(progressCallback, result.AffectedRows, data.Count);
                
                // 检查是否取消
                if (IsCancelled(progressCallback))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "操作已取消";
                    return result;
                }
            }

            result.IsSuccess = true;
            return result;
        }

        private async Task<InsertResult> InsertDataSqlServerAsync(string tableName, List<Dictionary<string, object>> data, string connectionString, ISqlProgressCallback? progressCallback = null)
        {
            var result = new InsertResult();

            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            // 获取列名
            var columns = data[0].Keys.ToList();
            var columnsSql = string.Join(", ", columns.Select(c => $"[{c}]"));
            var parametersSql = string.Join(", ", columns.Select(c => $"@{c}"));

            var insertSql = $"INSERT INTO [{tableName}] ({columnsSql}) VALUES ({parametersSql})";
            _logger.LogInformation("插入SQL: {InsertSql}", insertSql);

            using var command = new Microsoft.Data.SqlClient.SqlCommand(insertSql, connection);

            for (int i = 0; i < data.Count; i++)
            {
                var row = data[i];
                command.Parameters.Clear();
                foreach (var column in columns)
                {
                    var value = row.ContainsKey(column) ? row[column] : null;
                    command.Parameters.AddWithValue($"@{column}", value ?? DBNull.Value);
                }
                await command.ExecuteNonQueryAsync();
                result.AffectedRows++;
                
                // 更新进度
                UpdateInsertProgress(progressCallback, result.AffectedRows, data.Count);
                
                // 检查是否取消
                if (IsCancelled(progressCallback))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "操作已取消";
                    return result;
                }
            }

            result.IsSuccess = true;
            return result;
        }

        private async Task<InsertResult> InsertDataPostgreSqlAsync(string tableName, List<Dictionary<string, object>> data, string connectionString, ISqlProgressCallback? progressCallback = null)
        {
            var result = new InsertResult();

            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            // 获取列名
            var columns = data[0].Keys.ToList();
            var columnsSql = string.Join(", ", columns.Select(c => $"\"{c}\""));
            var parametersSql = string.Join(", ", columns.Select(c => $"@{c}"));

            var insertSql = $"INSERT INTO \"{tableName}\" ({columnsSql}) VALUES ({parametersSql})";
            _logger.LogInformation("插入SQL: {InsertSql}", insertSql);

            using var command = new Npgsql.NpgsqlCommand(insertSql, connection);

            for (int i = 0; i < data.Count; i++)
            {
                var row = data[i];
                command.Parameters.Clear();
                foreach (var column in columns)
                {
                    var value = row.ContainsKey(column) ? row[column] : null;
                    command.Parameters.AddWithValue($"@{column}", value ?? DBNull.Value);
                }
                await command.ExecuteNonQueryAsync();
                result.AffectedRows++;
                
                // 更新进度
                UpdateInsertProgress(progressCallback, result.AffectedRows, data.Count);
                
                // 检查是否取消
                if (IsCancelled(progressCallback))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "操作已取消";
                    return result;
                }
            }

            result.IsSuccess = true;
            return result;
        }

        private async Task<InsertResult> InsertDataOracleAsync(string tableName, List<Dictionary<string, object>> data, string connectionString, ISqlProgressCallback? progressCallback = null)
        {
            var result = new InsertResult();

            using var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString);
            await connection.OpenAsync();

            // 获取列名
            var columns = data[0].Keys.ToList();
            var columnsSql = string.Join(", ", columns.Select(c => $"{c}"));
            var parametersSql = string.Join(", ", columns.Select(c => $":{c}"));

            var insertSql = $"INSERT INTO {tableName} ({columnsSql}) VALUES ({parametersSql})";
            _logger.LogInformation("插入SQL: {InsertSql}", insertSql);

            using var command = new Oracle.ManagedDataAccess.Client.OracleCommand(insertSql, connection);

            for (int i = 0; i < data.Count; i++)
            {
                var row = data[i];
                command.Parameters.Clear();
                foreach (var column in columns)
                {
                    var value = row.ContainsKey(column) ? row[column] : null;
                    command.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter($":{column}", value ?? DBNull.Value));
                }
                await command.ExecuteNonQueryAsync();
                result.AffectedRows++;
                
                // 更新进度
                UpdateInsertProgress(progressCallback, result.AffectedRows, data.Count);
                
                // 检查是否取消
                if (IsCancelled(progressCallback))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "操作已取消";
                    return result;
                }
            }

            result.IsSuccess = true;
            return result;
        }

        #endregion

        #region 私有方法 - 表操作支持

        /// <summary>
        /// 清空表数据
        /// </summary>
        private async Task<bool> ClearTableAsync(string tableName, string connectionString)
        {
            try
            {
                var dataSourceType = GetDataSourceType(connectionString);
                string clearSql;

                switch (dataSourceType)
                {
                    case "sqlite":
                        clearSql = $"DELETE FROM {tableName}";
                        break;
                    case "mysql":
                        clearSql = $"DELETE FROM {tableName}";
                        break;
                    case "sqlserver":
                        clearSql = $"DELETE FROM {tableName}";
                        break;
                    case "postgresql":
                        clearSql = $"DELETE FROM {tableName}";
                        break;
                    case "oracle":
                        clearSql = $"DELETE FROM {tableName}";
                        break;
                    default:
                        clearSql = $"DELETE FROM {tableName}";
                        break;
                }

                // 使用具体的数据库连接类型来支持异步操作
                switch (dataSourceType)
                {
                    case "sqlite":
                        using (var connection = new SQLiteConnection(connectionString))
                        {
                            await connection.OpenAsync();
                            using var command = new SQLiteCommand(clearSql, connection);
                            await command.ExecuteNonQueryAsync();
                        }
                        break;
                    case "mysql":
                        using (var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString))
                        {
                            await connection.OpenAsync();
                            using var command = new MySql.Data.MySqlClient.MySqlCommand(clearSql, connection);
                            await command.ExecuteNonQueryAsync();
                        }
                        break;
                    case "sqlserver":
                        using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                        {
                            await connection.OpenAsync();
                            using var command = new Microsoft.Data.SqlClient.SqlCommand(clearSql, connection);
                            await command.ExecuteNonQueryAsync();
                        }
                        break;
                    case "postgresql":
                        using (var connection = new Npgsql.NpgsqlConnection(connectionString))
                        {
                            await connection.OpenAsync();
                            using var command = new Npgsql.NpgsqlCommand(clearSql, connection);
                            await command.ExecuteNonQueryAsync();
                        }
                        break;
                    case "oracle":
                        using (var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString))
                        {
                            await connection.OpenAsync();
                            using var command = new Oracle.ManagedDataAccess.Client.OracleCommand(clearSql, connection);
                            await command.ExecuteNonQueryAsync();
                        }
                        break;
                    default:
                        using (var connection = new SQLiteConnection(connectionString))
                        {
                            await connection.OpenAsync();
                            using var command = new SQLiteCommand(clearSql, connection);
                            await command.ExecuteNonQueryAsync();
                        }
                        break;
                }

                _logger.LogInformation("表 {TableName} 清空成功", tableName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空表 {TableName} 失败", tableName);
                return false;
            }
        }

        #endregion

        #region 私有方法 - 数据库连接支持

        /// <summary>
        /// 创建数据库连接
        /// </summary>
        private IDbConnection CreateConnection(string connectionString)
        {
            var dataSourceType = GetDataSourceType(connectionString);
            
            return dataSourceType switch
            {
                "sqlite" => new SQLiteConnection(connectionString),
                "mysql" => new MySql.Data.MySqlClient.MySqlConnection(connectionString),
                "sqlserver" => new Microsoft.Data.SqlClient.SqlConnection(connectionString),
                "postgresql" => new Npgsql.NpgsqlConnection(connectionString),
                "oracle" => new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString),
                _ => new SQLiteConnection(connectionString)
            };
        }

        /// <summary>
        /// 创建数据库命令
        /// </summary>
        private IDbCommand CreateCommand(string sql, IDbConnection connection)
        {
            var dataSourceType = GetDataSourceType(connection.ConnectionString);
            
            return dataSourceType switch
            {
                "sqlite" => new SQLiteCommand(sql, (SQLiteConnection)connection),
                "mysql" => new MySql.Data.MySqlClient.MySqlCommand(sql, (MySql.Data.MySqlClient.MySqlConnection)connection),
                "sqlserver" => new Microsoft.Data.SqlClient.SqlCommand(sql, (Microsoft.Data.SqlClient.SqlConnection)connection),
                "postgresql" => new Npgsql.NpgsqlCommand(sql, (Npgsql.NpgsqlConnection)connection),
                "oracle" => new Oracle.ManagedDataAccess.Client.OracleCommand(sql, (Oracle.ManagedDataAccess.Client.OracleConnection)connection),
                _ => new SQLiteCommand(sql, (SQLiteConnection)connection)
            };
        }

        /// <summary>
        /// 添加参数到命令
        /// </summary>
        private void AddParameter(IDbCommand command, string parameterName, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        #endregion

        /// <summary>
        /// 更新插入进度
        /// </summary>
        private void UpdateInsertProgress(ISqlProgressCallback? progressCallback, int currentRow, int totalRows)
        {
            progressCallback?.UpdateStatistics(currentRow, totalRows);
            progressCallback?.UpdateSubDetailMessage($"已插入 {currentRow:N0} / {totalRows:N0} 行");
        }

        /// <summary>
        /// 检查是否取消操作
        /// </summary>
        private bool IsCancelled(ISqlProgressCallback? progressCallback)
        {
            return progressCallback?.IsCancelled() == true;
        }

        /// <summary>
        /// 根据数据源ID获取数据源配置
        /// </summary>
        /// <param name="dataSourceId">数据源ID</param>
        /// <returns>数据源配置</returns>
        public async Task<DataSourceConfig?> GetDataSourceByIdAsync(string dataSourceId)
        {
            try
            {
                return await _dataSourceService.GetDataSourceByIdAsync(dataSourceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取数据源配置失败: {DataSourceId}", dataSourceId);
                throw;
            }
        }

        /// <summary>
        /// 执行SQL语句
        /// </summary>
        /// <param name="sqlStatement">SQL语句</param>
        /// <param name="connectionString">连接字符串</param>
        /// <returns>执行结果</returns>
        public async Task<SqlExecutionResult> ExecuteSqlAsync(string sqlStatement, string connectionString)
        {
            var startTime = DateTime.Now;
            try
            {
                using var connection = CreateConnection(connectionString);
                connection.Open();

                using var command = CreateCommand(sqlStatement, connection);
                var affectedRows = command.ExecuteNonQuery();

                var executionTime = (DateTime.Now - startTime).TotalMilliseconds;

                return new SqlExecutionResult
                {
                    Id = Guid.NewGuid().ToString(),
                    SqlConfigId = string.Empty,
                    Status = "成功",
                    StartTime = startTime,
                    EndTime = DateTime.Now,
                    Duration = (long)executionTime,
                    AffectedRows = affectedRows,
                    ErrorMessage = null
                };
            }
            catch (Exception ex)
            {
                var executionTime = (DateTime.Now - startTime).TotalMilliseconds;
                _logger.LogError(ex, "执行SQL语句失败: {SqlStatement}", sqlStatement);

                return new SqlExecutionResult
                {
                    Id = Guid.NewGuid().ToString(),
                    SqlConfigId = string.Empty,
                    Status = "失败",
                    StartTime = startTime,
                    EndTime = DateTime.Now,
                    Duration = (long)executionTime,
                    AffectedRows = 0,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
} 