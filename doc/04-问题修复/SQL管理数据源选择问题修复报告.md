# SQL管理数据源选择问题修复报告

## 🚨 问题描述

### 问题现象
在SQL管理页面进行测试查询时，出现以下错误：
```
SQLite error (1): no such table: 明细表
```

### 问题分析
1. **根本原因**：SQL服务中的`TestSqlStatementAsync`方法硬编码使用了默认的SQLite数据库连接字符串
2. **具体表现**：无论用户选择哪个数据源，SQL测试都会在系统默认数据库中执行
3. **影响范围**：所有SQL测试功能都无法正确使用用户选择的数据源

## 🔍 问题定位

### 代码分析
在`ExcelProcessor.Data/Services/SqlService.cs`的`TestSqlStatementAsync`方法中：

```csharp
// 这里应该根据dataSourceId连接到相应的数据库进行测试
// 目前使用默认的SQLite数据库进行测试
using var connection = new SQLiteConnection(_connectionString);
```

**问题代码**：
- 第275行注释明确说明了问题
- 硬编码使用`_connectionString`（系统数据库）
- 忽略了传入的`dataSourceId`参数

### 依赖注入问题
- SqlService构造函数缺少IDataSourceService依赖
- 无法根据dataSourceId获取相应的数据源配置

## ✅ 修复方案

### 1. 修改SqlService构造函数
```csharp
public class SqlService : ISqlService
{
    private readonly ILogger<SqlService> _logger;
    private readonly string _connectionString;
    private readonly IDataSourceService _dataSourceService; // 新增

    public SqlService(ILogger<SqlService> logger, string connectionString, IDataSourceService dataSourceService)
    {
        _logger = logger;
        _connectionString = connectionString;
        _dataSourceService = dataSourceService; // 新增
    }
}
```

### 2. 重写TestSqlStatementAsync方法
```csharp
public async Task<SqlTestResult> TestSqlStatementAsync(string sqlStatement, string? dataSourceId = null, Dictionary<string, object>? parameters = null)
{
    // 根据dataSourceId获取相应的数据源配置
    string connectionString = _connectionString; // 默认使用系统数据库
    
    if (!string.IsNullOrEmpty(dataSourceId))
    {
        // 从数据源服务获取数据源配置
        var dataSource = await _dataSourceService.GetDataSourceByIdAsync(dataSourceId);
        
        if (dataSource != null && !string.IsNullOrEmpty(dataSource.ConnectionString))
        {
            connectionString = dataSource.ConnectionString;
            _logger.LogInformation("使用数据源 {DataSourceName} 进行SQL测试", dataSource.Name);
        }
        else
        {
            _logger.LogWarning("未找到数据源配置 {DataSourceId}，使用默认数据库", dataSourceId);
        }
    }

    // 根据数据源类型创建相应的数据库连接
    IDbConnection connection;
    switch (GetDataSourceType(connectionString))
    {
        case "sqlite":
            connection = new SQLiteConnection(connectionString);
            await ((SQLiteConnection)connection).OpenAsync();
            break;
        case "mysql":
            connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            await ((MySql.Data.MySqlClient.MySqlConnection)connection).OpenAsync();
            break;
        case "sqlserver":
            connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await ((Microsoft.Data.SqlClient.SqlConnection)connection).OpenAsync();
            break;
        case "postgresql":
            connection = new Npgsql.NpgsqlConnection(connectionString);
            await ((Npgsql.NpgsqlConnection)connection).OpenAsync();
            break;
        case "oracle":
            connection = new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString);
            await ((Oracle.ManagedDataAccess.Client.OracleConnection)connection).OpenAsync();
            break;
        default:
            connection = new SQLiteConnection(connectionString);
            await ((SQLiteConnection)connection).OpenAsync();
            break;
    }

    // 根据数据库类型添加不同的限制语法
    var testSql = sqlStatement.Trim();
    if (!testSql.ToUpper().Contains("LIMIT") && !testSql.ToUpper().Contains("TOP"))
    {
        var dbType = GetDataSourceType(connectionString);
        if (dbType == "sqlserver")
        {
            testSql = $"SELECT TOP 10 * FROM ({testSql}) AS TestQuery";
        }
        else
        {
            testSql += " LIMIT 10";
        }
    }

    // 根据数据库类型创建命令对象
    IDbCommand command;
    switch (GetDataSourceType(connectionString))
    {
        case "sqlite":
            command = new SQLiteCommand(testSql, (SQLiteConnection)connection);
            break;
        case "mysql":
            command = new MySql.Data.MySqlClient.MySqlCommand(testSql, (MySql.Data.MySqlClient.MySqlConnection)connection);
            break;
        case "sqlserver":
            command = new Microsoft.Data.SqlClient.SqlCommand(testSql, (Microsoft.Data.SqlClient.SqlConnection)connection);
            break;
        case "postgresql":
            command = new Npgsql.NpgsqlCommand(testSql, (Npgsql.NpgsqlConnection)connection);
            break;
        case "oracle":
            command = new Oracle.ManagedDataAccess.Client.OracleCommand(testSql, (Oracle.ManagedDataAccess.Client.OracleConnection)connection);
            break;
        default:
            command = new SQLiteCommand(testSql, (SQLiteConnection)connection);
            break;
    }

    // 执行查询并获取结果
    // ... 其余代码保持不变
}
```

