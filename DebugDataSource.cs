using System;
using System.Linq;
using System.Threading.Tasks;
using ExcelProcessor.Data.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Data.SQLite;

namespace ExcelProcessor.Debug
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 数据源配置调试程序 ===\n");

            // 创建服务容器
            var services = new ServiceCollection();
            
            // 添加日志服务
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // 添加数据源服务
            var connectionString = "Data Source=ExcelProcessor.db;Version=3;";
            services.AddSingleton<DataSourceService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<DataSourceService>>();
                return new DataSourceService(logger, connectionString);
            });

            services.AddSingleton<DatabaseTableService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<DatabaseTableService>>();
                var dataSourceService = provider.GetRequiredService<DataSourceService>();
                return new DatabaseTableService(logger, dataSourceService);
            });

            var serviceProvider = services.BuildServiceProvider();

            try
            {
                var dataSourceService = serviceProvider.GetRequiredService<DataSourceService>();
                var databaseTableService = serviceProvider.GetRequiredService<DatabaseTableService>();

                // 1. 检查数据源配置
                Console.WriteLine("1. 检查数据源配置:");
                var dataSources = await dataSourceService.GetAllDataSourcesAsync();
                Console.WriteLine($"   找到 {dataSources.Count} 个数据源");

                foreach (var ds in dataSources)
                {
                    Console.WriteLine($"   - {ds.Name} ({ds.Type}): {ds.Status}, IsDefault={ds.IsDefault}");
                }

                if (!dataSources.Any())
                {
                    Console.WriteLine("   ❌ 没有找到任何数据源配置");
                    return;
                }

                // 2. 测试第一个数据源的表名获取
                var firstDataSource = dataSources.First();
                Console.WriteLine($"\n2. 测试数据源 '{firstDataSource.Name}' 的表名获取:");

                try
                {
                    var tableNames = await databaseTableService.GetTableNamesAsync(firstDataSource.Name);
                    Console.WriteLine($"   找到 {tableNames.Count} 个表");

                    foreach (var tableName in tableNames)
                    {
                        Console.WriteLine($"   - {tableName}");
                    }

                    if (!tableNames.Any())
                    {
                        Console.WriteLine("   ⚠️  没有找到任何表");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ 获取表名失败: {ex.Message}");
                }

                // 3. 检查数据库连接
                Console.WriteLine($"\n3. 检查数据库连接:");
                try
                {
                    using var connection = new SQLiteConnection(connectionString);
                    await connection.OpenAsync();
                    Console.WriteLine("   ✅ 数据库连接成功");

                    // 检查DataSourceConfig表是否存在
                    var tableExists = await connection.QueryFirstOrDefaultAsync<int>(
                        "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='DataSourceConfig'");
                    
                    if (tableExists > 0)
                    {
                        Console.WriteLine("   ✅ DataSourceConfig表存在");
                        
                        // 检查表中的数据
                        var count = await connection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM DataSourceConfig");
                        Console.WriteLine($"   📊 DataSourceConfig表中有 {count} 条记录");
                    }
                    else
                    {
                        Console.WriteLine("   ❌ DataSourceConfig表不存在");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ 数据库连接失败: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 程序执行失败: {ex.Message}");
                Console.WriteLine($"   详细错误: {ex}");
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
} 