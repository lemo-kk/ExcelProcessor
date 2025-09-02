# SQL管理功能重构说明

## 概述

本次重构将SQL管理功能中的SQL执行逻辑重构为一个公用方法，支持参数传递，提高了代码的复用性和灵活性。

## 主要变更

### 1. 新增公用方法

#### `ExecuteSqlQueryAsync` - 核心SQL执行方法
```csharp
public async Task<SqlQueryResult> ExecuteSqlQueryAsync(
    string sqlStatement, 
    string? dataSourceId = null, 
    Dictionary<string, object>? parameters = null, 
    ISqlProgressCallback? progressCallback = null)
```

**功能特点：**
- 支持多种数据库类型（SQLite、MySQL、SQL Server、PostgreSQL、Oracle）
- 支持参数化查询，防止SQL注入
- 统一的错误处理和日志记录
- 支持进度回调

**使用场景：**
- SQL测试功能
- SQL输出到表
- SQL输出到Excel
- 其他需要执行SQL查询的功能

### 2. 参数支持

#### 无参数调用
```csharp
// 执行简单查询，无参数
var result = await sqlService.ExecuteSqlQueryAsync("SELECT * FROM Users");
```

#### 有参数调用
```csharp
// 执行参数化查询
var parameters = new Dictionary<string, object>
{
    { "@UserId", 123 },
    { "@Status", "Active" }
};

var result = await sqlService.ExecuteSqlQueryAsync(
    "SELECT * FROM Users WHERE Id = @UserId AND Status = @Status",
    dataSourceId: "ds001",
    parameters: parameters
);
```

### 3. 重构后的方法调用链

```
用户操作 → 业务逻辑 → ExecuteSqlQueryAsync → 数据库特定实现 → 返回结果
```

#### 具体流程：
1. **SQL测试**：`TestSqlStatementAsync` → `ExecuteSqlQueryAsync`
2. **输出到表**：`ExecuteSqlToTableAsync` → `ExecuteQueryInternalAsync`
3. **输出到Excel**：`ExecuteSqlToExcelAsync` → `ExecuteQueryInternalAsync`
4. **执行配置**：`ExecuteSqlConfigAsync` → `ExecuteSqlQueryAsync`

### 4. 数据库类型支持

系统自动识别连接字符串中的数据库类型，并调用相应的实现：

- **SQLite**: 默认数据库，支持参数化查询
- **MySQL**: 支持参数化查询
- **SQL Server**: 支持参数化查询
- **PostgreSQL**: 支持参数化查询
- **Oracle**: 支持参数化查询

## 使用示例

### 示例1：测试SQL语句
```csharp
// 测试带参数的SQL
var testResult = await sqlService.TestSqlStatementAsync(
    "SELECT * FROM Orders WHERE OrderDate >= @StartDate AND Status = @Status",
    dataSourceId: "mysql_ds",
    parameters: new Dictionary<string, object>
    {
        { "@StartDate", DateTime.Today.AddDays(-30) },
        { "@Status", "Completed" }
    }
);
```

### 示例2：输出到数据表
```csharp
// 执行SQL并输出到数据表
var outputResult = await sqlService.ExecuteSqlToTableAsync(
    "SELECT * FROM Sales WHERE Region = @Region AND Year = @Year",
    queryDataSourceId: "sales_ds",
    targetDataSourceId: "warehouse_ds",
    targetTableName: "SalesSummary",
    clearTableBeforeInsert: true,
    parameters: new Dictionary<string, object>
    {
        { "@Region", "North" },
        { "@Year", 2024 }
    }
);
```

### 示例3：输出到Excel
```csharp
// 执行SQL并输出到Excel
var excelResult = await sqlService.ExecuteSqlToExcelAsync(
    "SELECT ProductName, SUM(Quantity) as TotalQty FROM Orders WHERE OrderDate >= @StartDate GROUP BY ProductName",
    queryDataSourceId: "orders_ds",
    outputPath: @"C:\Reports\ProductSummary.xlsx",
    sheetName: "ProductSummary",
    clearSheetBeforeOutput: true,
    parameters: new Dictionary<string, object>
    {
        { "@StartDate", DateTime.Today.AddMonths(-1) }
    }
);
```

## 参数处理说明

### 参数命名规范
- 使用 `@` 前缀（如：`@UserId`, `@StartDate`）
- 参数名区分大小写
- 支持各种数据类型（字符串、数字、日期等）

### 参数类型映射
```csharp
// 字符串参数
{ "@Name", "John Doe" }

// 数字参数
{ "@Age", 25 }
{ "@Price", 99.99m }

// 日期参数
{ "@BirthDate", DateTime.Today }
{ "@StartTime", DateTime.Now }

// 布尔参数
{ "@IsActive", true }

// 空值参数
{ "@Description", DBNull.Value }
```

### 安全性
- 所有参数都通过参数化查询处理，防止SQL注入
- 参数值自动转义和类型转换
- 支持NULL值处理

## 错误处理

### 常见错误类型
1. **SQL语法错误**：返回具体的语法错误信息
2. **连接错误**：数据库连接失败时的错误信息
3. **权限错误**：数据库权限不足时的错误信息
4. **参数错误**：参数类型不匹配或缺失时的错误信息

### 错误处理示例
```csharp
try
{
    var result = await sqlService.ExecuteSqlQueryAsync(sql, dataSourceId, parameters);
    if (result.IsSuccess)
    {
        // 处理成功结果
        Console.WriteLine($"查询成功，返回 {result.RowCount} 行数据");
    }
    else
    {
        // 处理错误
        Console.WriteLine($"查询失败：{result.ErrorMessage}");
    }
}
catch (Exception ex)
{
    // 处理异常
    Console.WriteLine($"执行异常：{ex.Message}");
}
```

## 性能优化

### 查询限制
- SQL测试功能自动添加LIMIT/TOP子句，限制返回行数
- 支持进度回调，可以显示执行进度
- 统一的连接管理和资源释放

### 监控和日志
- 详细的执行时间统计
- 完整的操作日志记录
- 支持进度回调，实时反馈执行状态

## 向后兼容性

### 现有代码兼容
- 所有现有方法调用保持不变
- 新增的参数参数为可选参数
- 无参数调用仍然有效

### 迁移建议
1. **立即使用**：新的公用方法可以立即使用
2. **渐进迁移**：逐步将现有代码迁移到新的参数化方式
3. **性能提升**：参数化查询通常比字符串拼接有更好的性能

## 总结

本次重构实现了以下目标：

✅ **代码复用**：统一的SQL执行逻辑，减少重复代码  
✅ **参数支持**：支持参数化查询，提高安全性和性能  
✅ **灵活调用**：支持有参和无参两种调用方式  
✅ **向后兼容**：现有代码无需修改即可继续使用  
✅ **统一接口**：所有SQL相关功能使用相同的执行引擎  
✅ **错误处理**：统一的错误处理和日志记录机制  

通过这次重构，SQL管理功能变得更加强大、安全和易用。 