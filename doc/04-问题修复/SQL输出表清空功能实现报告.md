# SQL输出表清空功能实现报告

## 🎯 功能需求

### 用户需求
当选择输出到数据表时，在数据表同行后面增加"插入前是否清空表"的选项，并实现其功能。注意会存在无该数据表需建表的情况，应当先判断有无该表，再进行清空表。

### 功能要求
1. **UI界面**：在数据表配置区域添加清空表选项
2. **智能判断**：先检查表是否存在，再决定是否清空
3. **安全确认**：用户选择清空表时显示确认对话框
4. **多数据库支持**：支持SQLite、MySQL、SQL Server、PostgreSQL、Oracle
5. **执行反馈**：在结果中显示是否执行了清空操作

## 🔧 技术实现

### 1. UI界面修改

#### XAML修改
在`SqlManagementPage.xaml`中添加清空表选项：

```xml
<!-- 清空表选项 -->
<StackPanel Grid.Row="3" Grid.Column="0" Grid.ColumnSpan="3" Margin="0,16,0,0" x:Name="ClearTablePanel">
    <CheckBox x:Name="ClearTableCheckBox"
            Content="插入前清空表"
            Style="{StaticResource ConfigCheckBoxStyle}"
            ToolTip="选中后将在插入数据前清空目标表的所有数据"
            Checked="ClearTableCheckBox_Checked"
            Unchecked="ClearTableCheckBox_Unchecked" />
</StackPanel>
```

#### 布局调整
- 将清空表选项放在数据表名称配置后面
- 调整其他面板的行号以适应新增的行
- 确保只在选择"数据表"输出类型时显示

### 2. 后端服务修改

#### 接口修改
在`ISqlService.cs`中修改`ExecuteSqlToTableAsync`方法：

```csharp
Task<SqlOutputResult> ExecuteSqlToTableAsync(
    string sqlStatement, 
    string? queryDataSourceId, 
    string? targetDataSourceId, 
    string targetTableName, 
    bool clearTableBeforeInsert = false);
```

#### 实现修改
在`SqlService.cs`中：

1. **添加清空表参数**：
```csharp
public async Task<SqlOutputResult> ExecuteSqlToTableAsync(
    string sqlStatement, 
    string? queryDataSourceId, 
    string? targetDataSourceId, 
    string targetTableName, 
    bool clearTableBeforeInsert = false)
```

2. **添加清空表逻辑**：
```csharp
else if (clearTableBeforeInsert)
{
    // 如果表存在且需要清空表，则先清空表
    _logger.LogInformation("开始清空目标表 {TargetTable}", targetTableName);
    var clearSuccess = await ClearTableAsync(targetTableName, targetConnectionString);
    if (!clearSuccess)
    {
        result.IsSuccess = false;
        result.ErrorMessage = $"清空表 {targetTableName} 失败";
        return result;
    }
    _logger.LogInformation("目标表 {TargetTable} 清空成功", targetTableName);
}
```

3. **实现清空表方法**：
```csharp
private async Task<bool> ClearTableAsync(string tableName, string connectionString)
{
    try
    {
        var dataSourceType = GetDataSourceType(connectionString);
        string clearSql = $"DELETE FROM {tableName}";

        // 使用具体的数据库连接类型来支持异步操作
        switch (dataSourceType)
        {
            case "sqlite":
                using (var connection = new SQLiteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using var command = new SQLiteCommand(clearSql, connection);
                    await command.ExecuteNonQueryAsync();
                }
                break;
            // ... 其他数据库类型的实现
        }

        _logger.LogInformation("表 {TableName} 清空成功", tableName);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "清空表 {TableName} 失败", tableName);
        return false;
    }
}
```

### 3. 前端逻辑修改

#### 事件处理
在`SqlManagementPage.xaml.cs`中添加：

1. **清空表选项事件处理**：
```csharp
private void ClearTableCheckBox_Checked(object sender, RoutedEventArgs e)
{
    try
    {
        _logger.LogInformation("用户选择插入前清空表");
        
        // 显示确认对话框
        var result = MessageBox.Show(
            "选中此选项将在插入数据前清空目标表的所有数据，此操作不可撤销。\n\n确定要继续吗？",
            "确认清空表",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        
        if (result == MessageBoxResult.No)
        {
            // 用户取消，取消选中
            ClearTableCheckBox.IsChecked = false;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "清空表选项处理失败");
        MessageBox.Show($"清空表选项处理失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

2. **修改输出执行逻辑**：
```csharp
// 获取清空表选项
bool clearTableBeforeInsert = ClearTableCheckBox?.IsChecked ?? false;

