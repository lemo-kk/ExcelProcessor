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
    /// 数据源服务实现
    /// </summary>
    public class DataSourceService : IDataSourceService
    {
        private readonly ILogger<DataSourceService> _logger;
        private readonly string _connectionString;

        public DataSourceService(ILogger<DataSourceService> logger, string connectionString)
        {
            _logger = logger;
            _connectionString = connectionString;
        }

        /// <summary>
        /// 获取所有数据源配置
        /// </summary>
        public async Task<List<DataSourceConfig>> GetAllDataSourcesAsync()
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT Id, Name, Type, Description, ConnectionString, 
                           IsConnected, Status, LastTestTime, IsEnabled, IsDefault,
                           CreatedTime, UpdatedTime
                    FROM DataSourceConfig 
                    ORDER BY CreatedTime DESC";

                var dataSources = await connection.QueryAsync<DataSourceConfig>(sql);
                
                // 处理LastTestTime的转换
                foreach (var dataSource in dataSources)
                {
                    if (!string.IsNullOrEmpty(dataSource.Status) && dataSource.Status == "未连接")
                    {
                        dataSource.LastTestTime = DateTime.MinValue;
                    }
                }
                
                return dataSources.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有数据源配置失败");
                throw;
            }
        }

        /// <summary>
        /// 根据ID获取数据源配置
        /// </summary>
        public async Task<DataSourceConfig> GetDataSourceByIdAsync(string id)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT Id, Name, Type, Description, ConnectionString, 
                           IsConnected, Status, LastTestTime, IsEnabled, IsDefault,
                           CreatedTime, UpdatedTime
                    FROM DataSourceConfig 
                    WHERE Id = @Id";

                var dataSource = await connection.QueryFirstOrDefaultAsync<DataSourceConfig>(sql, new { Id = id });
                
                // 处理LastTestTime的转换
                if (dataSource != null && !string.IsNullOrEmpty(dataSource.Status) && dataSource.Status == "未连接")
                {
                    dataSource.LastTestTime = DateTime.MinValue;
                }
                
                return dataSource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取数据源配置失败: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根据名称获取数据源配置
        /// </summary>
        public async Task<DataSourceConfig> GetDataSourceByNameAsync(string name)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT Id, Name, Type, Description, ConnectionString, 
                           IsConnected, Status, LastTestTime, IsEnabled, 
                           CreatedTime, UpdatedTime
                    FROM DataSourceConfig 
                    WHERE Name = @Name";

                var dataSource = await connection.QueryFirstOrDefaultAsync<DataSourceConfig>(sql, new { Name = name });
                
                if (dataSource == null)
                {
                    throw new InvalidOperationException($"未找到名称为 '{name}' 的数据源配置");
                }
                
                // 处理LastTestTime的转换
                if (!string.IsNullOrEmpty(dataSource.Status) && dataSource.Status == "未连接")
                {
                    dataSource.LastTestTime = DateTime.MinValue;
                }
                
                return dataSource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据名称获取数据源配置失败: {Name}", name);
                throw;
            }
        }

        /// <summary>
        /// 保存数据源配置
        /// </summary>
        public async Task<bool> SaveDataSourceAsync(DataSourceConfig dataSource)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO DataSourceConfig 
                    (Id, Name, Type, Description, ConnectionString, IsConnected, Status, 
                     LastTestTime, IsEnabled, IsDefault, CreatedTime, UpdatedTime)
                    VALUES 
                    (@Id, @Name, @Type, @Description, @ConnectionString, @IsConnected, @Status,
                     @LastTestTime, @IsEnabled, @IsDefault, @CreatedTime, @UpdatedTime)";

                dataSource.CreatedTime = DateTime.Now;
                dataSource.UpdatedTime = DateTime.Now;

                var result = await connection.ExecuteAsync(sql, dataSource);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存数据源配置失败: {Name}", dataSource.Name);
                throw;
            }
        }

        /// <summary>
        /// 更新数据源配置
        /// </summary>
        public async Task<bool> UpdateDataSourceAsync(DataSourceConfig dataSource)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE DataSourceConfig 
                    SET Name = @Name, Type = @Type, Description = @Description, 
                        ConnectionString = @ConnectionString, IsConnected = @IsConnected, 
                        Status = @Status, LastTestTime = @LastTestTime, IsEnabled = @IsEnabled,
                        IsDefault = @IsDefault, UpdatedTime = @UpdatedTime
                    WHERE Id = @Id";

                dataSource.UpdatedTime = DateTime.Now;

                var result = await connection.ExecuteAsync(sql, dataSource);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新数据源配置失败: {Id}", dataSource.Id);
                throw;
            }
        }

        /// <summary>
        /// 删除数据源配置
        /// </summary>
        public async Task<bool> DeleteDataSourceAsync(string id)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "DELETE FROM DataSourceConfig WHERE Id = @Id";
                var result = await connection.ExecuteAsync(sql, new { Id = id });
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除数据源配置失败: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 测试数据源连接
        /// </summary>
        public async Task<bool> TestConnectionAsync(DataSourceConfig dataSource)
        {
            try
            {
                _logger.LogInformation("开始测试数据源连接: {Name}", dataSource.Name);

                switch (dataSource.Type.ToLower())
                {
                    case "sqlite":
                        return await TestSQLiteConnectionAsync(dataSource);
                    case "mysql":
                        return await TestMySQLConnectionAsync(dataSource);
                    case "sqlserver":
                        return await TestSQLServerConnectionAsync(dataSource);
                    case "postgresql":
                        return await TestPostgreSQLConnectionAsync(dataSource);
                    case "oracle":
                        return await TestOracleConnectionAsync(dataSource);
                    default:
                        _logger.LogWarning("不支持的数据源类型: {Type}", dataSource.Type);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试数据源连接失败: {Name}", dataSource.Name);
                return false;
            }
        }

        /// <summary>
        /// 测试数据源连接并返回详细错误信息
        /// </summary>
        public async Task<(bool isConnected, string errorMessage)> TestConnectionWithDetailsAsync(DataSourceConfig dataSource)
        {
            try
            {
                _logger.LogInformation("开始测试数据源连接: {Name}", dataSource.Name);

                switch (dataSource.Type.ToLower())
                {
                    case "sqlite":
                        return await TestSQLiteConnectionWithDetailsAsync(dataSource);
                    case "mysql":
                        return await TestMySQLConnectionWithDetailsAsync(dataSource);
                    case "sqlserver":
                        return await TestSQLServerConnectionWithDetailsAsync(dataSource);
                    case "postgresql":
                        return await TestPostgreSQLConnectionWithDetailsAsync(dataSource);
                    case "oracle":
                        return await TestOracleConnectionWithDetailsAsync(dataSource);
                    default:
                        var errorMsg = $"不支持的数据源类型: {dataSource.Type}";
                        _logger.LogWarning(errorMsg);
                        return (false, errorMsg);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"测试数据源连接失败: {ex.Message}";
                _logger.LogError(ex, errorMsg);
                return (false, errorMsg);
            }
        }

        /// <summary>
        /// 批量测试所有数据源连接
        /// </summary>
        public async Task<List<DataSourceConfig>> TestAllConnectionsAsync()
        {
            try
            {
                var dataSources = await GetAllDataSourcesAsync();
                var updatedDataSources = new List<DataSourceConfig>();

                foreach (var dataSource in dataSources)
                {
                    var (isConnected, errorMessage) = await TestConnectionWithDetailsAsync(dataSource);
                    dataSource.IsConnected = isConnected;
                    dataSource.Status = isConnected ? "已连接" : "连接失败";
                    dataSource.LastTestTime = DateTime.Now;

                    // 记录详细的错误信息
                    if (!isConnected && !string.IsNullOrEmpty(errorMessage))
                    {
                        _logger.LogWarning("数据源 {Name} 连接失败: {ErrorMessage}", dataSource.Name, errorMessage);
                    }

                    // 更新数据库中的状态
                    await UpdateDataSourceAsync(dataSource);
                    updatedDataSources.Add(dataSource);
                }

                return updatedDataSources;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量测试数据源连接失败");
                throw;
            }
        }

        /// <summary>
        /// 检查数据源名称是否已存在
        /// </summary>
        public async Task<bool> IsDataSourceNameExistsAsync(string name, string? excludeId = null)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                string sql;
                object parameters;

                if (string.IsNullOrEmpty(excludeId))
                {
                    sql = "SELECT COUNT(1) FROM DataSourceConfig WHERE Name = @Name";
                    parameters = new { Name = name };
                }
                else
                {
                    sql = "SELECT COUNT(1) FROM DataSourceConfig WHERE Name = @Name AND Id != @ExcludeId";
                    parameters = new { Name = name, ExcludeId = excludeId };
                }

                var count = await connection.ExecuteScalarAsync<int>(sql, parameters);
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查数据源名称是否存在失败: {Name}", name);
                throw;
            }
        }

        #region 私有方法 - 连接测试

        private async Task<bool> TestSQLiteConnectionAsync(DataSourceConfig dataSource)
        {
            try
            {
                using var connection = new SQLiteConnection(dataSource.ConnectionString);
                await connection.OpenAsync();
                
                // 执行简单查询测试连接
                using var command = new SQLiteCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQLite连接测试失败: {Name}", dataSource.Name);
                return false;
            }
        }

        private async Task<(bool isConnected, string errorMessage)> TestSQLiteConnectionWithDetailsAsync(DataSourceConfig dataSource)
        {
            try
            {
                using var connection = new SQLiteConnection(dataSource.ConnectionString);
                await connection.OpenAsync();
                
                // 执行简单查询测试连接
                using var command = new SQLiteCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();
                
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQLite连接测试失败: {Name}", dataSource.Name);
                return (false, ex.Message);
            }
        }

        private async Task<bool> TestMySQLConnectionAsync(DataSourceConfig dataSource)
        {
            try
            {
                using var connection = new MySqlConnection(dataSource.ConnectionString);
                await connection.OpenAsync();
                
                // 执行简单查询测试连接
                using var command = new MySqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MySQL连接测试失败: {Name}", dataSource.Name);
                return false;
            }
        }

        private async Task<(bool isConnected, string errorMessage)> TestMySQLConnectionWithDetailsAsync(DataSourceConfig dataSource)
        {
            try
            {
                using var connection = new MySqlConnection(dataSource.ConnectionString);
                await connection.OpenAsync();
                
                // 执行简单查询测试连接
                using var command = new MySqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();
                
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MySQL连接测试失败: {Name}", dataSource.Name);
                return (false, ex.Message);
            }
        }

        private async Task<bool> TestSQLServerConnectionAsync(DataSourceConfig dataSource)
        {
            try
            {
                using var connection = new SqlConnection(dataSource.ConnectionString);
                await connection.OpenAsync();
                
                // 执行简单查询测试连接
                using var command = new SqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL Server连接测试失败: {Name}", dataSource.Name);
                return false;
            }
        }

        private async Task<(bool isConnected, string errorMessage)> TestSQLServerConnectionWithDetailsAsync(DataSourceConfig dataSource)
        {
            try
            {
                using var connection = new SqlConnection(dataSource.ConnectionString);
                await connection.OpenAsync();
                
                // 执行简单查询测试连接
                using var command = new SqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();
                
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL Server连接测试失败: {Name}", dataSource.Name);
                return (false, ex.Message);
            }
        }

        private async Task<bool> TestPostgreSQLConnectionAsync(DataSourceConfig dataSource)
        {
            try
            {
                using var connection = new NpgsqlConnection(dataSource.ConnectionString);
                await connection.OpenAsync();
                
                // 执行简单查询测试连接
                using var command = new NpgsqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgreSQL连接测试失败: {Name}", dataSource.Name);
                return false;
            }
        }

        private async Task<(bool isConnected, string errorMessage)> TestPostgreSQLConnectionWithDetailsAsync(DataSourceConfig dataSource)
        {
            try
            {
                using var connection = new NpgsqlConnection(dataSource.ConnectionString);
                await connection.OpenAsync();
                
                // 执行简单查询测试连接
                using var command = new NpgsqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();
                
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgreSQL连接测试失败: {Name}", dataSource.Name);
                return (false, ex.Message);
            }
        }

        private async Task<bool> TestOracleConnectionAsync(DataSourceConfig dataSource)
        {
            try
            {
                using var connection = new OracleConnection(dataSource.ConnectionString);
                await connection.OpenAsync();
                
                // 执行简单查询测试连接
                using var command = new OracleCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Oracle连接测试失败: {Name}", dataSource.Name);
                return false;
            }
        }

        private async Task<(bool isConnected, string errorMessage)> TestOracleConnectionWithDetailsAsync(DataSourceConfig dataSource)
        {
            try
            {
                using var connection = new OracleConnection(dataSource.ConnectionString);
                await connection.OpenAsync();
                
                // 执行简单查询测试连接
                using var command = new OracleCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();
                
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Oracle连接测试失败: {Name}", dataSource.Name);
                return (false, ex.Message);
            }
        }

        #endregion
    }
} 