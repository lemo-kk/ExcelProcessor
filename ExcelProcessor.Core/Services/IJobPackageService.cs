using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelProcessor.Models;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 作业配置包服务接口
    /// </summary>
    public interface IJobPackageService
    {
        #region 导出相关

        /// <summary>
        /// 导出作业配置包
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <param name="filePath">导出文件路径</param>
        /// <param name="options">导出选项</param>
        /// <returns>导出结果</returns>
        Task<(bool success, string message)> ExportJobPackageAsync(string jobId, string filePath, ExportOptions options);

        /// <summary>
        /// 生成作业配置包预览信息
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <returns>预览信息</returns>
        Task<JobPackagePreviewInfo> GenerateJobPackagePreviewAsync(string jobId);

        #endregion

        #region 导入相关

        /// <summary>
        /// 导入作业配置包
        /// </summary>
        /// <param name="filePath">配置文件路径</param>
        /// <param name="options">导入选项</param>
        /// <returns>导入结果</returns>
        Task<(bool success, string message)> ImportJobPackageAsync(string filePath, ImportOptions options);

        /// <summary>
        /// 验证作业配置包
        /// </summary>
        /// <param name="filePath">配置文件路径</param>
        /// <returns>验证结果</returns>
        Task<(bool isValid, string message, object validationDetails)> ValidateJobPackageAsync(string filePath);

        #endregion

        #region 内容分析

        /// <summary>
        /// 分析配置包内容
        /// </summary>
        /// <param name="filePath">配置文件路径</param>
        /// <returns>包内容信息</returns>
        Task<JobPackageContent> AnalyzePackageContentAsync(string filePath);

        /// <summary>
        /// 检测配置冲突
        /// </summary>
        /// <param name="filePath">配置文件路径</param>
        /// <returns>冲突列表</returns>
        Task<List<string>> DetectConflictsAsync(string filePath);

        #endregion
    }

    /// <summary>
    /// 导出选项
    /// </summary>
    public class ExportOptions
    {
        /// <summary>
        /// 是否包含Excel配置数据
        /// </summary>
        public bool IncludeExcelConfigs { get; set; } = true;

        /// <summary>
        /// 是否包含SQL管理数据
        /// </summary>
        public bool IncludeSqlScripts { get; set; } = true;

        /// <summary>
        /// 是否包含作业配置
        /// </summary>
        public bool IncludeJobConfig { get; set; } = true;

        /// <summary>
        /// 是否包含数据源配置
        /// </summary>
        public bool IncludeDataSources { get; set; } = true;

        /// <summary>
        /// 是否包含字段映射配置
        /// </summary>
        public bool IncludeFieldMappings { get; set; } = true;

        /// <summary>
        /// 是否包含执行历史记录
        /// </summary>
        public bool IncludeExecutionHistory { get; set; } = false;

        /// <summary>
        /// 是否包含作业步骤配置
        /// </summary>
        public bool IncludeJobSteps { get; set; } = true;
    }

    /// <summary>
    /// 导入选项
    /// </summary>
    public class ImportOptions
    {
        /// <summary>
        /// 是否覆盖现有配置
        /// </summary>
        public bool OverwriteExisting { get; set; } = true;

        /// <summary>
        /// 是否自动创建数据源连接
        /// </summary>
        public bool AutoCreateDataSources { get; set; } = true;

        /// <summary>
        /// 导入后是否自动启用作业
        /// </summary>
        public bool AutoEnableJobs { get; set; } = false;

        /// <summary>
        /// 是否跳过冲突检测
        /// </summary>
        public bool SkipConflictDetection { get; set; } = false;
    }

    /// <summary>
    /// 作业配置包内容
    /// </summary>
    public class JobPackageContent
    {
        /// <summary>
        /// 包版本
        /// </summary>
        public string PackageVersion { get; set; } = string.Empty;

        /// <summary>
        /// 导出时间
        /// </summary>
        public DateTime ExportTime { get; set; }

        /// <summary>
        /// 导出用户
        /// </summary>
        public string ExportedBy { get; set; } = string.Empty;

        /// <summary>
        /// Excel配置列表
        /// </summary>
        public List<string> ExcelConfigs { get; set; } = new();

        /// <summary>
        /// SQL脚本列表
        /// </summary>
        public List<string> SqlScripts { get; set; } = new();

        /// <summary>
        /// 作业配置列表
        /// </summary>
        public List<string> JobConfigs { get; set; } = new();

        /// <summary>
        /// 数据源列表
        /// </summary>
        public List<string> DataSources { get; set; } = new();

        /// <summary>
        /// 字段映射列表
        /// </summary>
        public List<string> FieldMappings { get; set; } = new();

        /// <summary>
        /// 依赖项列表
        /// </summary>
        public List<string> Dependencies { get; set; } = new();

        /// <summary>
        /// 包大小（字节）
        /// </summary>
        public long PackageSize { get; set; }

        /// <summary>
        /// 预估导入时间（秒）
        /// </summary>
        public int EstimatedImportTimeSeconds { get; set; }
    }
} 