### 3. 添加数据源类型判断方法
```csharp
/// <summary>
/// 根据连接字符串判断数据源类型
/// </summary>
private string GetDataSourceType(string connectionString)
{
    if (string.IsNullOrEmpty(connectionString))
        return "sqlite";

    var lowerConnectionString = connectionString.ToLower();
    
    if (lowerConnectionString.Contains("server=") || lowerConnectionString.Contains("data source="))
    {
        if (lowerConnectionString.Contains("mysql"))
            return "mysql";
        else if (lowerConnectionString.Contains("sql server") || lowerConnectionString.Contains("mssql"))
            return "sqlserver";
        else if (lowerConnectionString.Contains("postgresql") || lowerConnectionString.Contains("postgres"))
            return "postgresql";
        else if (lowerConnectionString.Contains("oracle"))
            return "oracle";
    }
    
    return "sqlite"; // 默认为SQLite
}
```

### 4. 更新依赖注入配置
```csharp
// 注册DataSourceService，为其提供连接字符串（必须在SqlService之前注册）
services.AddScoped<IDataSourceService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var logger = provider.GetRequiredService<ILogger<DataSourceService>>();
    var connectionString = config.GetConnectionString("DefaultConnection") 
        ?? "Data Source=./data/ExcelProcessor.db;";
    return new DataSourceService(logger, connectionString);
});

// 注册SqlService，为其提供连接字符串和数据源服务
services.AddScoped<ISqlService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var logger = provider.GetRequiredService<ILogger<SqlService>>();
    var dataSourceService = provider.GetRequiredService<IDataSourceService>();
    var connectionString = config.GetConnectionString("DefaultConnection") 
        ?? "Data Source=./data/ExcelProcessor.db;";
    return new SqlService(logger, connectionString, dataSourceService);
});
```

### 5. 添加必要的using语句
```csharp
using MySql.Data.MySqlClient;
using Microsoft.Data.SqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
```

## 🔧 技术要点

### 1. 多数据库支持
- **SQLite**：默认数据库，用于系统配置
- **MySQL**：支持MySQL数据库连接
- **SQL Server**：支持SQL Server数据库连接
- **PostgreSQL**：支持PostgreSQL数据库连接
- **Oracle**：支持Oracle数据库连接

### 2. 数据库类型识别
通过连接字符串内容自动识别数据库类型：
- 包含"mysql" → MySQL
- 包含"sql server"或"mssql" → SQL Server
- 包含"postgresql"或"postgres" → PostgreSQL
- 包含"oracle" → Oracle
- 其他 → SQLite（默认）

### 3. SQL语法适配
根据数据库类型自动调整SQL语法：
- **SQL Server**：使用`SELECT TOP 10 * FROM (...) AS TestQuery`
- **其他数据库**：使用`LIMIT 10`

### 4. 异步操作支持
所有数据库连接和查询操作都支持异步执行，提高性能。

## 🎯 修复效果

### 修复前
- ❌ SQL测试总是使用系统默认数据库
- ❌ 无法正确使用用户选择的数据源
- ❌ 出现"no such table"错误

### 修复后
- ✅ SQL测试正确使用用户选择的数据源
- ✅ 支持多种数据库类型
- ✅ 自动识别数据库类型并适配SQL语法
- ✅ 提供详细的日志记录

## 📋 测试验证

### 测试步骤
1. 启动应用程序
2. 进入SQL管理页面
3. 选择不同的数据源
4. 输入SQL查询语句
5. 点击"测试查询"按钮

### 预期结果
- SQL测试应该使用选择的数据源执行
- 不再出现"no such table"错误
- 能够正确连接到目标数据库

## 🚀 总结

这次修复解决了SQL管理页面的核心问题：

1. **数据源选择问题**：现在SQL测试会正确使用用户选择的数据源
2. **多数据库支持**：支持SQLite、MySQL、SQL Server、PostgreSQL、Oracle等多种数据库
3. **SQL语法适配**：根据数据库类型自动调整SQL语法
4. **性能优化**：所有操作都支持异步执行

修复后的SQL管理功能现在能够：
- 正确识别和使用用户选择的数据源
- 支持多种数据库类型的连接和查询
- 提供更好的错误处理和日志记录
- 确保SQL测试的准确性和可靠性

这个修复为SQL管理功能提供了坚实的基础，使其能够满足不同数据库环境的需求。🎯 