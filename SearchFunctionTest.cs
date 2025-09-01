using System;
using System.Collections.Generic;
using System.Linq;
using ExcelProcessor.Models;

namespace ExcelProcessor.Tests
{
    /// <summary>
    /// 搜索功能测试示例
    /// </summary>
    public class SearchFunctionTest
    {
        /// <summary>
        /// 测试搜索功能
        /// </summary>
        public static void TestSearchFunction()
        {
            Console.WriteLine("=== 数据源搜索功能测试 ===\n");

            // 创建测试数据源
            var testDataSources = CreateTestDataSources();

            // 测试不同的搜索关键词
            var searchKeywords = new[]
            {
                "mysql",
                "localhost",
                "test",
                "sqlite",
                "oracle",
                "postgres",
                "3306",
                "5432",
                "1521",
                "production",
                "development"
            };

            foreach (var keyword in searchKeywords)
            {
                TestSearch(testDataSources, keyword);
            }

            Console.WriteLine("\n=== 搜索功能测试完成 ===");
        }

        /// <summary>
        /// 创建测试数据源
        /// </summary>
        private static List<DataSourceConfig> CreateTestDataSources()
        {
            return new List<DataSourceConfig>
            {
                new DataSourceConfig
                {
                    Id = "1",
                    Name = "MySQL生产数据库",
                    Type = "MySQL",
                    Description = "生产环境的MySQL数据库，用于存储用户数据",
                    ConnectionString = "Server=localhost;Port=3306;Database=production;Uid=root;Pwd=password;",
                    Status = "已连接",
                    IsConnected = true,
                    LastTestTime = DateTime.Now.AddHours(-1)
                },
                new DataSourceConfig
                {
                    Id = "2",
                    Name = "SQLite本地数据库",
                    Type = "SQLite",
                    Description = "本地SQLite数据库，用于开发和测试",
                    ConnectionString = "Data Source=./data/local.db;Version=3;",
                    Status = "已连接",
                    IsConnected = true,
                    LastTestTime = DateTime.Now.AddHours(-2)
                },
                new DataSourceConfig
                {
                    Id = "3",
                    Name = "PostgreSQL开发数据库",
                    Type = "PostgreSQL",
                    Description = "开发环境的PostgreSQL数据库",
                    ConnectionString = "Host=localhost;Port=5432;Database=development;Username=postgres;Password=password;",
                    Status = "连接失败",
                    IsConnected = false,
                    LastTestTime = DateTime.Now.AddHours(-3)
                },
                new DataSourceConfig
                {
                    Id = "4",
                    Name = "Oracle测试数据库",
                    Type = "Oracle",
                    Description = "Oracle数据库测试实例",
                    ConnectionString = "Data Source=localhost:1521/XE;User Id=system;Password=password;",
                    Status = "未测试",
                    IsConnected = false,
                    LastTestTime = DateTime.MinValue
                },
                new DataSourceConfig
                {
                    Id = "5",
                    Name = "SQL Server主数据库",
                    Type = "SQLServer",
                    Description = "主要的SQL Server数据库服务器",
                    ConnectionString = "Server=localhost,1433;Database=master;User Id=sa;Password=password;",
                    Status = "已连接",
                    IsConnected = true,
                    LastTestTime = DateTime.Now.AddMinutes(-30)
                }
            };
        }

        /// <summary>
        /// 测试单个搜索关键词
        /// </summary>
        private static void TestSearch(List<DataSourceConfig> dataSources, string keyword)
        {
            var filteredDataSources = dataSources.Where(dataSource =>
                ContainsKeyword(dataSource.Name, keyword) ||
                ContainsKeyword(dataSource.Type, keyword) ||
                ContainsKeyword(dataSource.Description, keyword) ||
                ContainsKeyword(dataSource.ConnectionString, keyword)
            ).ToList();

            Console.WriteLine($"搜索关键词: '{keyword}'");
            Console.WriteLine($"  找到 {filteredDataSources.Count} 个匹配的数据源:");
            
            foreach (var dataSource in filteredDataSources)
            {
                Console.WriteLine($"    - {dataSource.Name} ({dataSource.Type})");
            }
            
            Console.WriteLine();
        }

        /// <summary>
        /// 检查文本是否包含关键词（不区分大小写）
        /// </summary>
        private static bool ContainsKeyword(string text, string keyword)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
                return false;

            return text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 演示搜索功能的使用方法
        /// </summary>
        public static void DemonstrateSearchUsage()
        {
            Console.WriteLine("=== 搜索功能使用方法演示 ===\n");

            Console.WriteLine("1. 在数据源管理页面的搜索框中输入关键词");
            Console.WriteLine("2. 系统会自动过滤显示匹配的数据源");
            Console.WriteLine("3. 搜索范围包括：");
            Console.WriteLine("   - 数据源名称");
            Console.WriteLine("   - 数据库类型");
            Console.WriteLine("   - 描述信息");
            Console.WriteLine("   - 连接字符串");
            Console.WriteLine("4. 搜索不区分大小写");
            Console.WriteLine("5. 点击清除按钮可以清空搜索条件");
            Console.WriteLine("6. 刷新数据后搜索状态会保持");
            Console.WriteLine();

            Console.WriteLine("示例搜索关键词：");
            Console.WriteLine("- mysql, sqlite, oracle, postgresql, sqlserver");
            Console.WriteLine("- localhost, 3306, 5432, 1521, 1433");
            Console.WriteLine("- production, development, test");
            Console.WriteLine("- 连接, 失败, 成功");
        }
    }
} 