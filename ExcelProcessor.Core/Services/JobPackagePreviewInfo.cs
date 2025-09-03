using System;
using System.Collections.Generic;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 作业配置包预览信息
    /// </summary>
    public class JobPackagePreviewInfo
    {
        /// <summary>
        /// 包基本信息
        /// </summary>
        public PackageBasicInfo BasicInfo { get; set; } = new();

        /// <summary>
        /// 包含的配置项
        /// </summary>
        public PackageContents Contents { get; set; } = new();

        /// <summary>
        /// 依赖关系
        /// </summary>
        public List<PackageDependency> Dependencies { get; set; } = new();

        /// <summary>
        /// 潜在冲突
        /// </summary>
        public List<PackageConflict> PotentialConflicts { get; set; } = new();

        /// <summary>
        /// 导入建议
        /// </summary>
        public List<string> ImportRecommendations { get; set; } = new();

        /// <summary>
        /// 预估导入时间（秒）
        /// </summary>
        public int EstimatedImportTimeSeconds { get; set; }

        /// <summary>
        /// 包大小（字节）
        /// </summary>
        public long PackageSize { get; set; }

        /// <summary>
        /// 是否可以直接导入
        /// </summary>
        public bool CanImportDirectly { get; set; }

        /// <summary>
        /// 导入前需要确认的事项
        /// </summary>
        public List<string> ConfirmationRequired { get; set; } = new();
    }

    /// <summary>
    /// 包基本信息
    /// </summary>
    public class PackageBasicInfo
    {
        /// <summary>
        /// 包名称
        /// </summary>
        public string PackageName { get; set; } = string.Empty;

        /// <summary>
        /// 包版本
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 创建用户
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// 包描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 兼容性信息
        /// </summary>
        public string Compatibility { get; set; } = string.Empty;

        /// <summary>
        /// 标签
        /// </summary>
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// 包内容
    /// </summary>
    public class PackageContents
    {
        /// <summary>
        /// Excel配置数量
        /// </summary>
        public int ExcelConfigCount { get; set; }

        /// <summary>
        /// Excel配置列表
        /// </summary>
        public List<ContentItem> ExcelConfigs { get; set; } = new();

        /// <summary>
        /// SQL脚本数量
        /// </summary>
        public int SqlScriptCount { get; set; }

        /// <summary>
        /// SQL脚本列表
        /// </summary>
        public List<ContentItem> SqlScripts { get; set; } = new();

        /// <summary>
        /// 作业配置数量
        /// </summary>
        public int JobConfigCount { get; set; }

        /// <summary>
        /// 作业配置列表
        /// </summary>
        public List<ContentItem> JobConfigs { get; set; } = new();

        /// <summary>
        /// 数据源数量
        /// </summary>
        public int DataSourceCount { get; set; }

        /// <summary>
        /// 数据源列表
        /// </summary>
        public List<ContentItem> DataSources { get; set; } = new();

        /// <summary>
        /// 字段映射数量
        /// </summary>
        public int FieldMappingCount { get; set; }

        /// <summary>
        /// 字段映射列表
        /// </summary>
        public List<ContentItem> FieldMappings { get; set; } = new();

        /// <summary>
        /// 总配置项数量
        /// </summary>
        public int TotalItemCount => ExcelConfigCount + SqlScriptCount + JobConfigCount + DataSourceCount + FieldMappingCount;
    }

    /// <summary>
    /// 内容项
    /// </summary>
    public class ContentItem
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 项目类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 项目描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastModifiedTime { get; set; }

        /// <summary>
        /// 是否已存在（用于冲突检测）
        /// </summary>
        public bool AlreadyExists { get; set; }

        /// <summary>
        /// 冲突类型（如果存在冲突）
        /// </summary>
        public string? ConflictType { get; set; }
    }

    /// <summary>
    /// 包依赖关系
    /// </summary>
    public class PackageDependency
    {
        /// <summary>
        /// 依赖项名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 依赖项类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 依赖项版本
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 是否必需
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 是否已满足
        /// </summary>
        public bool IsSatisfied { get; set; }

        /// <summary>
        /// 依赖项描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 包冲突
    /// </summary>
    public class PackageConflict
    {
        /// <summary>
        /// 冲突类型
        /// </summary>
        public string ConflictType { get; set; } = string.Empty;

        /// <summary>
        /// 冲突描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 冲突级别（Low, Medium, High, Critical）
        /// </summary>
        public string Severity { get; set; } = string.Empty;

        /// <summary>
        /// 受影响的配置项
        /// </summary>
        public List<string> AffectedItems { get; set; } = new();

        /// <summary>
        /// 解决建议
        /// </summary>
        public List<string> ResolutionSuggestions { get; set; } = new();

        /// <summary>
        /// 是否阻止导入
        /// </summary>
        public bool BlocksImport { get; set; }
    }
} 