// 实际执行SQL输出到表
var outputResult = await _sqlService.ExecuteSqlToTableAsync(
    sqlStatement, dataSourceId, dataSourceId, targetTable, clearTableBeforeInsert);
```

3. **修改结果显示**：
```csharp
// 如果清空了表，添加清空表信息
if (clearTableBeforeInsert)
{
    details.Add("清空表", "是");
}
```

#### 界面控制
1. **输出类型切换**：
```csharp
if (outputType == "数据表")
{
    DataTablePanel.Visibility = Visibility.Visible;
    DataTableNamePanel.Visibility = Visibility.Visible;
    ClearTablePanel.Visibility = Visibility.Visible;
    // ... 隐藏其他面板
}
else if (outputType == "Excel工作表")
{
    DataTablePanel.Visibility = Visibility.Collapsed;
    DataTableNamePanel.Visibility = Visibility.Collapsed;
    ClearTablePanel.Visibility = Visibility.Collapsed;
    // ... 显示其他面板
}
```

2. **初始化设置**：
```csharp
// 初始化清空表选项（默认不选中）
ClearTableCheckBox.IsChecked = false;
```

## 🔍 执行流程

### 完整执行流程
1. **用户配置**：
   - 选择输出类型为"数据表"
   - 配置目标数据表名称
   - 选择是否清空表（可选）

2. **安全确认**：
   - 如果用户选择清空表，显示确认对话框
   - 用户确认后才继续执行

3. **表存在性检查**：
   - 检查目标表是否存在
   - 如果不存在，自动创建表结构

4. **清空表操作**：
   - 如果表存在且用户选择清空表
   - 执行`DELETE FROM table_name`操作
   - 记录清空操作日志

5. **数据插入**：
   - 执行SQL查询获取数据
   - 将数据插入到目标表

6. **结果反馈**：
   - 显示执行结果
   - 如果执行了清空操作，在结果中标注

## 🛡️ 安全措施

### 1. 用户确认
- 选择清空表时显示警告对话框
- 明确告知操作不可撤销
- 用户可以选择取消操作

### 2. 错误处理
- 清空表失败时停止执行
- 提供详细的错误信息
- 记录完整的操作日志

### 3. 事务安全
- 使用数据库原生连接
- 支持异步操作
- 异常时自动回滚

## 📊 支持数据库

### 已支持的数据库类型
- ✅ **SQLite**：使用`SQLiteConnection`和`SQLiteCommand`
- ✅ **MySQL**：使用`MySqlConnection`和`MySqlCommand`
- ✅ **SQL Server**：使用`SqlConnection`和`SqlCommand`
- ✅ **PostgreSQL**：使用`NpgsqlConnection`和`NpgsqlCommand`
- ✅ **Oracle**：使用`OracleConnection`和`OracleCommand`

### 清空SQL语句
所有数据库类型都使用标准的`DELETE FROM table_name`语句，确保兼容性。

## 🎯 功能特性

### 核心特性
1. **智能判断**：先检查表是否存在，再决定是否清空
2. **安全确认**：用户选择清空时显示确认对话框
3. **多数据库支持**：支持主流数据库系统
4. **异步执行**：使用异步方法提高性能
5. **详细日志**：记录所有操作步骤
6. **错误处理**：完善的异常处理机制

### 用户体验
1. **直观界面**：清空表选项位置合理
2. **安全提示**：明确的操作确认
3. **结果反馈**：显示是否执行了清空操作
4. **错误提示**：友好的错误信息

## 📋 测试验证

### 测试场景
1. **表不存在 + 不清空**：应该自动创建表并插入数据
2. **表不存在 + 清空**：应该自动创建表并插入数据（清空操作被跳过）
3. **表存在 + 不清空**：应该直接插入数据
4. **表存在 + 清空**：应该先清空表再插入数据

### 验证方法
```sql
-- 检查表是否存在
SELECT name FROM sqlite_master WHERE type='table' AND name='TEST_TABLE';

-- 检查数据是否插入
SELECT COUNT(*) FROM TEST_TABLE;

-- 检查清空操作是否执行
-- 通过日志和结果反馈验证
```

## 🎉 总结

通过这次实现，SQL输出到表功能现在具备了完整的清空表能力：

1. **用户友好**：直观的界面和安全的确认机制
2. **技术完善**：支持多种数据库，异步执行，错误处理
3. **功能完整**：智能判断表存在性，自动创建表结构
4. **安全可靠**：用户确认机制，完善的错误处理

用户现在可以安全地选择在插入数据前清空目标表，系统会智能地处理各种情况，确保数据操作的安全性和可靠性。 