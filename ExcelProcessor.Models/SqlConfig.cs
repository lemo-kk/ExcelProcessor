using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// SQL配置模型
    /// </summary>
    [Table("SqlConfigs")]
    public class SqlConfig
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// SQL名称
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// SQL分类
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 输出类型（数据表/Excel工作表）
        /// </summary>
        [Required]
        [StringLength(20)]
        public string OutputType { get; set; } = string.Empty;

        /// <summary>
        /// 输出目标（表名或工作表名）
        /// </summary>
        [Required]
        [StringLength(100)]
        public string OutputTarget { get; set; } = string.Empty;

        /// <summary>
        /// SQL描述
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// SQL语句
        /// </summary>
        [Required]
        public string SqlStatement { get; set; } = string.Empty;

        /// <summary>
        /// 数据源ID（关联数据源）
        /// </summary>
        public string? DataSourceId { get; set; }

        /// <summary>
        /// 输出数据源ID（当输出类型为数据表时使用）
        /// </summary>
        public string? OutputDataSourceId { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.Now;

        /// <summary>
        /// 创建用户ID
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// 最后修改用户ID
        /// </summary>
        public string? LastModifiedBy { get; set; }

        /// <summary>
        /// 参数化SQL参数（JSON格式）
        /// </summary>
        [StringLength(2000)]
        public string Parameters { get; set; } = string.Empty;

        /// <summary>
        /// 执行超时时间（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// 最大返回行数
        /// </summary>
        public int MaxRows { get; set; } = 10000;

        /// <summary>
        /// 是否允许删除目标表数据
        /// </summary>
        public bool AllowDeleteTarget { get; set; } = false;

        /// <summary>
        /// 是否在导入前清除目标表数据
        /// </summary>
        public bool ClearTargetBeforeImport { get; set; } = false;

        /// <summary>
        /// 是否清空Sheet页（当输出类型为Excel工作表时使用）
        /// </summary>
        public bool ClearSheetBeforeOutput { get; set; } = false;


        /// <summary>
        /// 导航属性：数据源
        /// </summary>
        [ForeignKey("DataSourceId")]
        public virtual DataSourceConfig? DataSource { get; set; }

        /// <summary>
        /// 导航属性：创建用户
        /// </summary>
        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; }

        /// <summary>
        /// 导航属性：最后修改用户
        /// </summary>
        [ForeignKey("LastModifiedBy")]
        public virtual User? LastModifiedByUser { get; set; }
    }

    /// <summary>
    /// SQL执行结果模型
    /// </summary>
    [Table("SqlExecutionResults")]
    public class SqlExecutionResult
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// SQL配置ID
        /// </summary>
        [Required]
        public string SqlConfigId { get; set; } = string.Empty;

        /// <summary>
        /// 执行状态
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 执行开始时间
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 执行结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 执行耗时（毫秒）
        /// </summary>
        public long Duration { get; set; }

        /// <summary>
        /// 影响行数
        /// </summary>
        public int AffectedRows { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 执行结果数据（JSON格式）
        /// </summary>
        public string? ResultData { get; set; }

        /// <summary>
        /// 执行用户ID
        /// </summary>
        public string? ExecutedBy { get; set; }

        /// <summary>
        /// 执行参数（JSON格式）
        /// </summary>
        public string? ExecutionParameters { get; set; }

        /// <summary>
        /// 导航属性：SQL配置
        /// </summary>
        [ForeignKey("SqlConfigId")]
        public virtual SqlConfig? SqlConfig { get; set; }

        /// <summary>
        /// 导航属性：执行用户
        /// </summary>
        [ForeignKey("ExecutedBy")]
        public virtual User? ExecutedByUser { get; set; }
    }

    /// <summary>
    /// SQL参数模型
    /// </summary>
    public class SqlParameter
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 默认值
        /// </summary>
        public string? DefaultValue { get; set; }
    }
} 
 