using System;
using System.Collections.Generic;

namespace ExcelProcessor.WPF.Models
{
    /// <summary>
    /// Excel导入测试结果
    /// </summary>
    public class ImportTestResult
    {
        /// <summary>
        /// 测试是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 测试结果消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 错误信息列表
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// 测试的配置信息
        /// </summary>
        public string ConfigName { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 工作表名称
        /// </summary>
        public string SheetName { get; set; }

        /// <summary>
        /// 标题行号
        /// </summary>
        public int HeaderRow { get; set; }

        /// <summary>
        /// 数据行数
        /// </summary>
        public int DataRowCount { get; set; }

        /// <summary>
        /// 列数
        /// </summary>
        public int ColumnCount { get; set; }

        /// <summary>
        /// 字段映射数量
        /// </summary>
        public int FieldMappingCount { get; set; }

        /// <summary>
        /// 测试时间
        /// </summary>
        public DateTime TestTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 数据预览（前5行）
        /// </summary>
        public List<Dictionary<string, object>> PreviewData { get; set; } = new List<Dictionary<string, object>>();

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static ImportTestResult Success(string message, string configName, string filePath, string sheetName, int headerRow, int dataRowCount, int columnCount, int fieldMappingCount, List<Dictionary<string, object>> previewData = null)
        {
            return new ImportTestResult
            {
                IsSuccess = true,
                Message = message,
                ConfigName = configName,
                FilePath = filePath,
                SheetName = sheetName,
                HeaderRow = headerRow,
                DataRowCount = dataRowCount,
                ColumnCount = columnCount,
                FieldMappingCount = fieldMappingCount,
                PreviewData = previewData ?? new List<Dictionary<string, object>>()
            };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static ImportTestResult Failure(string message, List<string> errors = null, List<string> warnings = null)
        {
            return new ImportTestResult
            {
                IsSuccess = false,
                Message = message,
                Errors = errors ?? new List<string>(),
                Warnings = warnings ?? new List<string>()
            };
        }

        /// <summary>
        /// 添加错误信息
        /// </summary>
        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Errors.Add(error);
            }
        }

        /// <summary>
        /// 添加警告信息
        /// </summary>
        public void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                Warnings.Add(warning);
            }
        }

        /// <summary>
        /// 获取详细的错误信息
        /// </summary>
        public string GetDetailedErrorMessage()
        {
            var message = Message;
            
            if (Errors.Count > 0)
            {
                message += "\n\n错误详情：";
                foreach (var error in Errors)
                {
                    message += $"\n• {error}";
                }
            }

            if (Warnings.Count > 0)
            {
                message += "\n\n警告信息：";
                foreach (var warning in Warnings)
                {
                    message += $"\n• {warning}";
                }
            }

            return message;
        }
    }
} 