using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// 作业配置模型
    /// </summary>
    public class JobConfig
    {
        /// <summary>
        /// 作业ID（主键）
        /// </summary>
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 作业名称
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 作业描述
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 作业类型（数据导入、数据处理、报表生成等）
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 作业分类
        /// </summary>
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 作业状态
        /// </summary>
        public JobStatus Status { get; set; } = JobStatus.Pending;

        /// <summary>
        /// 优先级
        /// </summary>
        public JobPriority Priority { get; set; } = JobPriority.Normal;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 执行模式（手动、定时、触发）
        /// </summary>
        public ExecutionMode ExecutionMode { get; set; } = ExecutionMode.Manual;

        /// <summary>
        /// 定时表达式（Cron表达式）
        /// </summary>
        [MaxLength(100)]
        public string CronExpression { get; set; } = string.Empty;

        /// <summary>
        /// 超时时间（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 3600;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// 重试间隔（秒）
        /// </summary>
        public int RetryIntervalSeconds { get; set; } = 300;

        /// <summary>
        /// 是否并行执行
        /// </summary>
        public bool AllowParallelExecution { get; set; } = false;

        /// <summary>
        /// 作业步骤配置（JSON格式）
        /// </summary>
        public string StepsConfig { get; set; } = string.Empty;

        /// <summary>
        /// 输入参数配置（JSON格式）
        /// </summary>
        public string InputParameters { get; set; } = string.Empty;

        /// <summary>
        /// 业务开始日期
        /// </summary>
        public DateTime? BusinessStartDate { get; set; }

        /// <summary>
        /// 业务终止日期（可为空，表示无终止日期）
        /// </summary>
        public DateTime? BusinessEndDate { get; set; }

        /// <summary>
        /// 业务周期数值
        /// </summary>
        public int BusinessCycleValue { get; set; } = 1;

        /// <summary>
        /// 业务周期单位
        /// </summary>
        [MaxLength(20)]
        public string BusinessCycleUnit { get; set; } = "Days";

        /// <summary>
        /// 执行频次数值
        /// </summary>
        public int ExecutionFrequencyValue { get; set; } = 1;

        /// <summary>
        /// 执行频次单位
        /// </summary>
        [MaxLength(20)]
        public string ExecutionFrequencyUnit { get; set; } = "Days";

        /// <summary>
        /// 执行周期（秒）
        /// </summary>
        public int ExecutionIntervalSeconds { get; set; } = 3600;

        /// <summary>
        /// 执行频次模式（简单模式：秒数，复杂模式：Cron表达式）
        /// </summary>
        public ExecutionFrequencyMode FrequencyMode { get; set; } = ExecutionFrequencyMode.Simple;

        /// <summary>
        /// 简单模式下的执行间隔（秒）
        /// </summary>
        public int SimpleIntervalSeconds { get; set; } = 3600;

        /// <summary>
        /// 复杂模式下的Cron表达式
        /// </summary>
        [MaxLength(200)]
        public string ComplexCronExpression { get; set; } = string.Empty;

        /// <summary>
        /// 输出配置（JSON格式）
        /// </summary>
        public string OutputConfig { get; set; } = string.Empty;

        /// <summary>
        /// 通知配置（JSON格式）
        /// </summary>
        public string NotificationConfig { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 创建人
        /// </summary>
        [MaxLength(50)]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新人
        /// </summary>
        [MaxLength(50)]
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// 最后执行时间
        /// </summary>
        public DateTime? LastExecutionTime { get; set; }

        /// <summary>
        /// 下次执行时间
        /// </summary>
        public DateTime? NextExecutionTime { get; set; }

        /// <summary>
        /// 执行次数
        /// </summary>
        public int ExecutionCount { get; set; } = 0;

        /// <summary>
        /// 成功次数
        /// </summary>
        public int SuccessCount { get; set; } = 0;

        /// <summary>
        /// 失败次数
        /// </summary>
        public int FailureCount { get; set; } = 0;

        /// <summary>
        /// 平均执行时间（秒）
        /// </summary>
        public double AverageExecutionTime { get; set; } = 0;

        /// <summary>
        /// 备注信息
        /// </summary>
        [MaxLength(1000)]
        public string Remarks { get; set; } = string.Empty;

        /// <summary>
        /// 执行进度（0-100）
        /// </summary>
        [JsonIgnore]
        public int Progress { get; set; } = 0;

        /// <summary>
        /// 克隆作业配置
        /// </summary>
        /// <returns>新的作业配置副本</returns>
        public JobConfig Clone()
        {
            return new JobConfig
            {
                Id = this.Id,
                Name = this.Name,
                Description = this.Description,
                Type = this.Type,
                Category = this.Category,
                Status = this.Status,
                Priority = this.Priority,
                IsEnabled = this.IsEnabled,
                ExecutionMode = this.ExecutionMode,
                CronExpression = this.CronExpression,
                TimeoutSeconds = this.TimeoutSeconds,
                MaxRetryCount = this.MaxRetryCount,
                RetryIntervalSeconds = this.RetryIntervalSeconds,
                AllowParallelExecution = this.AllowParallelExecution,
                StepsConfig = this.StepsConfig,
                InputParameters = this.InputParameters,
                BusinessStartDate = this.BusinessStartDate,
                BusinessEndDate = this.BusinessEndDate,
                BusinessCycleValue = this.BusinessCycleValue,
                BusinessCycleUnit = this.BusinessCycleUnit,
                ExecutionFrequencyValue = this.ExecutionFrequencyValue,
                ExecutionFrequencyUnit = this.ExecutionFrequencyUnit,
                ExecutionIntervalSeconds = this.ExecutionIntervalSeconds,
                FrequencyMode = this.FrequencyMode,
                SimpleIntervalSeconds = this.SimpleIntervalSeconds,
                ComplexCronExpression = this.ComplexCronExpression,
                OutputConfig = this.OutputConfig,
                NotificationConfig = this.NotificationConfig,
                CreatedAt = this.CreatedAt,
                UpdatedAt = this.UpdatedAt,
                CreatedBy = this.CreatedBy,
                UpdatedBy = this.UpdatedBy,
                Remarks = this.Remarks,
                Progress = this.Progress,
                // 同步复制非持久化字段，方便编辑界面显示
                Steps = this.Steps != null ? new List<JobStep>(this.Steps) : new List<JobStep>(),
                Parameters = this.Parameters != null ? new List<JobParameter>(this.Parameters) : new List<JobParameter>()
            };
        }

        /// <summary>
        /// 作业步骤列表（非数据库字段）
        /// </summary>
        [JsonIgnore]
        public List<JobStep> Steps { get; set; } = new List<JobStep>();

        /// <summary>
        /// 输入参数列表（非数据库字段）
        /// </summary>
        [JsonIgnore]
        public List<JobParameter> Parameters { get; set; } = new List<JobParameter>();
    }



    /// <summary>
    /// 作业优先级枚举
    /// </summary>
    public enum JobPriority
    {
        /// <summary>
        /// 低优先级
        /// </summary>
        Low = 0,

        /// <summary>
        /// 普通优先级
        /// </summary>
        Normal = 1,

        /// <summary>
        /// 高优先级
        /// </summary>
        High = 2,

        /// <summary>
        /// 紧急优先级
        /// </summary>
        Urgent = 3
    }

    /// <summary>
    /// 执行模式枚举
    /// </summary>
    public enum ExecutionMode
    {
        /// <summary>
        /// 手动执行
        /// </summary>
        Manual = 0,

        /// <summary>
        /// 定时执行
        /// </summary>
        Scheduled = 1,

        /// <summary>
        /// 触发执行
        /// </summary>
        Triggered = 2,

        /// <summary>
        /// 事件驱动
        /// </summary>
        EventDriven = 3
    }

    /// <summary>
    /// 执行频次模式枚举
    /// </summary>
    public enum ExecutionFrequencyMode
    {
        /// <summary>
        /// 简单模式（按秒数间隔）
        /// </summary>
        Simple = 0,

        /// <summary>
        /// 复杂模式（Cron表达式）
        /// </summary>
        Complex = 1
    }
} 