# SQL参数使用说明

## 🎯 概述

SQL管理功能现在支持参数化查询，可以防止SQL注入攻击，提高查询的灵活性和安全性。

## 📝 参数定义规则

### 1. 参数命名规范
- 参数名必须以 `@` 开头
- 参数名区分大小写
- 建议使用有意义的名称，如：`@startDate`、`@endDate`、`@userId` 等

### 2. 支持的参数类型
- **整数**: `@userId`、`@age`、`@count`
- **小数**: `@price`、`@rate`、`@amount`
- **字符串**: `@name`、`@status`、`@category`
- **布尔值**: `@isActive`、`@enabled`
- **日期时间**: `@startDate`、`@endDate`、`@createdTime`

## 🔧 界面操作步骤

### 1. 添加参数
1. 在SQL管理页面的"参数配置"区域
2. 点击"添加参数"按钮
3. 填写参数名称（必须以@开头）
4. 填写默认值
5. 可以添加多个参数

### 2. 配置参数示例
```
参数名称: @startDate
默认值: 2024-01-01

参数名称: @endDate  
默认值: 2024-12-31

参数名称: @status
默认值: Active

参数名称: @userId
默认值: 123
```

## 💻 SQL语句示例

### 1. 基础查询示例
```sql
SELECT * FROM Users 
WHERE CreatedDate >= @startDate 
  AND CreatedDate <= @endDate
  AND Status = @status
```

### 2. 复杂查询示例
```sql
SELECT 
    u.UserName,
    u.Email,
    o.OrderCount,
    o.TotalAmount
FROM Users u
LEFT JOIN (
    SELECT 
        UserId,
        COUNT(*) as OrderCount,
        SUM(Amount) as TotalAmount
    FROM Orders 
    WHERE OrderDate >= @startDate 
      AND OrderDate <= @endDate
    GROUP BY UserId
) o ON u.Id = o.UserId
WHERE u.Status = @status
  AND u.CreatedDate >= @userStartDate
ORDER BY o.TotalAmount DESC
```

### 3. 更新语句示例
```sql
UPDATE Users 
SET LastLoginDate = @lastLoginDate,
    LoginCount = LoginCount + 1
WHERE Id = @userId
  AND Status = @status
```

### 4. 删除语句示例
```sql
DELETE FROM Logs 
WHERE LogDate < @cutoffDate
  AND LogLevel = @logLevel
```

## 🚀 使用场景

### 1. 报表查询
```sql
-- 月度销售报表
SELECT 
    ProductName,
    SUM(Quantity) as TotalQuantity,
    SUM(Amount) as TotalAmount
FROM Sales 
WHERE SaleDate >= @monthStart
  AND SaleDate <= @monthEnd
  AND Region = @region
GROUP BY ProductName
ORDER BY TotalAmount DESC
```

### 2. 数据清理
```sql
-- 清理过期数据
DELETE FROM TempData 
WHERE CreatedDate < @expiryDate
  AND DataType = @dataType
```

### 3. 批量操作
```sql
-- 批量更新用户状态
UPDATE Users 
SET Status = @newStatus,
    UpdatedDate = @updateDate
WHERE DepartmentId = @deptId
  AND LastLoginDate < @inactiveThreshold
```

## ⚠️ 注意事项

### 1. 参数值验证
- 确保参数值类型与数据库字段类型匹配
- 日期参数使用标准格式：`YYYY-MM-DD` 或 `YYYY-MM-DD HH:MM:SS`
- 布尔值使用：`true`、`false`、`1`、`0`

### 2. SQL语法兼容性
- 不同数据库的参数语法可能略有不同
- 系统会自动识别数据库类型并使用相应的参数化查询
- 支持的数据库：SQLite、MySQL、SQL Server、PostgreSQL、Oracle

### 3. 性能考虑
- 参数化查询通常比字符串拼接性能更好
- 数据库可以缓存执行计划
- 避免在循环中重复创建相同的参数化查询

## 🔍 故障排除

### 1. 常见错误
- **"Insufficient parameters supplied"**: 参数数量不足，检查SQL语句中的参数是否都有对应的值
- **"Parameter type mismatch"**: 参数类型不匹配，检查参数值类型是否正确
- **"Unknown parameter"**: 未知参数，检查参数名是否正确

### 2. 调试技巧
- 在日志中查看参数传递情况
- 使用SQL测试功能验证参数是否正确
- 检查参数名称是否与SQL语句中的完全匹配

### 3. 参数检查清单
- [ ] 所有 `@` 开头的参数都有对应的值
- [ ] 参数值类型与数据库字段类型匹配
- [ ] 参数名称拼写正确（区分大小写）
- [ ] 日期参数使用正确的格式

## 📚 最佳实践

### 1. 参数命名
- 使用描述性的名称
- 保持命名一致性
- 避免使用缩写

### 2. 参数验证
- 在应用层验证参数值
- 设置合理的默认值
- 处理NULL值情况

### 3. 安全性
- 始终使用参数化查询
- 避免动态构建SQL语句
- 限制参数值的范围和类型

## 🎉 总结

通过使用参数化查询，您可以：
- ✅ 提高SQL查询的安全性
- ✅ 增强代码的可维护性
- ✅ 提升查询性能
- ✅ 简化复杂查询的管理

开始使用参数化查询，让您的SQL管理更加安全和高效！ 