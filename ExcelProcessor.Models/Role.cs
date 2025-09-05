using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// 角色模型
    /// </summary>
    public class Role : INotifyPropertyChanged
    {
        private int _id;
        private string _code = string.Empty;
        private string _name = string.Empty;
        private string _description = string.Empty;
        private RoleType _type = RoleType.Custom;
        private bool _isSystem = false;
        private bool _isEnabled = true;
        private int _sortOrder = 0;
        private DateTime _createdTime;
        private DateTime _updatedTime;
        private string _remarks = string.Empty;

        /// <summary>
        /// 角色ID
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
        /// 角色代码
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Code
        {
            get => _code;
            set
            {
                _code = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 角色名称
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 角色描述
        /// </summary>
        [StringLength(500)]
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 角色类型
        /// </summary>
        public RoleType Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否系统角色
        /// </summary>
        public bool IsSystem
        {
            get => _isSystem;
            set
            {
                _isSystem = value;
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
        /// 排序顺序
        /// </summary>
        public int SortOrder
        {
            get => _sortOrder;
            set
            {
                _sortOrder = value;
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
        /// 角色权限列表
        /// </summary>
        public List<RolePermission> Permissions { get; set; } = new List<RolePermission>();

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