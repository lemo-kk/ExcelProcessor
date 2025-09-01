using System;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// 系统配置模型
    /// </summary>
    public class SystemConfig
    {
        /// <summary>
        /// 配置键
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 配置值
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 配置描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 系统配置键常量
    /// </summary>
    public static class SystemConfigKeys
    {
        /// <summary>
        /// 默认输入文件路径
        /// </summary>
        public const string DefaultInputPath = "DefaultInputPath";

        /// <summary>
        /// 默认输出文件路径
        /// </summary>
        public const string DefaultOutputPath = "DefaultOutputPath";

        /// <summary>
        /// Excel模板文件路径
        /// </summary>
        public const string ExcelTemplatePath = "ExcelTemplatePath";

        /// <summary>
        /// 临时文件路径
        /// </summary>
        public const string TempFilePath = "TempFilePath";

        /// <summary>
        /// 是否使用相对路径
        /// </summary>
        public const string UseRelativePath = "UseRelativePath";

        /// <summary>
        /// 日志保留天数
        /// </summary>
        public const string LogRetentionDays = "LogRetentionDays";

        /// <summary>
        /// 最大文件大小(MB)
        /// </summary>
        public const string MaxFileSize = "MaxFileSize";

        /// <summary>
        /// 是否启用登录
        /// </summary>
        public const string EnableLogin = "EnableLogin";

        /// <summary>
        /// 自动保存间隔（分钟）
        /// </summary>
        public const string AutoSaveInterval = "AutoSaveInterval";

        /// <summary>
        /// 是否启用自动保存
        /// </summary>
        public const string AutoSaveEnabled = "AutoSaveEnabled";

        /// <summary>
        /// 是否启动时最小化
        /// </summary>
        public const string StartupMinimize = "StartupMinimize";

        /// <summary>
        /// 是否检查更新
        /// </summary>
        public const string CheckForUpdates = "CheckForUpdates";

        /// <summary>
        /// 日志级别
        /// </summary>
        public const string LogLevel = "LogLevel";

        /// <summary>
        /// 是否启用日志
        /// </summary>
        public const string EnableLogging = "EnableLogging";

        /// <summary>
        /// 是否启用通知
        /// </summary>
        public const string EnableNotifications = "EnableNotifications";

        /// <summary>
        /// 界面语言
        /// </summary>
        public const string Language = "Language";

        /// <summary>
        /// 界面主题
        /// </summary>
        public const string Theme = "Theme";

        /// <summary>
        /// 是否启用动画
        /// </summary>
        public const string EnableAnimations = "EnableAnimations";

        /// <summary>
        /// 最大最近文件数
        /// </summary>
        public const string MaxRecentFiles = "MaxRecentFiles";

        /// <summary>
        /// 是否关闭前确认
        /// </summary>
        public const string ConfirmBeforeClose = "ConfirmBeforeClose";

        /// <summary>
        /// 是否启用备份
        /// </summary>
        public const string EnableBackup = "EnableBackup";

        /// <summary>
        /// 备份保留天数
        /// </summary>
        public const string BackupRetentionDays = "BackupRetentionDays";
    }
} 