using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Models;

namespace ExcelProcessor.Data.Demo
{
    /// <summary>
    /// 数据库连接演示类
    /// </summary>
    public class DatabaseConnectionDemo
    {
        private readonly IDataSourceService _dataSourceService;
        private readonly ILogger<DatabaseConnectionDemo> _logger;

        public DatabaseConnectionDemo(IDataSourceService dataSourceService, ILogger<DatabaseConnectionDemo> logger)
        {
            _dataSourceService = dataSourceService;
            _logger = logger;
        }

        /// <summary>
        /// 演示各种数据库连接测试
        /// </summary>
        public async Task RunDemoAsync()
        {
            _logger.LogInformation("开始数据库连接演示...");

            // 演示MySQL连接
            await DemoMySQLConnection();

            // 演示SQL Server连接
            await DemoSQLServerConnection();

            // 演示PostgreSQL连接
            await DemoPostgreSQLConnection();

            // 演示Oracle连接
            await DemoOracleConnection();

            // 演示SQLite连接
            await DemoSQLiteConnection();

            _logger.LogInformation("数据库连接演示完成");
        }

        private async Task DemoMySQLConnection()
        {
            _logger.LogInformation("=== MySQL连接演示 ===");

            var dataSource = new DataSourceConfig
            {
                Name = "MySQL演示连接",
                Type = "MySQL",
                ConnectionString = "Server=localhost;Port=3306;Database=testdb;Uid=root;Pwd=password;"
            };

            try
            {
                var (isConnected, errorMessage) = await _dataSourceService.TestConnectionWithDetailsAsync(dataSource);
                
                if (isConnected)
                {
                    _logger.LogInformation("✅ MySQL连接成功");
                }
                else
                {
                    _logger.LogWarning("❌ MySQL连接失败: {ErrorMessage}", errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MySQL连接测试异常");
            }
        }

        private async Task DemoSQLServerConnection()
        {
            _logger.LogInformation("=== SQL Server连接演示 ===");

            var dataSource = new DataSourceConfig
            {
                Name = "SQL Server演示连接",
                Type = "SQLServer",
                ConnectionString = "Server=localhost,1433;Database=testdb;User Id=sa;Password=password;"
            };

            try
            {
                var (isConnected, errorMessage) = await _dataSourceService.TestConnectionWithDetailsAsync(dataSource);
                
                if (isConnected)
                {
                    _logger.LogInformation("✅ SQL Server连接成功");
                }
                else
                {
                    _logger.LogWarning("❌ SQL Server连接失败: {ErrorMessage}", errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL Server连接测试异常");
            }
        }

        private async Task DemoPostgreSQLConnection()
        {
            _logger.LogInformation("=== PostgreSQL连接演示 ===");

            var dataSource = new DataSourceConfig
            {
                Name = "PostgreSQL演示连接",
                Type = "PostgreSQL",
                ConnectionString = "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=password;"
            };

            try
            {
                var (isConnected, errorMessage) = await _dataSourceService.TestConnectionWithDetailsAsync(dataSource);
                
                if (isConnected)
                {
                    _logger.LogInformation("✅ PostgreSQL连接成功");
                }
                else
                {
                    _logger.LogWarning("❌ PostgreSQL连接失败: {ErrorMessage}", errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgreSQL连接测试异常");
            }
        }

        private async Task DemoOracleConnection()
        {
            _logger.LogInformation("=== Oracle连接演示 ===");

            var dataSource = new DataSourceConfig
            {
                Name = "Oracle演示连接",
                Type = "Oracle",
                ConnectionString = "Data Source=localhost:1521/XE;User Id=system;Password=password;"
            };

            try
            {
                var (isConnected, errorMessage) = await _dataSourceService.TestConnectionWithDetailsAsync(dataSource);
                
                if (isConnected)
                {
                    _logger.LogInformation("✅ Oracle连接成功");
                }
                else
                {
                    _logger.LogWarning("❌ Oracle连接失败: {ErrorMessage}", errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Oracle连接测试异常");
            }
        }

        private async Task DemoSQLiteConnection()
        {
            _logger.LogInformation("=== SQLite连接演示 ===");

            var dataSource = new DataSourceConfig
            {
                Name = "SQLite演示连接",
                Type = "SQLite",
                ConnectionString = "Data Source=:memory:;Version=3;"
            };

            try
            {
                var (isConnected, errorMessage) = await _dataSourceService.TestConnectionWithDetailsAsync(dataSource);
                
                if (isConnected)
                {
                    _logger.LogInformation("✅ SQLite连接成功");
                }
                else
                {
                    _logger.LogWarning("❌ SQLite连接失败: {ErrorMessage}", errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQLite连接测试异常");
            }
        }

        /// <summary>
        /// 演示连接字符串构建
        /// </summary>
        public void DemoConnectionStringBuilding()
        {
            _logger.LogInformation("=== 连接字符串构建演示 ===");

            // MySQL连接字符串示例
            var mysqlConnectionString = "Server=localhost;Port=3306;Database=testdb;Uid=root;Pwd=password;";
            _logger.LogInformation("MySQL连接字符串: {ConnectionString}", mysqlConnectionString);

            // SQL Server连接字符串示例
            var sqlServerConnectionString = "Server=localhost,1433;Database=testdb;User Id=sa;Password=password;";
            _logger.LogInformation("SQL Server连接字符串: {ConnectionString}", sqlServerConnectionString);

            // PostgreSQL连接字符串示例
            var postgresqlConnectionString = "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=password;";
            _logger.LogInformation("PostgreSQL连接字符串: {ConnectionString}", postgresqlConnectionString);

            // Oracle连接字符串示例
            var oracleConnectionString = "Data Source=localhost:1521/XE;User Id=system;Password=password;";
            _logger.LogInformation("Oracle连接字符串: {ConnectionString}", oracleConnectionString);

            // SQLite连接字符串示例
            var sqliteConnectionString = "Data Source=test.db;Version=3;";
            _logger.LogInformation("SQLite连接字符串: {ConnectionString}", sqliteConnectionString);
        }
    }
} 
 
 
 
 
 