# SQL管理功能实现说明

## 功能概述

已成功实现SQL管理页面的保存SQL和执行SQL功能，包括以下核心特性：

### 1. 保存SQL功能

#### 功能特点：
- **创建新SQL配置**：支持创建全新的SQL查询配置
- **更新现有配置**：支持修改已存在的SQL配置
- **数据验证**：自动验证必填字段（SQL名称、SQL语句、分类）
- **分类管理**：支持SQL分类的创建和管理
- **输出配置**：支持配置输出到数据表或Excel文件

#### 实现细节：
- 使用`SqlConfig`模型存储SQL配置信息
- 通过`ISqlService`接口进行数据持久化
- 支持数据源关联和参数化配置
- 自动记录创建时间和修改时间

### 2. 执行SQL功能

#### 功能特点：
- **执行确认**：执行前显示确认对话框
- **进度显示**：执行过程中显示进度窗口
- **结果反馈**：显示执行状态、耗时、影响行数等信息
- **错误处理**：完善的异常处理和错误信息显示
- **执行历史**：记录SQL执行历史

#### 实现细节：
- 使用`SqlExecutionResult`模型记录执行结果
- 支持异步执行，避免界面阻塞
- 提供详细的执行统计信息
- 支持执行参数传递

### 3. 测试SQL功能

#### 功能特点：
- **语法检查**：验证SQL语句的基本语法
- **性能预估**：预估执行时间和返回行数
- **数据源验证**：验证SQL与数据源的兼容性
- **实时反馈**：立即显示测试结果

## 技术实现

### 后端服务
- **SqlService**：提供SQL配置的CRUD操作
- **DataSourceService**：管理数据源配置
- **DatabaseTableService**：提供数据库表信息

### 前端界面
- **SqlManagementPage**：主要的SQL管理界面
- **数据绑定**：使用MVVM模式进行数据绑定
- **事件处理**：完善的事件处理机制

### 数据模型
- **SqlConfig**：SQL配置模型
- **SqlExecutionResult**：SQL执行结果模型
- **SqlItem**：前端显示用的SQL项目模型

## 使用说明

### 保存SQL配置
1. 在SQL管理页面填写SQL名称
2. 选择或创建SQL分类
3. 输入SQL描述信息
4. 编写SQL语句
5. 配置输出类型和目标
6. 点击"保存SQL"按钮

### 执行SQL查询
1. 在SQL列表中选择要执行的SQL配置
2. 点击"执行查询"按钮
3. 确认执行信息
4. 等待执行完成
5. 查看执行结果

### 测试SQL语句
1. 在SQL语句文本框中输入SQL
2. 点击"语法检查"按钮
3. 查看测试结果和建议

## 配置选项

### 输出类型
- **数据表**：将查询结果输出到数据库表
- **Excel工作表**：将查询结果输出到Excel文件

### 参数配置
- 支持SQL参数化查询
- 支持多种数据类型（String、Integer、Decimal、DateTime、Boolean）
- 支持参数验证和默认值

### 执行选项
- 超时时间设置
- 最大返回行数限制
- 事务控制选项
- 错误处理策略

## 扩展功能

### 已实现的功能
- ✅ SQL配置的创建和更新
- ✅ SQL查询的执行
- ✅ SQL语法检查
- ✅ 参数化SQL支持
- ✅ 多种输出格式
- ✅ 执行历史记录
- ✅ 分类管理

### 可扩展的功能
- 🔄 批量SQL执行
- 🔄 SQL模板管理
- 🔄 执行计划优化
- 🔄 性能监控
- 🔄 权限控制

## 注意事项

1. **数据源配置**：确保数据源配置正确且可连接
2. **SQL语法**：使用标准SQL语法，避免数据库特定的扩展
3. **权限控制**：确保有足够的数据库权限执行SQL
4. **性能考虑**：对于大数据量查询，建议设置合理的超时时间和行数限制

## 故障排除

### 常见问题
1. **保存失败**：检查必填字段是否完整
2. **执行失败**：检查数据源连接和SQL语法
3. **权限错误**：确认数据库用户权限
4. **超时错误**：调整超时时间设置

### 日志查看
- 应用程序日志记录详细的执行信息
- 可通过日志文件查看错误详情
- 支持不同级别的日志记录 

## 通用功能：SQL输出（到表/到Excel）

### 服务接口
- 接口：`ExcelProcessor.Core.Interfaces.ISqlOutputService`
- 实现：`ExcelProcessor.Core.Services.SqlOutputService`
- 依赖：`ISqlService`（底层执行），`ILogger<SqlOutputService>`

### DI注册
在 `ExcelProcessor.WPF/App.xaml.cs`：

```csharp
services.AddScoped<ISqlOutputService, SqlOutputService>();
```

### 用法示例
- 输出到数据表：
```csharp
var svc = App.Services.GetRequiredService<ISqlOutputService>();
await svc.OutputToTableAsync(
    sqlStatement: sql,
    queryDataSourceId: queryDsId,
    targetDataSourceId: targetDsId,
    targetTableName: targetTable,
    clearTableBeforeInsert: clearBeforeInsert,
    progressCallback: progress // 可为 null
);
```

- 输出到Excel：
```csharp
var svc = App.Services.GetRequiredService<ISqlOutputService>();
await svc.OutputToExcelAsync(
    sqlStatement: sql,
    queryDataSourceId: queryDsId,
    outputPath: outputPathWithXlsx, // 会确保目录存在
    sheetName: sheet,
    clearSheetBeforeOutput: clearBeforeOutput,
    progressCallback: progress // 可为 null
);
```

### 进度回调
- 使用 `ExcelProcessor.Core.Interfaces.ISqlProgressCallback`
- 常用回调方法：
  - `UpdateOperation(string)` 当前操作
  - `UpdateDetailMessage(string)` 详细信息
  - `UpdateProgress(double)` 0-100 进度
  - `UpdateStatistics(int processed, int total)` 统计
  - `IsCancelled()` 支持取消

> 页面中可实现适配器，将上述回调绑定到进度对话框或状态栏。

### 页面改造参考
`Controls/SqlManagementPage.xaml.cs` 已重构为使用 `ISqlOutputService`，其他页面可比照迁移，统一复用逻辑并减少重复代码。 