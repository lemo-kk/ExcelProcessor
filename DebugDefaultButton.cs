using System;
using System.Linq;
using System.Threading.Tasks;
using ExcelProcessor.Data.Services;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Debug
{
    /// <summary>
    /// 调试默认按钮显示问题
    /// </summary>
    public class DebugDefaultButton
    {
        private readonly DataSourceService _dataSourceService;
        private readonly ILogger<DebugDefaultButton> _logger;

        public DebugDefaultButton(DataSourceService dataSourceService, ILogger<DebugDefaultButton> logger)
        {
            _dataSourceService = dataSourceService;
            _logger = logger;
        }

        /// <summary>
        /// 调试默认按钮显示问题
        /// </summary>
        public async Task DebugDefaultButtonIssue()
        {
            Console.WriteLine("=== 调试默认按钮显示问题 ===\n");

            try
            {
                // 1. 检查数据库表结构
                await CheckDatabaseStructure();

                // 2. 检查数据源数据
                await CheckDataSourceData();

                // 3. 检查IsDefault属性
                await CheckIsDefaultProperty();

                // 4. 测试设置默认数据库
                await TestSetDefaultDatabase();

                // 5. 验证UI绑定
                await VerifyUIBinding();

                Console.WriteLine("\n=== 调试完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"调试过程中发生错误: {ex.Message}");
                _logger.LogError(ex, "调试默认按钮显示问题失败");
            }
        }

        /// <summary>
        /// 检查数据库表结构
        /// </summary>
        private async Task CheckDatabaseStructure()
        {
            Console.WriteLine("1. 检查数据库表结构:");
            
            try
            {
                // 这里应该检查数据库表是否有IsDefault字段
                // 由于无法直接访问数据库，我们通过查询数据来验证
                var dataSources = await _dataSourceService.GetAllDataSourcesAsync();
                Console.WriteLine($"   数据源表存在，共有 {dataSources.Count} 条记录");
                
                if (dataSources.Any())
                {
                    var firstDataSource = dataSources.First();
                    Console.WriteLine($"   第一个数据源的IsDefault属性: {firstDataSource.IsDefault}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 检查数据库表结构失败: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 检查数据源数据
        /// </summary>
        private async Task CheckDataSourceData()
        {
            Console.WriteLine("2. 检查数据源数据:");
            
            try
            {
                var dataSources = await _dataSourceService.GetAllDataSourcesAsync();
                
                if (dataSources.Any())
                {
                    foreach (var ds in dataSources)
                    {
                        Console.WriteLine($"   数据源: {ds.Name}");
                        Console.WriteLine($"     ID: {ds.Id}");
                        Console.WriteLine($"     类型: {ds.Type}");
                        Console.WriteLine($"     状态: {ds.Status}");
                        Console.WriteLine($"     IsDefault: {ds.IsDefault}");
                        Console.WriteLine($"     创建时间: {ds.CreatedTime}");
                        Console.WriteLine($"     更新时间: {ds.UpdatedTime}");
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("   没有数据源数据");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 检查数据源数据失败: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 检查IsDefault属性
        /// </summary>
        private async Task CheckIsDefaultProperty()
        {
            Console.WriteLine("3. 检查IsDefault属性:");
            
            try
            {
                var dataSources = await _dataSourceService.GetAllDataSourcesAsync();
                
                if (dataSources.Any())
                {
                    var defaultDataSource = dataSources.FirstOrDefault(ds => ds.IsDefault);
                    
                    if (defaultDataSource != null)
                    {
                        Console.WriteLine($"   当前默认数据源: {defaultDataSource.Name}");
                        Console.WriteLine($"   IsDefault值: {defaultDataSource.IsDefault}");
                    }
                    else
                    {
                        Console.WriteLine("   当前没有默认数据源");
                        
                        // 尝试设置第一个数据源为默认
                        var firstDataSource = dataSources.First();
                        Console.WriteLine($"   尝试设置 '{firstDataSource.Name}' 为默认数据源...");
                        
                        firstDataSource.IsDefault = true;
                        var success = await _dataSourceService.UpdateDataSourceAsync(firstDataSource);
                        
                        if (success)
                        {
                            Console.WriteLine($"   ✅ 成功设置默认数据源");
                        }
                        else
                        {
                            Console.WriteLine($"   ❌ 设置默认数据源失败");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 检查IsDefault属性失败: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 测试设置默认数据库
        /// </summary>
        private async Task TestSetDefaultDatabase()
        {
            Console.WriteLine("4. 测试设置默认数据库:");
            
            try
            {
                var dataSources = await _dataSourceService.GetAllDataSourcesAsync();
                
                if (dataSources.Any())
                {
                    // 先取消所有默认设置
                    foreach (var ds in dataSources)
                    {
                        if (ds.IsDefault)
                        {
                            ds.IsDefault = false;
                            await _dataSourceService.UpdateDataSourceAsync(ds);
                            Console.WriteLine($"   取消 '{ds.Name}' 的默认设置");
                        }
                    }

                    // 设置第一个数据源为默认
                    var firstDataSource = dataSources.First();
                    firstDataSource.IsDefault = true;
                    var success = await _dataSourceService.UpdateDataSourceAsync(firstDataSource);
                    
                    if (success)
                    {
                        Console.WriteLine($"   ✅ 成功设置 '{firstDataSource.Name}' 为默认数据源");
                        
                        // 验证设置结果
                        var updatedDataSources = await _dataSourceService.GetAllDataSourcesAsync();
                        var defaultDataSource = updatedDataSources.FirstOrDefault(ds => ds.IsDefault);
                        
                        if (defaultDataSource != null && defaultDataSource.Id == firstDataSource.Id)
                        {
                            Console.WriteLine($"   ✅ 验证成功，默认数据源设置正确");
                        }
                        else
                        {
                            Console.WriteLine($"   ❌ 验证失败，默认数据源设置不正确");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ 设置默认数据源失败");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 测试设置默认数据库失败: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 验证UI绑定
        /// </summary>
        private async Task VerifyUIBinding()
        {
            Console.WriteLine("5. 验证UI绑定:");
            
            try
            {
                var dataSources = await _dataSourceService.GetAllDataSourcesAsync();
                
                if (dataSources.Any())
                {
                    Console.WriteLine("   数据源列表:");
                    foreach (var ds in dataSources)
                    {
                        var defaultStatus = ds.IsDefault ? "⭐ (默认)" : "";
                        Console.WriteLine($"     - {ds.Name} {defaultStatus}");
                        
                        if (ds.IsDefault)
                        {
                            Console.WriteLine($"       应该显示: 取消默认按钮 (StarOff图标)");
                        }
                        else
                        {
                            Console.WriteLine($"       应该显示: 设置默认按钮 (Star图标)");
                        }
                    }
                    
                    Console.WriteLine("\n   如果按钮没有显示，可能的原因:");
                    Console.WriteLine("   1. BoolToVisibilityConverter没有正确注册");
                    Console.WriteLine("   2. 数据绑定路径错误");
                    Console.WriteLine("   3. IsDefault属性没有正确实现INotifyPropertyChanged");
                    Console.WriteLine("   4. 按钮的Visibility绑定语法错误");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 验证UI绑定失败: {ex.Message}");
            }
            Console.WriteLine();
        }
    }
} 