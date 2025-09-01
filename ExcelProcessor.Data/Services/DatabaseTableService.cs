using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Microsoft.Data.SqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// 数据库表服务实现
    /// </summary>
    public class DatabaseTableService : IDatabaseTableService
    {
        private readonly ILogger<DatabaseTableService> _logger;
        private readonly IDataSourceService _dataSourceService;

        public DatabaseTableService(ILogger<DatabaseTableService> logger, IDataSourceService dataSourceService)
        {
            _logger = logger;
            _dataSourceService = dataSourceService;
        }

        /// <summary>
        /// 根据数据源名称获取所有表名
        /// </summary>
        public async Task<List<string>> GetTableNamesAsync(string dataSourceName)
        {
            try
            {
                // 获取数据源配置
                var dataSource = await _dataSourceService.GetDataSourceByNameAsync(dataSourceName);
                if (dataSource == null)
                {
                    _logger.LogWarning("数据源不存在: {DataSourceName}", dataSourceName);
                    return new List<string>();
                }

                // 根据数据源类型获取表名
                switch (dataSource.Type.ToLower())
                {
                    case "sqlite":
                        return await GetSQLiteTableNamesAsync(dataSource.ConnectionString);
                    case "mysql":
                        return await GetMySQLTableNamesAsync(dataSource.ConnectionString);
                    case "sqlserver":
                        return await GetSQLServerTableNamesAsync(dataSource.ConnectionString);
                    case "postgresql":
                        return await GetPostgreSQLTableNamesAsync(dataSource.ConnectionString);
                    case "oracle":
                        return await GetOracleTableNamesAsync(dataSource.ConnectionString);
                    default:
                        _logger.LogWarning("不支持的数据源类型: {Type}", dataSource.Type);
                        return new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取表名失败: {DataSourceName}", dataSourceName);
                return new List<string>();
            }
        }

        /// <summary>
        /// 根据数据源名称和搜索关键词获取匹配的表名
        /// </summary>
        public async Task<List<string>> SearchTableNamesAsync(string dataSourceName, string searchKeyword)
        {
            try
            {
                var allTableNames = await GetTableNamesAsync(dataSourceName);
                
                if (string.IsNullOrWhiteSpace(searchKeyword))
                {
                    return allTableNames;
                }

                // 使用不区分大小写的搜索
                return allTableNames
                    .Where(tableName => tableName.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索表名失败: {DataSourceName}, {SearchKeyword}", dataSourceName, searchKeyword);
                return new List<string>();
            }
        }

        #region 私有方法 - 不同数据库类型的表名获取

        /// <summary>
        /// 获取SQLite表名
        /// </summary>
        private async Task<List<string>> GetSQLiteTableNamesAsync(string connectionString)
        {
            try
            {
                using var connection = new SQLiteConnection(connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT name 
                    FROM sqlite_master 
                    WHERE type='table' 
                    AND name NOT LIKE 'sqlite_%'
                    ORDER BY name";

                var tableNames = await connection.QueryAsync<string>(sql);
                return tableNames.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取SQLite表名失败");
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取MySQL表名
        /// </summary>
        private async Task<List<string>> GetMySQLTableNamesAsync(string connectionString)
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = DATABASE()
                    ORDER BY TABLE_NAME";

                var tableNames = await connection.QueryAsync<string>(sql);
                return tableNames.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取MySQL表名失败");
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取SQL Server表名
        /// </summary>
        private async Task<List<string>> GetSQLServerTableNamesAsync(string connectionString)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_TYPE = 'BASE TABLE'
                    ORDER BY TABLE_NAME";

                var tableNames = await connection.QueryAsync<string>(sql);
                return tableNames.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取SQL Server表名失败");
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取PostgreSQL表名
        /// </summary>
        private async Task<List<string>> GetPostgreSQLTableNamesAsync(string connectionString)
        {
            try
            {
                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public'
                    ORDER BY table_name";

                var tableNames = await connection.QueryAsync<string>(sql);
                return tableNames.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取PostgreSQL表名失败");
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取Oracle表名
        /// </summary>
        private async Task<List<string>> GetOracleTableNamesAsync(string connectionString)
        {
            try
            {
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT table_name 
                    FROM user_tables 
                    ORDER BY table_name";

                var tableNames = await connection.QueryAsync<string>(sql);
                return tableNames.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取Oracle表名失败");
                return new List<string>();
            }
        }

        #endregion
    }
} 