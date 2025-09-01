using System;
using System.Linq;
using System.Threading.Tasks;
using ExcelProcessor.Data.Services;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace ExcelProcessor.Demo
{
    /// <summary>
    /// 默认数据库功能演示
    /// </summary>
    public class DefaultDatabaseDemo
    {
        private readonly DataSourceService _dataSourceService;
        private readonly ILogger<DefaultDatabaseDemo> _logger;

        public DefaultDatabaseDemo()
        {
            // 创建日志记录器
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            _logger = loggerFactory.CreateLogger<DefaultDatabaseDemo>();

            // 创建数据源服务
            var connectionString = "Data Source=./data/excel_processor.db;Version=3;";
            _dataSourceService = new DataSourceService(_logger, connectionString);
        }

        /// <summary>
        /// 运行演示
        /// </summary>
        public async Task RunDemo()
        {
            Console.WriteLine("=== 默认数据库功能演示 ===\n");

            try
            {
                // 1. 显示当前数据源
                await ShowCurrentDataSources();

                // 2. 创建测试数据源
                await CreateTestDataSources();

                // 3. 测试设置默认数据库
                await TestSetDefaultDatabase();

                // 4. 测试唯一性保证
                await TestUniquenessConstraint();

                // 5. 测试取消默认数据库
                await TestRemoveDefaultDatabase();

                Console.WriteLine("\n=== 演示完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"演示过程中发生错误: {ex.Message}");
                _logger.LogError(ex, "默认数据库功能演示失败");
            }
        }

        /// <summary>
        /// 显示当前数据源
        /// </summary>
        private async Task ShowCurrentDataSources()
        {
            Console.WriteLine("1. 显示当前数据源:");
            var dataSources = await _dataSourceService.GetAllDataSourcesAsync();
            
            if (dataSources.Any())
            {
                foreach (var ds in dataSources)
                {
                    var defaultStatus = ds.IsDefault ? "⭐ (默认)" : "";
                    Console.WriteLine($"   - {ds.Name} ({ds.Type}) {defaultStatus}");
                }
            }
            else
            {
                Console.WriteLine("   没有数据源");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 创建测试数据源
        /// </summary>
        private async Task CreateTestDataSources()
        {
            Console.WriteLine("2. 创建测试数据源:");

            var testDataSources = new[]
            {
                new DataSourceConfig
                {
                    Name = "MySQL测试数据库",
                    Type = "MySQL",
                    Description = "用于测试的MySQL数据库",
                    ConnectionString = "Server=localhost;Port=3306;Database=test;Uid=root;Pwd=password;",
                    Status = "已连接",
                    IsConnected = true,
                    IsDefault = false
                },
                new DataSourceConfig
                {
                    Name = "SQLite本地数据库",
                    Type = "SQLite",
                    Description = "本地SQLite数据库",
                    ConnectionString = "Data Source=./data/local.db;Version=3;",
                    Status = "已连接",
                    IsConnected = true,
                    IsDefault = false
                }
            };

            foreach (var dataSource in testDataSources)
            {
                try
                {
                    await _dataSourceService.SaveDataSourceAsync(dataSource);
                    Console.WriteLine($"   ✅ 创建数据源: {dataSource.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ 创建数据源失败: {dataSource.Name} - {ex.Message}");
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 测试设置默认数据库
        /// </summary>
        private async Task TestSetDefaultDatabase()
        {
            Console.WriteLine("3. 测试设置默认数据库:");

            var dataSources = await _dataSourceService.GetAllDataSourcesAsync();
            if (dataSources.Any())
            {
                var firstDataSource = dataSources.First();
                Console.WriteLine($"   设置 '{firstDataSource.Name}' 为默认数据库...");

                // 设置默认数据库
                firstDataSource.IsDefault = true;
                var success = await _dataSourceService.UpdateDataSourceAsync(firstDataSource);

                if (success)
                {
                    Console.WriteLine($"   ✅ 成功设置默认数据库");
                }
                else
                {
                    Console.WriteLine($"   ❌ 设置默认数据库失败");
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 测试唯一性保证
        /// </summary>
        private async Task TestUniquenessConstraint()
        {
            Console.WriteLine("4. 测试唯一性保证:");

            var dataSources = await _dataSourceService.GetAllDataSourcesAsync();
            if (dataSources.Count >= 2)
            {
                var secondDataSource = dataSources[1];
                Console.WriteLine($"   设置 '{secondDataSource.Name}' 为默认数据库...");

                // 先取消所有数据源的默认状态
                foreach (var ds in dataSources)
                {
                    if (ds.IsDefault)
                    {
                        ds.IsDefault = false;
                        await _dataSourceService.UpdateDataSourceAsync(ds);
                        Console.WriteLine($"   取消 '{ds.Name}' 的默认状态");
                    }
                }

                // 设置第二个数据源为默认
                secondDataSource.IsDefault = true;
                var success = await _dataSourceService.UpdateDataSourceAsync(secondDataSource);

                if (success)
                {
                    Console.WriteLine($"   ✅ 成功设置新的默认数据库");
                    
                    // 验证唯一性
                    var updatedDataSources = await _dataSourceService.GetAllDataSourcesAsync();
                    var defaultCount = updatedDataSources.Count(ds => ds.IsDefault);
                    
                    if (defaultCount == 1)
                    {
                        Console.WriteLine($"   ✅ 唯一性验证通过，只有1个默认数据库");
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ 唯一性验证失败，发现{defaultCount}个默认数据库");
                    }
                }
                else
                {
                    Console.WriteLine($"   ❌ 设置新的默认数据库失败");
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 测试取消默认数据库
        /// </summary>
        private async Task TestRemoveDefaultDatabase()
        {
            Console.WriteLine("5. 测试取消默认数据库:");

            var dataSources = await _dataSourceService.GetAllDataSourcesAsync();
            var defaultDataSource = dataSources.FirstOrDefault(ds => ds.IsDefault);

            if (defaultDataSource != null)
            {
                Console.WriteLine($"   取消 '{defaultDataSource.Name}' 的默认设置...");

                defaultDataSource.IsDefault = false;
                var success = await _dataSourceService.UpdateDataSourceAsync(defaultDataSource);

                if (success)
                {
                    Console.WriteLine($"   ✅ 成功取消默认数据库设置");
                    
                    // 验证取消结果
                    var updatedDataSources = await _dataSourceService.GetAllDataSourcesAsync();
                    var remainingDefaultCount = updatedDataSources.Count(ds => ds.IsDefault);
                    
                    if (remainingDefaultCount == 0)
                    {
                        Console.WriteLine($"   ✅ 验证通过，没有默认数据库");
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ 验证失败，仍有{remainingDefaultCount}个默认数据库");
                    }
                }
                else
                {
                    Console.WriteLine($"   ❌ 取消默认数据库设置失败");
                }
            }
            else
            {
                Console.WriteLine("   当前没有默认数据库");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 主程序入口
        /// </summary>
        public static async Task Main(string[] args)
        {
            var demo = new DefaultDatabaseDemo();
            await demo.RunDemo();
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
} 