using System;
using System.ComponentModel.DataAnnotations;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// Excel字段映射模型
    /// </summary>
    public class ExcelFieldMapping
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Excel配置ID
        /// </summary>
        [Required]
        public int ExcelConfigId { get; set; }

        /// <summary>
        /// Excel列名（原始列名）
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ExcelColumnName { get; set; } = string.Empty;

        /// <summary>
        /// Excel列索引（从0开始）
        /// </summary>
        [Required]
        public int ExcelColumnIndex { get; set; }

        /// <summary>
        /// 目标数据库字段名
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TargetFieldName { get; set; } = string.Empty;

        /// <summary>
        /// 目标数据库字段类型
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string TargetFieldType { get; set; } = string.Empty;

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// 默认值
        /// </summary>
        [MaxLength(500)]
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// 数据转换规则（JSON格式）
        /// </summary>
        [MaxLength(2000)]
        public string TransformRule { get; set; } = string.Empty;

        /// <summary>
        /// 数据验证规则（JSON格式）
        /// </summary>
        [MaxLength(2000)]
        public string ValidationRule { get; set; } = string.Empty;

        /// <summary>
        /// 排序顺序
        /// </summary>
        [Required]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [MaxLength(500)]
        public string Remarks { get; set; } = string.Empty;
    }
} 