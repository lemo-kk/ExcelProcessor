using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// 配置引用模型
    /// </summary>
    public class ConfigurationReference
    {
        /// <summary>
        /// 引用ID
        /// </summary>
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 引用名称
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 引用描述
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 引用类型
        /// </summary>
        [Required]
        public ReferenceType Type { get; set; }

        /// <summary>
        /// 引用的配置ID
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ReferencedConfigId { get; set; } = string.Empty;

        /// <summary>
        /// 引用的配置名称（用于显示）
        /// </summary>
        [MaxLength(100)]
        public string ReferencedConfigName { get; set; } = string.Empty;

        /// <summary>
        /// 引用参数（JSON格式，用于覆盖原配置的某些参数）
        /// </summary>
        public string OverrideParameters { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 创建人
        /// </summary>
        [MaxLength(50)]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// 更新人
        /// </summary>
        [MaxLength(50)]
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// 引用参数字典（非数据库字段）
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 引用类型枚举
    /// </summary>
    public enum ReferenceType
    {
        /// <summary>
        /// Excel配置引用
        /// </summary>
        ExcelConfig = 0,

        /// <summary>
        /// SQL配置引用
        /// </summary>
        SqlConfig = 1,

        /// <summary>
        /// 数据源配置引用
        /// </summary>
        DataSourceConfig = 2
    }

    /// <summary>
    /// 配置引用执行结果
    /// </summary>
    public class ReferenceExecutionResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 执行结果数据
        /// </summary>
        public Dictionary<string, object> ResultData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 执行时间（秒）
        /// </summary>
        public double ExecutionTimeSeconds { get; set; }

        /// <summary>
        /// 引用的配置信息
        /// </summary>
        public object? ReferencedConfig { get; set; }

        /// <summary>
        /// 执行日志
        /// </summary>
        public List<string> ExecutionLogs { get; set; } = new List<string>();
    }
} 