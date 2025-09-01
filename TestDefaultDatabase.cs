using System;
using System.Linq;
using System.Threading.Tasks;
using ExcelProcessor.Data.Services;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Tests
{
    /// <summary>
    /// 默认数据库功能测试
    /// </summary>
    public class TestDefaultDatabase
    {
        private readonly DataSourceService _dataSourceService;
        private readonly ILogger<TestDefaultDatabase> _logger;

        public TestDefaultDatabase(DataSourceService dataSourceService, ILogger<TestDefaultDatabase> logger)
        {
            _dataSourceService = dataSourceService;
            _logger = logger;
        }

        /// <summary>
        /// 测试默认数据库功能
        /// </summary>
        public async Task TestDefaultDatabaseFunctionality()
        {
            Console.WriteLine("=== 默认数据库功能测试 ===\n");

            try
            {
                // 1. 获取所有数据源
                var allDataSources = await _dataSourceService.GetAllDataSourcesAsync();
                Console.WriteLine($"当前共有 {allDataSources.Count} 个数据源");

                // 2. 显示所有数据源的默认状态
                Console.WriteLine("\n当前数据源状态:");
                foreach (var ds in allDataSources)
                {
                    Console.WriteLine($"  - {ds.Name} ({ds.Type}): IsDefault = {ds.IsDefault}");
                }

                // 3. 查找默认数据源
                var defaultDataSource = allDataSources.FirstOrDefault(ds => ds.IsDefault);
                if (defaultDataSource != null)
                {
                    Console.WriteLine($"\n当前默认数据源: {defaultDataSource.Name}");
                }
                else
                {
                    Console.WriteLine("\n当前没有默认数据源");
                }

                // 4. 如果有数据源，设置第一个为默认
                if (allDataSources.Any())
                {
                    var firstDataSource = allDataSources.First();
                    Console.WriteLine($"\n设置 '{firstDataSource.Name}' 为默认数据源...");

                    // 先取消所有数据源的默认状态
                    foreach (var ds in allDataSources)
                    {
                        if (ds.IsDefault)
                        {
                            ds.IsDefault = false;
                            await _dataSourceService.UpdateDataSourceAsync(ds);
                            Console.WriteLine($"  取消 '{ds.Name}' 的默认状态");
                        }
                    }

                    // 设置第一个数据源为默认
                    firstDataSource.IsDefault = true;
                    var success = await _dataSourceService.UpdateDataSourceAsync(firstDataSource);
                    
                    if (success)
                    {
                        Console.WriteLine($"  成功设置 '{firstDataSource.Name}' 为默认数据源");
                    }
                    else
                    {
                        Console.WriteLine($"  设置默认数据源失败");
                    }

                    // 5. 验证设置结果
                    var updatedDataSources = await _dataSourceService.GetAllDataSourcesAsync();
                    var newDefaultDataSource = updatedDataSources.FirstOrDefault(ds => ds.IsDefault);
                    
                    if (newDefaultDataSource != null && newDefaultDataSource.Id == firstDataSource.Id)
                    {
                        Console.WriteLine($"\n✅ 验证成功: '{newDefaultDataSource.Name}' 现在是默认数据源");
                    }
                    else
                    {
                        Console.WriteLine($"\n❌ 验证失败: 默认数据源设置不正确");
                    }
                }
                else
                {
                    Console.WriteLine("\n没有数据源可以测试");
                }

                Console.WriteLine("\n=== 测试完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试过程中发生错误: {ex.Message}");
                _logger.LogError(ex, "默认数据库功能测试失败");
            }
        }

        /// <summary>
        /// 显示数据源详细信息
        /// </summary>
        public async Task ShowDataSourceDetails()
        {
            try
            {
                var allDataSources = await _dataSourceService.GetAllDataSourcesAsync();
                
                Console.WriteLine("\n=== 数据源详细信息 ===");
                foreach (var ds in allDataSources)
                {
                    Console.WriteLine($"\n数据源: {ds.Name}");
                    Console.WriteLine($"  ID: {ds.Id}");
                    Console.WriteLine($"  类型: {ds.Type}");
                    Console.WriteLine($"  描述: {ds.Description}");
                    Console.WriteLine($"  连接状态: {ds.Status}");
                    Console.WriteLine($"  是否连接: {ds.IsConnected}");
                    Console.WriteLine($"  是否启用: {ds.IsEnabled}");
                    Console.WriteLine($"  是否默认: {ds.IsDefault}");
                    Console.WriteLine($"  创建时间: {ds.CreatedTime}");
                    Console.WriteLine($"  更新时间: {ds.UpdatedTime}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取数据源详细信息失败: {ex.Message}");
            }
        }
    }
} 