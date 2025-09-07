# 数据库连接功能使用指南

## 概述

本项目支持多种数据库类型的连接和测试功能，包括：

- **SQLite** - 轻量级文件数据库
- **MySQL** - 开源关系型数据库
- **SQL Server** - 微软关系型数据库
- **PostgreSQL** - 开源对象关系型数据库
- **Oracle** - 企业级关系型数据库

## 功能特性

### ✅ 已实现功能

1. **连接字符串构建** - 自动根据用户输入构建标准连接字符串
2. **连接测试** - 实时测试数据库连接是否可用
3. **连接状态管理** - 记录连接状态和最后测试时间
4. **错误信息反馈** - 提供详细的连接错误信息
5. **多数据库支持** - 统一的接口支持多种数据库类型

### 🔧 技术实现

- 使用官方数据库驱动包
- 异步连接测试，不阻塞UI
- 详细的错误日志记录
- 连接超时处理

## 数据库驱动包

项目已添加以下NuGet包：

```xml
<!-- MySQL驱动 -->
<PackageReference Include="MySql.Data" Version="8.3.0" />

<!-- SQL Server驱动 -->
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.5" />

<!-- PostgreSQL驱动 -->
<PackageReference Include="Npgsql" Version="8.0.2" />

<!-- Oracle驱动 -->
<PackageReference Include="Oracle.ManagedDataAccess.Core" Version="3.21.130" />
```

## 连接字符串格式

### MySQL
```
Server=localhost;Port=3306;Database=testdb;Uid=root;Pwd=password;
```

### SQL Server
```
Server=localhost,1433;Database=testdb;User Id=sa;Password=password;
```

### PostgreSQL
```
Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=password;
```

### Oracle
```
Data Source=localhost:1521/XE;User Id=system;Password=password;
```

### SQLite
```
Data Source=test.db;Version=3;
```

## 使用方法

### 1. 在UI中添加数据源

1. 打开数据源管理页面
2. 点击"添加数据源"按钮
3. 选择数据库类型
4. 填写连接信息：
   - 服务器地址
   - 端口号
   - 数据库名称
   - 用户名
   - 密码
5. 点击"测试连接"验证连接
6. 保存数据源配置

### 2. 程序化使用

```csharp
// 创建数据源配置
var dataSource = new DataSourceConfig
{
    Name = "测试数据源",
    Type = "MySQL",
    ConnectionString = "Server=localhost;Port=3306;Database=testdb;Uid=root;Pwd=password;"
};

// 测试连接
var (isConnected, errorMessage) = await dataSourceService.TestConnectionWithDetailsAsync(dataSource);

if (isConnected)
{
    Console.WriteLine("连接成功！");
}
else
{
    Console.WriteLine($"连接失败：{errorMessage}");
}
```

## 连接测试逻辑

### 测试流程

1. **参数验证** - 检查连接字符串格式
2. **建立连接** - 使用对应数据库驱动创建连接
3. **执行测试查询** - 执行 `SELECT 1` 验证连接
4. **返回结果** - 返回连接状态和错误信息

### 错误处理

- **连接超时** - 网络连接问题
- **认证失败** - 用户名或密码错误
- **数据库不存在** - 指定的数据库不存在
- **服务器不可达** - 服务器地址或端口错误

## 常见问题

### Q: 连接测试失败怎么办？

A: 请检查以下项目：
1. 服务器地址和端口是否正确
2. 数据库名称是否存在
3. 用户名和密码是否正确
4. 防火墙是否允许连接
5. 数据库服务是否正在运行

### Q: 如何配置数据库服务器？

A: 各数据库的默认端口：
- MySQL: 3306
- SQL Server: 1433
- PostgreSQL: 5432
- Oracle: 1521

### Q: 支持哪些认证方式？

A: 目前支持用户名/密码认证，后续可扩展支持：
- Windows认证（SQL Server）
- SSL证书认证
- Kerberos认证

## 开发说明

### 添加新的数据库类型

1. 在 `DataSourceService.cs` 中添加新的测试方法
2. 在 `DataSourcePage.xaml.cs` 中添加连接字符串构建方法
3. 在UI中添加对应的连接面板
4. 更新数据源类型枚举

### 扩展连接功能

- 支持连接池配置
- 添加SSL/TLS加密
- 支持读写分离
- 添加连接监控

## 测试

运行单元测试验证连接功能：

```bash
dotnet test --filter "DatabaseConnectionTests"
```

## 许可证

本项目使用MIT许可证，数据库驱动包遵循各自的许可证。 
 
 
 
 
 