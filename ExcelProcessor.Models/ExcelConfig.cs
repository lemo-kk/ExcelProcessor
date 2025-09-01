using System;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// Excel配置模型
    /// </summary>
    public class ExcelConfig
    {
        public string Id { get; set; } = string.Empty;
        public string ConfigName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string TargetDataSourceId { get; set; } = string.Empty; // 改为string类型，匹配DataSourceConfig.Id
        public string TargetDataSourceName { get; set; } = string.Empty;
        public string TargetTableName { get; set; } = string.Empty;
        public string SheetName { get; set; } = string.Empty;
        public int HeaderRow { get; set; }
        public int DataStartRow { get; set; }
        public int MaxRows { get; set; }
        public bool SkipEmptyRows { get; set; }
        public bool SplitEachRow { get; set; }
        public bool ClearTableDataBeforeImport { get; set; }
        public bool EnableValidation { get; set; }
        public bool EnableTransaction { get; set; }
        public string ErrorHandlingStrategy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? CreatedByUserId { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        
        // 复选框选择状态
        public bool IsSelected { get; set; }
        
        // 兼容性属性
        public string TargetDataSource => TargetDataSourceName;
        public string CreateTime => CreatedAt;
    }
} 