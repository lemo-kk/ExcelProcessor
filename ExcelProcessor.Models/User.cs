using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// 用户模型
    /// </summary>
    public class User : INotifyPropertyChanged
    {
        private int _id;
        private string _username = string.Empty;
        private string _passwordHash = string.Empty;
        private string _displayName = string.Empty;
        private string _email = string.Empty;
        private UserRole _role = UserRole.User;
        private UserStatus _status = UserStatus.Active;
        private bool _isEnabled = true;
        private DateTime? _lastLoginTime;
        private DateTime _createdTime;
        private DateTime _updatedTime;
        private string _remarks = string.Empty;

        /// <summary>
        /// 用户ID
        /// </summary>
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 用户名
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 密码哈希
        /// </summary>
        [Required]
        public string PasswordHash
        {
            get => _passwordHash;
            set
            {
                _passwordHash = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 显示名称
        /// </summary>
        [Required]
        [StringLength(100)]
        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 邮箱
        /// </summary>
        [EmailAddress]
        [StringLength(100)]
        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 用户角色
        /// </summary>
        public UserRole Role
        {
            get => _role;
            set
            {
                _role = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 用户状态
        /// </summary>
        public UserStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        public DateTime? LastLoginTime
        {
            get => _lastLoginTime;
            set
            {
                _lastLoginTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime
        {
            get => _createdTime;
            set
            {
                _createdTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 创建时间（兼容性）
        /// </summary>
        public DateTime CreatedAt
        {
            get => _createdTime;
            set
            {
                _createdTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedTime
        {
            get => _updatedTime;
            set
            {
                _updatedTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 更新时间（兼容性）
        /// </summary>
        public DateTime UpdatedAt
        {
            get => _updatedTime;
            set
            {
                _updatedTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 用户权限列表
        /// </summary>
        public List<UserPermission> Permissions { get; set; } = new List<UserPermission>();

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string Remarks
        {
            get => _remarks;
            set
            {
                _remarks = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 用户角色枚举
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// 所有角色
        /// </summary>
        All = -1,

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
        /// 所有状态
        /// </summary>
        All = -1,

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