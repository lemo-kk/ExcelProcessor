using System;
using System.Linq;
using System.Threading.Tasks;
using ExcelProcessor.Data.Services;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Tests
{
    /// <summary>
    /// 快速测试默认数据库功能
    /// </summary>
    public class QuickTest
    {
        public static async Task TestDefaultDatabase()
        {
            Console.WriteLine("=== 快速测试默认数据库功能 ===\n");

            try
            {
                // 创建日志记录器
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                });
                var logger = loggerFactory.CreateLogger<QuickTest>();

                // 创建数据源服务
                var connectionString = "Data Source=./data/excel_processor.db;Version=3;";
                var dataSourceService = new DataSourceService(logger, connectionString);

                // 1. 获取所有数据源
                var dataSources = await dataSourceService.GetAllDataSourcesAsync();
                Console.WriteLine($"当前共有 {dataSources.Count} 个数据源");

                if (dataSources.Any())
                {
                    // 2. 显示每个数据源的IsDefault属性
                    Console.WriteLine("\n数据源详情:");
                    foreach (var ds in dataSources)
                    {
                        Console.WriteLine($"  - {ds.Name} ({ds.Type})");
                        Console.WriteLine($"    IsDefault: {ds.IsDefault}");
                        Console.WriteLine($"    ID: {ds.Id}");
                        Console.WriteLine();
                    }

                    // 3. 查找默认数据源
                    var defaultDataSource = dataSources.FirstOrDefault(ds => ds.IsDefault);
                    if (defaultDataSource != null)
                    {
                        Console.WriteLine($"当前默认数据源: {defaultDataSource.Name}");
                    }
                    else
                    {
                        Console.WriteLine("当前没有默认数据源");
                    }

                    // 4. 如果没有默认数据源，设置第一个为默认
                    if (defaultDataSource == null && dataSources.Any())
                    {
                        var firstDataSource = dataSources.First();
                        Console.WriteLine($"\n设置 '{firstDataSource.Name}' 为默认数据源...");

                        firstDataSource.IsDefault = true;
                        var success = await dataSourceService.UpdateDataSourceAsync(firstDataSource);

                        if (success)
                        {
                            Console.WriteLine("✅ 成功设置默认数据源");
                        }
                        else
                        {
                            Console.WriteLine("❌ 设置默认数据源失败");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("没有数据源可以测试");
                }

                Console.WriteLine("\n=== 测试完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
            }
        }
    }
} 