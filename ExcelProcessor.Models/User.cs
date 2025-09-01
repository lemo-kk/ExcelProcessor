using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// 用户模型
    /// </summary>
    public class User
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码哈希
        /// </summary>
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// </summary>
        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 邮箱
        /// </summary>
        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        /// <summary>
        /// 用户角色
        /// </summary>
        public UserRole Role { get; set; } = UserRole.User;

        /// <summary>
        /// 用户状态
        /// </summary>
        public UserStatus Status { get; set; } = UserStatus.Active;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 最后登录时间
        /// </summary>
        public DateTime? LastLoginTime { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 创建时间（兼容性）
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间（兼容性）
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 用户权限列表
        /// </summary>
        public List<UserPermission> Permissions { get; set; } = new List<UserPermission>();

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// 用户角色枚举
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// 超级管理员
        /// </summary>
        SuperAdmin = 0,

        /// <summary>
        /// 管理员
        /// </summary>
        Admin = 1,

        /// <summary>
        /// 普通用户
        /// </summary>
        User = 2,

        /// <summary>
        /// 只读用户
        /// </summary>
        ReadOnly = 3,

        /// <summary>
        /// 文件处理员
        /// </summary>
        FileProcessor = 4,

        /// <summary>
        /// 数据管理员
        /// </summary>
        DataManager = 5,

        /// <summary>
        /// 审计员
        /// </summary>
        Auditor = 6
    }

    /// <summary>
    /// 用户状态枚举
    /// </summary>
    public enum UserStatus
    {
        /// <summary>
        /// 活跃
        /// </summary>
        Active = 0,

        /// <summary>
        /// 非活跃
        /// </summary>
        Inactive = 1,

        /// <summary>
        /// 锁定
        /// </summary>
        Locked = 2
    }
} 