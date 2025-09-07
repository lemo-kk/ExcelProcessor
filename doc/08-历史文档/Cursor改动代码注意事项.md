# Cursor改动代码注意事项

## 🎯 核心原则：精准修改，避免影响

### 1. 修改前准备
- **明确问题范围**: 在修改前，清楚定义需要修改的具体问题
- **识别影响范围**: 分析修改可能影响的其他代码部分
- **备份重要文件**: 对关键文件进行备份或确保有版本控制

### 2. 代码修改策略

#### 2.1 使用精确的搜索和替换
```bash
# 使用grep_search工具精确定位问题代码
# 示例：只搜索特定文件中的特定函数
grep_search(query="问题函数名", include_pattern="*.cs")
```

#### 2.2 使用search_replace工具进行精准修改
```bash
# 提供足够的上下文确保唯一性
# 包含修改点前后3-5行代码作为上下文
search_replace(
    file_path="目标文件路径",
    old_string="包含足够上下文的原始代码",
    new_string="修改后的代码"
)
```

#### 2.3 使用edit_file工具进行小范围修改
```bash
# 只修改指定行范围
edit_file(
    target_file="文件路径",
    start_line_one_indexed=开始行,
    end_line_one_indexed=结束行,
    instructions="明确的修改说明"
)
```

### 3. 修改时的注意事项

#### 3.1 上下文完整性
- **包含足够上下文**: 在修改时包含修改点前后3-5行代码
- **保持缩进一致**: 确保修改后的代码缩进与原有代码一致
- **保持格式一致**: 遵循原有的代码格式和风格

#### 3.2 避免意外修改
- **不要使用全局替换**: 避免对整个文件或项目进行全局修改
- **不要修改未指定的部分**: 严格按照问题范围进行修改
- **不要删除或修改注释**: 除非明确要求，否则保留原有注释

#### 3.3 保持代码结构
- **不改变函数签名**: 除非明确要求，否则保持函数参数和返回类型不变
- **不改变类结构**: 不添加或删除类的成员，除非明确要求
- **不改变命名空间**: 保持原有的命名空间结构

### 4. 修改后的验证

#### 4.1 编译检查
```bash
# 修改后立即编译检查
dotnet build --project ExcelProcessor.WPF
```

#### 4.2 功能测试
```bash
# 运行项目验证修改是否生效
dotnet run --project ExcelProcessor.WPF
```

#### 4.3 回归测试
- 确保修改没有破坏现有功能
- 验证相关功能仍然正常工作
- 检查是否有新的编译错误或警告

### 5. 常见错误避免

#### 5.1 不要做的事情
- ❌ **全局搜索替换**: 避免对整个项目进行全局修改
- ❌ **修改不相关代码**: 不要修改与问题无关的代码部分
- ❌ **删除重要注释**: 不要删除重要的代码注释
- ❌ **改变代码结构**: 不要随意改变类的结构或函数签名
- ❌ **修改配置文件**: 不要修改项目配置文件，除非明确要求

#### 5.2 应该做的事情
- ✅ **精确定位问题**: 使用精确的搜索定位问题代码
- ✅ **提供足够上下文**: 在修改时提供足够的上下文代码
- ✅ **保持代码风格**: 遵循原有的代码风格和格式
- ✅ **验证修改效果**: 修改后立即验证是否解决了问题
- ✅ **记录修改内容**: 记录修改的具体内容和原因

### 6. 修改流程示例

#### 6.1 问题定位
```bash
# 1. 精确定位问题代码
grep_search(query="LoginButton_Click", include_pattern="*.cs")
```

#### 6.2 查看上下文
```bash
# 2. 查看问题代码的完整上下文
read_file(
    target_file="ExcelProcessor.WPF/Windows/LoginWindow.xaml.cs",
    start_line_one_indexed=50,
    end_line_one_indexed=80
)
```

#### 6.3 精准修改
```bash
# 3. 只修改问题部分，提供足够上下文
search_replace(
    file_path="ExcelProcessor.WPF/Windows/LoginWindow.xaml.cs",
    old_string="    private async void LoginButton_Click(object sender, RoutedEventArgs e)\n    {\n        // TODO: 实现登录逻辑\n        MessageBox.Show(\"登录功能待实现\");\n    }",
    new_string="    private async void LoginButton_Click(object sender, RoutedEventArgs e)\n    {\n        try\n        {\n            // 实现登录验证逻辑\n            if (string.IsNullOrEmpty(UsernameTextBox.Text))\n            {\n                MessageBox.Show(\"用户名不能为空\");\n                return;\n            }\n            // 其他登录逻辑...\n        }\n        catch (Exception ex)\n        {\n            MessageBox.Show($\"登录失败: {ex.Message}\");\n        }\n    }"
)
```

#### 6.4 验证修改
```bash
# 4. 编译验证
dotnet build --project ExcelProcessor.WPF

# 5. 运行验证
dotnet run --project ExcelProcessor.WPF
```

### 7. 特殊情况处理

#### 7.1 多文件修改
如果需要修改多个文件，应该：
- 逐个文件进行修改
- 每个文件修改后立即验证
- 确保文件间的依赖关系不受影响

#### 7.2 配置修改
如果需要修改配置文件：
- 只修改指定的配置项
- 保持其他配置项不变
- 修改后验证配置是否生效

#### 7.3 数据库相关修改
如果涉及数据库操作：
- 只修改指定的数据库操作代码
- 保持数据库连接配置不变
- 确保数据完整性不受影响

### 8. 修改记录模板

#### 8.1 修改记录格式
```
修改时间: [时间]
修改文件: [文件路径]
修改原因: [问题描述]
修改内容: [具体修改内容]
影响范围: [可能影响的其他部分]
验证结果: [修改后的验证结果]
```

#### 8.2 示例记录
```
修改时间: 2024-12-19 14:30
修改文件: ExcelProcessor.WPF/Windows/LoginWindow.xaml.cs
修改原因: 登录按钮点击事件未实现，需要添加登录验证逻辑
修改内容: 在LoginButton_Click方法中添加用户名验证和异常处理
影响范围: 登录功能，不影响其他窗口和页面
验证结果: 编译通过，登录验证功能正常工作
```

### 9. 总结

遵循以上注意事项可以确保：
- ✅ 只修改指定的问题代码
- ✅ 不影响其他功能和代码
- ✅ 保持代码质量和结构
- ✅ 便于后续维护和调试
- ✅ 减少意外错误和问题

记住：**精准修改，最小影响** 是代码修改的核心原则。 