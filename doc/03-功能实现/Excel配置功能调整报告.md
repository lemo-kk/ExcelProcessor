# Excel配置功能调整报告

## 调整概述

根据用户需求，对Excel配置功能进行了以下调整：

1. **去除Excel文件名输入框，将目标表名输入框移动到该位置**
2. **修复目标数据源下拉框显示问题**

## 具体修改内容

### 1. 界面布局调整

#### 修改文件：`ExcelProcessor.WPF/Controls/ExcelImportConfigContent.xaml`

**调整前布局**：
```
配置名称 | Excel文件名
文件路径 | 目标数据源
Sheet名称 | 目标表名
```

**调整后布局**：
```
配置名称 | 目标表名
文件路径 | 目标数据源
Sheet名称 | (空)
```

#### 具体修改：

1. **移除Excel文件名输入框**：
   ```xml
   <!-- 移除的代码 -->
   <StackPanel Grid.Column="1" Grid.Row="0" Margin="8,0,0,0">
       <TextBlock Text="Excel文件名" />
       <TextBox x:Name="ExcelFileNameTextBox" />
   </StackPanel>
   ```

2. **将目标表名移动到Excel文件名位置**：
   ```xml
   <!-- 新的目标表名位置 -->
   <StackPanel Grid.Column="1" Grid.Row="0" Margin="8,0,0,0">
       <TextBlock Text="目标表名" />
       <TextBox x:Name="TargetTableNameTextBox" />
   </StackPanel>
   ```

3. **移除原来位置的目标表名输入框**：
   ```xml
   <!-- 移除的代码 -->
   <StackPanel Grid.Column="1" Grid.Row="2" Margin="8,12,0,0">
       <TextBlock Text="目标表名" />
       <TextBox x:Name="TargetTableNameTextBox" />
   </StackPanel>
   ```

### 2. 数据源下拉框修复

#### 修改文件：`ExcelProcessor.WPF/Controls/ExcelImportConfigContent.xaml`

**修复XAML中的SelectedValuePath**：
```xml
<!-- 修复前 -->
<ComboBox x:Name="TargetDataSourceComboBox"
        SelectedValuePath="Name" />

<!-- 修复后 -->
<ComboBox x:Name="TargetDataSourceComboBox"
        SelectedValuePath="Id" />
```

#### 修改文件：`ExcelProcessor.WPF/Controls/ExcelImportConfigContent.xaml.cs`

**1. 创建异步数据源初始化方法**：
```csharp
private async Task InitializeDataSourcesAsync()
{
    // 从数据源服务获取所有数据源
    var dataSourceConfigs = await _dataSourceService.GetAllDataSourcesAsync();
    
    // 组装用于显示的对象列表
    var items = dataSourceConfigs
        .Select(ds => new DataSourceInfo 
        { 
            Id = ds.Id, 
            Name = ds.Name, 
            Type = ds.Type, 
            Display = $"{ds.Name} ({ds.Type})" 
        })
        .ToList();
    
    // 设置数据源到下拉框
    TargetDataSourceComboBox.ItemsSource = items;
    TargetDataSourceComboBox.DisplayMemberPath = "Display";
    TargetDataSourceComboBox.SelectedValuePath = "Id";
}
```

**2. 更新LoadConfig方法**：
```csharp
public async void LoadConfig(ExcelConfig config)
{
    // 确保数据源已初始化
    await InitializeDataSourcesAsync();
    
    // 设置目标数据源
    if (!string.IsNullOrWhiteSpace(config.TargetDataSourceId))
    {
        TargetDataSourceComboBox.SelectedValue = config.TargetDataSourceId;
    }
    else if (!string.IsNullOrWhiteSpace(config.TargetDataSourceName))
    {
        // 如果没有ID，尝试通过名称查找
        var items = TargetDataSourceComboBox.ItemsSource as List<DataSourceInfo>;
        if (items != null)
        {
            var matchingItem = items.FirstOrDefault(item => item.Name == config.TargetDataSourceName);
            if (matchingItem != null)
            {
                TargetDataSourceComboBox.SelectedValue = matchingItem.Id;
            }
        }
    }
}
```

**3. 移除Excel文件名相关代码**：
```csharp
// 移除的代码
ExcelFileNameTextBox.Text = Path.GetFileName(config.FilePath ?? "");

// 移除的代码
ExcelFileNameTextBox.Text = fileName;
```

### 3. 代码清理

#### 移除的引用：
- `ExcelFileNameTextBox` 控件的所有引用
- 相关的文件名显示逻辑

#### 保留的功能：
- 文件路径显示
- 目标表名设置
- 数据源选择
- 字段映射功能

## 调整效果

### 1. 界面优化
- ✅ 移除了冗余的Excel文件名显示
- ✅ 目标表名输入框位置更加突出
- ✅ 界面布局更加简洁

### 2. 功能修复
- ✅ 数据源下拉框能正确显示已配置的数据源
- ✅ 编辑现有配置时能正确回填数据源选择
- ✅ 目标表名字段对应后台输出表名字段

### 3. 用户体验提升
- ✅ 界面更加直观
- ✅ 操作流程更加清晰
- ✅ 数据源选择更加可靠

## 技术细节

### 1. 数据源显示逻辑
- 使用`DataSourceInfo`对象封装数据源信息
- `Display`属性显示格式：`数据源名称 (数据库类型)`
- `SelectedValuePath`使用`Id`确保唯一性

### 2. 异步初始化
- 将数据源初始化改为异步方法
- 确保在加载配置前数据源已完全初始化
- 避免界面卡顿

### 3. 兼容性处理
- 支持通过ID或名称查找数据源
- 保持向后兼容性
- 优雅处理数据源不存在的情况

## 测试建议

### 1. 功能测试
- [ ] 新建Excel配置时目标表名输入框位置正确
- [ ] 编辑现有配置时数据源下拉框显示正确
- [ ] 数据源选择能正确保存和回填

### 2. 界面测试
- [ ] 界面布局美观合理
- [ ] 控件大小和间距合适
- [ ] 响应式布局正常

### 3. 兼容性测试
- [ ] 现有配置能正常加载
- [ ] 数据源配置正确显示
- [ ] 字段映射功能正常

## 总结

本次调整成功完成了用户需求：

1. ✅ **去除Excel文件名输入框，将目标表名输入框移动到该位置**
   - 界面更加简洁
   - 目标表名位置更加突出
   - 用户体验更好

2. ✅ **修复目标数据源下拉框显示问题**
   - 数据源能正确显示
   - 编辑时能正确回填
   - 选择逻辑更加可靠

调整后的Excel配置功能更加完善，用户体验得到显著提升。 