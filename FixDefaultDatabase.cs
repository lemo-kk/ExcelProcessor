using System;
using System.Linq;
using System.Threading.Tasks;
using ExcelProcessor.Data.Services;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Fix
{
    /// <summary>
    /// 修复默认数据库显示问题
    /// </summary>
    public class FixDefaultDatabase
    {
        /// <summary>
        /// 修复默认数据库显示问题
        /// </summary>
        public static async Task FixDefaultDatabaseIssue()
        {
            Console.WriteLine("=== 修复默认数据库显示问题 ===\n");

            try
            {
                // 创建日志记录器
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                });
                var logger = loggerFactory.CreateLogger<FixDefaultDatabase>();

                // 创建数据源服务
                var connectionString = "Data Source=./data/excel_processor.db;Version=3;";
                var dataSourceService = new DataSourceService(logger, connectionString);

                // 1. 获取所有数据源
                var dataSources = await dataSourceService.GetAllDataSourcesAsync();
                Console.WriteLine($"找到 {dataSources.Count} 个数据源");

                if (dataSources.Any())
                {
                    // 2. 检查是否有默认数据源
                    var defaultDataSource = dataSources.FirstOrDefault(ds => ds.IsDefault);
                    
                    if (defaultDataSource != null)
                    {
                        Console.WriteLine($"当前默认数据源: {defaultDataSource.Name}");
                        Console.WriteLine("默认数据库已存在，无需修复");
                    }
                    else
                    {
                        Console.WriteLine("没有默认数据源，设置第一个数据源为默认...");
                        
                        // 3. 设置第一个数据源为默认
                        var firstDataSource = dataSources.First();
                        firstDataSource.IsDefault = true;
                        
                        var success = await dataSourceService.UpdateDataSourceAsync(firstDataSource);
                        
                        if (success)
                        {
                            Console.WriteLine($"✅ 成功设置 '{firstDataSource.Name}' 为默认数据源");
                            Console.WriteLine("\n现在你应该能看到:");
                            Console.WriteLine("1. 数据源名称旁边显示星形图标 ⭐");
                            Console.WriteLine("2. 数据源名称旁边显示 '(默认)' 标识");
                            Console.WriteLine("3. 操作按钮区域显示 '取消默认数据库' 按钮");
                            Console.WriteLine("4. 其他数据源显示 '设为默认数据库' 按钮");
                        }
                        else
                        {
                            Console.WriteLine("❌ 设置默认数据源失败");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("没有数据源，请先添加数据源");
                }

                Console.WriteLine("\n=== 修复完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"修复失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 主程序入口
        /// </summary>
        public static async Task Main(string[] args)
        {
            await FixDefaultDatabaseIssue();
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
} 