using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== 测试拆分逻辑 ===");
        
        // 模拟原始行数据
        var originalRow = new Dictionary<string, object>
        {
            ["A"] = "张三",
            ["B"] = "13800138000;13900139000",
            ["C"] = "北京市朝阳区;上海市浦东新区",
            ["D"] = "手机;电脑;平板"
        };
        
        Console.WriteLine("原始数据:");
        foreach (var kvp in originalRow)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }
        
        // 执行拆分
        var splitRows = SplitRowData(originalRow);
        
        Console.WriteLine($"\n拆分后生成 {splitRows.Count} 行数据:");
        for (int i = 0; i < splitRows.Count; i++)
        {
            Console.WriteLine($"\n第 {i + 1} 行:");
            foreach (var kvp in splitRows[i])
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
        }
    }
    
    static List<Dictionary<string, object>> SplitRowData(Dictionary<string, object> originalRow)
    {
        var splitRows = new List<Dictionary<string, object>>();
        
        Console.WriteLine("\n=== 开始拆分行数据 ===");
        Console.WriteLine($"原始行数据: {string.Join(", ", originalRow.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
        
        // 查找需要拆分的列（包含分隔符的单元格）
        var columnsToSplit = new Dictionary<string, List<string>>();
        var maxSplitCount = 1; // 至少有一行数据
        
        foreach (var kvp in originalRow)
        {
            var cellValue = kvp.Value?.ToString() ?? "";
            if (!string.IsNullOrEmpty(cellValue))
            {
                Console.WriteLine($"检查列 {kvp.Key}: {cellValue}");
                
                // 检查是否包含常见的分隔符
                var separators = new[] { ";", "；", ",", "，", "|", "\n", "\r\n", "\t" };
                foreach (var separator in separators)
                {
                    if (cellValue.Contains(separator))
                    {
                        Console.WriteLine($"发现分隔符 '{separator}' 在列 {kvp.Key}");
                        
                        var splitValues = cellValue.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(v => v.Trim())
                                                 .Where(v => !string.IsNullOrEmpty(v))
                                                 .ToList();
                        
                        Console.WriteLine($"拆分结果: {string.Join(" | ", splitValues)}");
                        
                        if (splitValues.Count > 1)
                        {
                            columnsToSplit[kvp.Key] = splitValues;
                            maxSplitCount = Math.Max(maxSplitCount, splitValues.Count);
                            Console.WriteLine($"列 {kvp.Key} 将被拆分为 {splitValues.Count} 个值");
                            break;
                        }
                    }
                }
            }
        }
        
        Console.WriteLine($"需要拆分的列数: {columnsToSplit.Count}");
        Console.WriteLine($"最大拆分数量: {maxSplitCount}");
        
        // 如果没有需要拆分的列，返回原始行
        if (columnsToSplit.Count == 0)
        {
            Console.WriteLine("没有需要拆分的列，返回原始行");
            splitRows.Add(originalRow);
            return splitRows;
        }
        
        // 创建拆分后的行
        for (int i = 0; i < maxSplitCount; i++)
        {
            var newRow = new Dictionary<string, object>();
            
            foreach (var kvp in originalRow)
            {
                if (columnsToSplit.ContainsKey(kvp.Key))
                {
                    // 使用拆分后的值
                    var splitValues = columnsToSplit[kvp.Key];
                    var value = i < splitValues.Count ? splitValues[i] : "";
                    newRow[kvp.Key] = value;
                    Console.WriteLine($"行 {i + 1}, 列 {kvp.Key}: {value}");
                }
                else
                {
                    // 保持原始值
                    newRow[kvp.Key] = kvp.Value;
                    Console.WriteLine($"行 {i + 1}, 列 {kvp.Key}: {kvp.Value} (原始值)");
                }
            }
            
            splitRows.Add(newRow);
        }
        
        Console.WriteLine($"拆分完成，共生成 {splitRows.Count} 行数据");
        Console.WriteLine("=== 拆分结束 ===");
        
        return splitRows;
    }
} 