using System;
using System.ComponentModel.DataAnnotations;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// Excel导入结果模型
    /// </summary>
    public class ExcelImportResult
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
        /// 导入批次号
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string BatchNumber { get; set; } = string.Empty;

        /// <summary>
        /// 导入开始时间
        /// </summary>
        [Required]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 导入结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 总行数
        /// </summary>
        [Required]
        public int TotalRows { get; set; }

        /// <summary>
        /// 成功导入行数
        /// </summary>
        [Required]
        public int SuccessRows { get; set; }

        /// <summary>
        /// 失败行数
        /// </summary>
        [Required]
        public int FailedRows { get; set; }

        /// <summary>
        /// 跳过行数
        /// </summary>
        [Required]
        public int SkippedRows { get; set; }

        /// <summary>
        /// 导入状态（Running/Completed/Failed/Cancelled）
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 错误信息
        /// </summary>
        [MaxLength(2000)]
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 执行用户ID
        /// </summary>
        public int? ExecutedByUserId { get; set; }

        /// <summary>
        /// 执行用户名称
        /// </summary>
        [MaxLength(100)]
        public string ExecutedByUserName { get; set; } = string.Empty;

        /// <summary>
        /// 处理进度（0-100）
        /// </summary>
        [Required]
        public int Progress { get; set; } = 0;

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
        [MaxLength(1000)]
        public string Remarks { get; set; } = string.Empty;
    }
} 