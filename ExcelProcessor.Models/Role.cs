using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// 角色模型
    /// </summary>
    public class Role
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 角色代码
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 角色名称
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 角色描述
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// 角色类型
        /// </summary>
        public RoleType Type { get; set; }

        /// <summary>
        /// 是否系统角色
        /// </summary>
        public bool IsSystem { get; set; } = false;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 角色权限列表
        /// </summary>
        public List<RolePermission> Permissions { get; set; } = new List<RolePermission>();

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// 角色权限关联模型
    /// </summary>
    public class RolePermission
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        public int RoleId { get; set; }

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
    /// 角色类型枚举
    /// </summary>
    public enum RoleType
    {
        /// <summary>
        /// 系统角色
        /// </summary>
        System = 0,

        /// <summary>
        /// 自定义角色
        /// </summary>
        Custom = 1,

        /// <summary>
        /// 临时角色
        /// </summary>
        Temporary = 2
    }

    /// <summary>
    /// 预定义角色代码
    /// </summary>
    public static class RoleCodes
    {
        /// <summary>
        /// 超级管理员
        /// </summary>
        public const string SuperAdmin = "SuperAdmin";

        /// <summary>
        /// 系统管理员
        /// </summary>
        public const string SystemAdmin = "SystemAdmin";

        /// <summary>
        /// 普通用户
        /// </summary>
        public const string User = "User";

        /// <summary>
        /// 只读用户
        /// </summary>
        public const string ReadOnly = "ReadOnly";

        /// <summary>
        /// 文件处理员
        /// </summary>
        public const string FileProcessor = "FileProcessor";

        /// <summary>
        /// 数据管理员
        /// </summary>
        public const string DataManager = "DataManager";

        /// <summary>
        /// 审计员
        /// </summary>
        public const string Auditor = "Auditor";
    }
} 