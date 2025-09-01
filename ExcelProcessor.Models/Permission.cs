using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// 权限模型
    /// </summary>
    public class Permission
    {
        /// <summary>
        /// 权限ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 权限代码
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 权限名称
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 权限描述
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// 权限类型
        /// </summary>
        public PermissionType Type { get; set; }

        /// <summary>
        /// 权限组
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Group { get; set; } = string.Empty;

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 权限分类
        /// </summary>
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// 用户权限关联模型
    /// </summary>
    public class UserPermission
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 权限ID
        /// </summary>
        public int PermissionId { get; set; }

        /// <summary>
        /// 是否授权
        /// </summary>
        public bool IsGranted { get; set; } = true;

        /// <summary>
        /// 授权时间
        /// </summary>
        public DateTime GrantedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 授权人ID
        /// </summary>
        public int? GrantedByUserId { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// 权限类型枚举
    /// </summary>
    public enum PermissionType
    {
        /// <summary>
        /// 菜单权限
        /// </summary>
        Menu = 0,

        /// <summary>
        /// 按钮权限
        /// </summary>
        Button = 1,

        /// <summary>
        /// 功能权限
        /// </summary>
        Function = 2,

        /// <summary>
        /// 数据权限
        /// </summary>
        Data = 3
    }

    /// <summary>
    /// 权限分组
    /// </summary>
    public static class PermissionGroups
    {
        /// <summary>
        /// 系统管理权限组
        /// </summary>
        public const string SystemManagement = "SystemManagement";

        /// <summary>
        /// 用户管理权限组
        /// </summary>
        public const string UserManagement = "UserManagement";

        /// <summary>
        /// 权限管理权限组
        /// </summary>
        public const string PermissionManagement = "PermissionManagement";

        /// <summary>
        /// 文件处理权限组
        /// </summary>
        public const string FileProcessing = "FileProcessing";

        /// <summary>
        /// 数据管理权限组
        /// </summary>
        public const string DataManagement = "DataManagement";

        /// <summary>
        /// 系统设置权限组
        /// </summary>
        public const string SystemSettings = "SystemSettings";

        /// <summary>
        /// 日志管理权限组
        /// </summary>
        public const string LogManagement = "LogManagement";
    }

    /// <summary>
    /// 权限组模型
    /// </summary>
    public class PermissionGroup
    {
        /// <summary>
        /// 组名
        /// </summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// 权限列表
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<PermissionItem> Permissions { get; set; } = new System.Collections.ObjectModel.ObservableCollection<PermissionItem>();
    }

    /// <summary>
    /// 权限项模型
    /// </summary>
    public class PermissionItem
    {
        /// <summary>
        /// 权限ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 权限名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 权限描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 权限类型
        /// </summary>
        public PermissionType Type { get; set; }

        /// <summary>
        /// 是否已授权
        /// </summary>
        public bool IsGranted { get; set; }

        /// <summary>
        /// 权限类型名称
        /// </summary>
        public string TypeName => Type switch
        {
            PermissionType.Menu => "菜单",
            PermissionType.Button => "按钮",
            PermissionType.Function => "功能",
            PermissionType.Data => "数据",
            _ => "未知"
        };

        /// <summary>
        /// 权限类型颜色
        /// </summary>
        public string TypeColor => Type switch
        {
            PermissionType.Menu => "#2196F3",
            PermissionType.Button => "#4CAF50",
            PermissionType.Function => "#FF9800",
            PermissionType.Data => "#9C27B0",
            _ => "#9E9E9E"
        };
    }

    /// <summary>
    /// 预定义权限代码
    /// </summary>
    public static class PermissionCodes
    {
        // 系统管理权限
        public const string SystemOverview = "SystemOverview";
        public const string SystemMonitor = "SystemMonitor";
        public const string SystemBackup = "SystemBackup";
        public const string SystemRestore = "SystemRestore";

        // 用户管理权限
        public const string UserView = "UserView";
        public const string UserCreate = "UserCreate";
        public const string UserEdit = "UserEdit";
        public const string UserDelete = "UserDelete";
        public const string UserEnable = "UserEnable";
        public const string UserDisable = "UserDisable";
        public const string UserResetPassword = "UserResetPassword";

        // 权限管理权限
        public const string PermissionView = "PermissionView";
        public const string PermissionAssign = "PermissionAssign";
        public const string PermissionRevoke = "PermissionRevoke";
        public const string RoleManage = "RoleManage";

        // 文件处理权限
        public const string FileUpload = "FileUpload";
        public const string FileDownload = "FileDownload";
        public const string FileDelete = "FileDelete";
        public const string FileProcess = "FileProcess";
        public const string FileExport = "FileExport";

        // 数据管理权限
        public const string DataView = "DataView";
        public const string DataCreate = "DataCreate";
        public const string DataEdit = "DataEdit";
        public const string DataDelete = "DataDelete";
        public const string DataImport = "DataImport";
        public const string DataExport = "DataExport";

        // 系统设置权限
        public const string SettingsView = "SettingsView";
        public const string SettingsEdit = "SettingsEdit";
        public const string SettingsReset = "SettingsReset";

        // 日志管理权限
        public const string LogView = "LogView";
        public const string LogExport = "LogExport";
        public const string LogClear = "LogClear";
    }
} 