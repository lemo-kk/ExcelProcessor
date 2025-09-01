using System;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// 系统设置模型
    /// </summary>
    public class SystemSettings
    {
        /// <summary>
        /// 是否启用自动保存
        /// </summary>
        public bool AutoSaveEnabled { get; set; } = true;

        /// <summary>
        /// 自动保存间隔（分钟）
        /// </summary>
        public int AutoSaveInterval { get; set; } = 5;

        /// <summary>
        /// 是否启动时最小化
        /// </summary>
        public bool StartupMinimize { get; set; } = false;

        /// <summary>
        /// 是否检查更新
        /// </summary>
        public bool CheckForUpdates { get; set; } = true;

        /// <summary>
        /// 是否启用日志
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// 日志级别
        /// </summary>
        public string LogLevel { get; set; } = "Info";

        /// <summary>
        /// 是否启用通知
        /// </summary>
        public bool EnableNotifications { get; set; } = true;

        /// <summary>
        /// 是否启用登录
        /// </summary>
        public bool EnableLogin { get; set; } = true;

        /// <summary>
        /// 语言
        /// </summary>
        public string Language { get; set; } = "简体中文";

        /// <summary>
        /// 主题
        /// </summary>
        public string Theme { get; set; } = "深色主题";

        /// <summary>
        /// 是否启用动画
        /// </summary>
        public bool EnableAnimations { get; set; } = true;

        /// <summary>
        /// 最大最近文件数
        /// </summary>
        public int MaxRecentFiles { get; set; } = 10;

        /// <summary>
        /// 关闭前确认
        /// </summary>
        public bool ConfirmBeforeClose { get; set; } = true;

        /// <summary>
        /// 是否启用备份
        /// </summary>
        public bool EnableBackup { get; set; } = true;

        /// <summary>
        /// 备份保留天数
        /// </summary>
        public int BackupRetentionDays { get; set; } = 30;
    }
} 