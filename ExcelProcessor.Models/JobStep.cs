using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// 作业步骤模型
    /// </summary>
    public class JobStep
    {
        /// <summary>
        /// 步骤ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 作业ID（外键）
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// 步骤名称
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 步骤描述
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 步骤类型
        /// </summary>
        [Required]
        public StepType Type { get; set; }

        /// <summary>
        /// 步骤顺序
        /// </summary>
        public int OrderIndex { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Excel配置ID（当步骤类型为ExcelImport时使用）
        /// </summary>
        [MaxLength(50)]
        public string ExcelConfigId { get; set; } = string.Empty;

        /// <summary>
        /// SQL配置ID（当步骤类型为SqlExecution时使用）
        /// </summary>
        [MaxLength(50)]
        public string SqlConfigId { get; set; } = string.Empty;

        /// <summary>
        /// 超时时间（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// 重试间隔（秒）
        /// </summary>
        public int RetryIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// 失败时是否继续执行后续步骤
        /// </summary>
        public bool ContinueOnFailure { get; set; } = false;

        /// <summary>
        /// 依赖步骤ID列表（JSON格式存储）
        /// </summary>
        public string Dependencies { get; set; } = string.Empty;

        /// <summary>
        /// 依赖步骤ID列表（计算属性，非数据库字段）
        /// </summary>
        [JsonIgnore]
        public List<string> DependenciesList
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Dependencies))
                    return new List<string>();
                
                try
                {
                    return JsonSerializer.Deserialize<List<string>>(Dependencies) ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            set
            {
                Dependencies = value != null ? JsonSerializer.Serialize(value) : string.Empty;
            }
        }

        /// <summary>
        /// 条件表达式（决定是否执行此步骤）
        /// </summary>
        public string ConditionExpression { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 步骤配置对象（非数据库字段）
        /// </summary>
        [JsonIgnore]
        public object? ConfigObject { get; set; }
    }

    /// <summary>
    /// 步骤类型枚举
    /// </summary>
    public enum StepType
    {
        /// <summary>
        /// Excel导入步骤
        /// </summary>
        ExcelImport = 0,

        /// <summary>
        /// SQL执行步骤
        /// </summary>
        SqlExecution = 1,

        /// <summary>
        /// 数据处理步骤
        /// </summary>
        DataProcessing = 2,

        /// <summary>
        /// 文件操作步骤
        /// </summary>
        FileOperation = 3,

        /// <summary>
        /// 邮件发送步骤
        /// </summary>
        EmailSend = 4,

        /// <summary>
        /// 条件判断步骤
        /// </summary>
        Condition = 5,

        /// <summary>
        /// 循环步骤
        /// </summary>
        Loop = 6,

        /// <summary>
        /// 等待步骤
        /// </summary>
        Wait = 7,

        /// <summary>
        /// 自定义脚本步骤
        /// </summary>
        CustomScript = 8,

        /// <summary>
        /// 数据验证步骤
        /// </summary>
        DataValidation = 9,

        /// <summary>
        /// 报表生成步骤
        /// </summary>
        ReportGeneration = 10,

        /// <summary>
        /// 数据导出步骤
        /// </summary>
        DataExport = 11,

        /// <summary>
        /// 通知步骤
        /// </summary>
        Notification = 12
    }

    /// <summary>
    /// Excel导入步骤配置
    /// </summary>
    public class ExcelImportStepConfig
    {
        /// <summary>
        /// Excel配置ID
        /// </summary>
        public string ExcelConfigId { get; set; } = string.Empty;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 工作表名称
        /// </summary>
        public string SheetName { get; set; } = "Sheet1";

        /// <summary>
        /// 目标表名
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// 是否清空目标表
        /// </summary>
        public bool ClearTable { get; set; } = false;

        /// <summary>
        /// 目标数据源ID
        /// </summary>
        public string TargetDataSourceId { get; set; } = string.Empty;
    }

    /// <summary>
    /// SQL执行步骤配置
    /// </summary>
    public class SqlExecutionStepConfig
    {
        /// <summary>
        /// SQL配置ID
        /// </summary>
        public string SqlConfigId { get; set; } = string.Empty;

        /// <summary>
        /// SQL语句
        /// </summary>
        public string SqlStatement { get; set; } = string.Empty;

        /// <summary>
        /// 数据源ID
        /// </summary>
        public string DataSourceId { get; set; } = string.Empty;

        /// <summary>
        /// 参数列表
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 超时时间（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// 最大返回行数
        /// </summary>
        public int MaxRows { get; set; } = 1000;
    }

    /// <summary>
    /// 文件操作步骤配置
    /// </summary>
    public class FileOperationStepConfig
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public FileOperationType OperationType { get; set; }

        /// <summary>
        /// 源文件路径
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// 目标文件路径
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;

        /// <summary>
        /// 是否覆盖
        /// </summary>
        public bool Overwrite { get; set; } = false;

        /// <summary>
        /// 文件过滤器
        /// </summary>
        public string FileFilter { get; set; } = "*.*";
    }

    /// <summary>
    /// 文件操作类型枚举
    /// </summary>
    public enum FileOperationType
    {
        /// <summary>
        /// 复制文件
        /// </summary>
        Copy = 0,

        /// <summary>
        /// 移动文件
        /// </summary>
        Move = 1,

        /// <summary>
        /// 删除文件
        /// </summary>
        Delete = 2,

        /// <summary>
        /// 创建目录
        /// </summary>
        CreateDirectory = 3,

        /// <summary>
        /// 删除目录
        /// </summary>
        DeleteDirectory = 4,

        /// <summary>
        /// 压缩文件
        /// </summary>
        Compress = 5,

        /// <summary>
        /// 解压文件
        /// </summary>
        Extract = 6
    }

    /// <summary>
    /// 邮件发送步骤配置
    /// </summary>
    public class EmailSendStepConfig
    {
        /// <summary>
        /// 收件人列表
        /// </summary>
        public List<string> To { get; set; } = new List<string>();

        /// <summary>
        /// 抄送列表
        /// </summary>
        public List<string> Cc { get; set; } = new List<string>();

        /// <summary>
        /// 密送列表
        /// </summary>
        public List<string> Bcc { get; set; } = new List<string>();

        /// <summary>
        /// 邮件主题
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// 邮件内容
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// 是否HTML格式
        /// </summary>
        public bool IsHtml { get; set; } = false;

        /// <summary>
        /// 附件列表
        /// </summary>
        public List<string> Attachments { get; set; } = new List<string>();

        /// <summary>
        /// SMTP服务器配置
        /// </summary>
        public SmtpConfig SmtpConfig { get; set; } = new SmtpConfig();
    }

    /// <summary>
    /// SMTP配置
    /// </summary>
    public class SmtpConfig
    {
        /// <summary>
        /// SMTP服务器地址
        /// </summary>
        public string Server { get; set; } = string.Empty;

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; } = 587;

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用SSL
        /// </summary>
        public bool EnableSsl { get; set; } = true;
    }

    /// <summary>
    /// 数据导出步骤配置
    /// </summary>
    public class DataExportStepConfig
    {
        /// <summary>
        /// 导出类型（Excel, CSV, JSON等）
        /// </summary>
        public string ExportType { get; set; } = "Excel";

        /// <summary>
        /// 目标路径
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;

        /// <summary>
        /// 数据源ID
        /// </summary>
        public string DataSourceId { get; set; } = string.Empty;

        /// <summary>
        /// 查询SQL
        /// </summary>
        public string QuerySql { get; set; } = string.Empty;

        /// <summary>
        /// 导出选项（JSON格式）
        /// </summary>
        public string ExportOptions { get; set; } = string.Empty;
    }

    /// <summary>
    /// 等待步骤配置
    /// </summary>
    public class WaitStepConfig
    {
        /// <summary>
        /// 等待时间（秒）
        /// </summary>
        public int WaitSeconds { get; set; } = 60;

        /// <summary>
        /// 等待描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 条件步骤配置
    /// </summary>
    public class ConditionStepConfig
    {
        /// <summary>
        /// 条件表达式
        /// </summary>
        public string ConditionExpression { get; set; } = string.Empty;

        /// <summary>
        /// 条件为真时执行的步骤ID
        /// </summary>
        public string TrueStepId { get; set; } = string.Empty;

        /// <summary>
        /// 条件为假时执行的步骤ID
        /// </summary>
        public string FalseStepId { get; set; } = string.Empty;

        /// <summary>
        /// 条件描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
} 