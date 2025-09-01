using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Models;

namespace ExcelProcessor.Examples
{
    /// <summary>
    /// 数据库连接使用示例
    /// </summary>
    public class DatabaseConnectionExample
    {
        private readonly IDataSourceService _dataSourceService;
        private readonly ILogger<DatabaseConnectionExample> _logger;

        public DatabaseConnectionExample(IDataSourceService dataSourceService, ILogger<DatabaseConnectionExample> logger)
        {
            _dataSourceService = dataSourceService;
            _logger = logger;
        }

        /// <summary>
        /// 运行完整的数据库连接示例
        /// </summary>
        public async Task RunExampleAsync()
        {
            Console.WriteLine("=== 数据库连接功能演示 ===\n");

            // 1. 演示各种数据库连接测试
            await TestAllDatabaseConnections();

            // 2. 演示数据源管理
            await DemoDataSourceManagement();

            Console.WriteLine("\n=== 演示完成 ===");
        }

        /// <summary>
        /// 测试所有数据库类型的连接
        /// </summary>
        private async Task TestAllDatabaseConnections()
        {
            Console.WriteLine("1. 测试各种数据库连接...\n");

            // MySQL连接测试
            await TestDatabaseConnection("MySQL", "Server=localhost;Port=3306;Database=testdb;Uid=root;Pwd=password;");

            // SQL Server连接测试
            await TestDatabaseConnection("SQLServer", "Server=localhost,1433;Database=testdb;User Id=sa;Password=password;");

            // PostgreSQL连接测试
            await TestDatabaseConnection("PostgreSQL", "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=password;");

            // Oracle连接测试
            await TestDatabaseConnection("Oracle", "Data Source=localhost:1521/XE;User Id=system;Password=password;");

            // SQLite连接测试
            await TestDatabaseConnection("SQLite", "Data Source=:memory:;Version=3;");
        }

        /// <summary>
        /// 测试单个数据库连接
        /// </summary>
        private async Task TestDatabaseConnection(string databaseType, string connectionString)
        {
            var dataSource = new DataSourceConfig
            {
                Name = $"{databaseType}测试连接",
                Type = databaseType,
                ConnectionString = connectionString
            };

            Console.Write($"测试 {databaseType} 连接... ");

            try
            {
                var (isConnected, errorMessage) = await _dataSourceService.TestConnectionWithDetailsAsync(dataSource);
                
                if (isConnected)
                {
                    Console.WriteLine("✅ 成功");
                }
                else
                {
                    Console.WriteLine($"❌ 失败: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 演示数据源管理功能
        /// </summary>
        private async Task DemoDataSourceManagement()
        {
            Console.WriteLine("\n2. 数据源管理演示...\n");

            // 创建测试数据源
            var testDataSource = new DataSourceConfig
            {
                Name = "演示数据源",
                Type = "SQLite",
                Description = "这是一个演示用的SQLite数据源",
                ConnectionString = "Data Source=demo.db;Version=3;",
                Status = "未测试"
            };

            try
            {
                // 保存数据源
                Console.Write("保存数据源... ");
                var saveResult = await _dataSourceService.SaveDataSourceAsync(testDataSource);
                Console.WriteLine(saveResult ? "✅ 成功" : "❌ 失败");

                if (saveResult)
                {
                    // 获取所有数据源
                    Console.Write("获取数据源列表... ");
                    var dataSources = await _dataSourceService.GetAllDataSourcesAsync();
                    Console.WriteLine($"✅ 成功，共 {dataSources.Count} 个数据源");

                    // 测试连接
                    Console.Write("测试数据源连接... ");
                    var (isConnected, errorMessage) = await _dataSourceService.TestConnectionWithDetailsAsync(testDataSource);
                    Console.WriteLine(isConnected ? "✅ 成功" : $"❌ 失败: {errorMessage}");

                    // 更新数据源状态
                    if (isConnected)
                    {
                        testDataSource.Status = "已连接";
                        testDataSource.IsConnected = true;
                        testDataSource.LastTestTime = DateTime.Now;

                        Console.Write("更新数据源状态... ");
                        var updateResult = await _dataSourceService.UpdateDataSourceAsync(testDataSource);
                        Console.WriteLine(updateResult ? "✅ 成功" : "❌ 失败");
                    }

                    // 删除测试数据源
                    Console.Write("删除测试数据源... ");
                    var deleteResult = await _dataSourceService.DeleteDataSourceAsync(testDataSource.Id);
                    Console.WriteLine(deleteResult ? "✅ 成功" : "❌ 失败");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 数据源管理演示异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 演示连接字符串构建
        /// </summary>
        public void DemoConnectionStringBuilding()
        {
            Console.WriteLine("\n3. 连接字符串构建演示...\n");

            // 演示各种连接字符串格式
            var examples = new[]
            {
                new { Type = "MySQL", ConnectionString = "Server=localhost;Port=3306;Database=testdb;Uid=root;Pwd=password;" },
                new { Type = "SQL Server", ConnectionString = "Server=localhost,1433;Database=testdb;User Id=sa;Password=password;" },
                new { Type = "PostgreSQL", ConnectionString = "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=password;" },
                new { Type = "Oracle", ConnectionString = "Data Source=localhost:1521/XE;User Id=system;Password=password;" },
                new { Type = "SQLite", ConnectionString = "Data Source=test.db;Version=3;" }
            };

            foreach (var example in examples)
            {
                Console.WriteLine($"{example.Type}:");
                Console.WriteLine($"  {example.ConnectionString}");
                Console.WriteLine();
            }
        }
    }
} 
 
 
 
 
 