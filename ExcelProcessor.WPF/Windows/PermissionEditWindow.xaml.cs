using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using ExcelProcessor.Models;

namespace ExcelProcessor.WPF.Windows
{
    public partial class PermissionEditWindow : Window, INotifyPropertyChanged
    {
        private object _target;
        private string _targetName = string.Empty;
        private string _targetDescription = string.Empty;
        private string _searchText = string.Empty;
        private PermissionType? _selectedPermissionType;
        private int _grantedPermissionsCount = 0;
        private int _totalPermissionsCount = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public PermissionEditWindow(object target)
        {
            InitializeComponent();
            DataContext = this;

            _target = target;
            InitializeData();
            LoadTargetInfo();
            LoadPermissions();
            UpdatePermissionCounts();
        }

        #region 属性

        public string WindowTitle => $"权限管理 - {TargetName}";
        public string WindowDescription => $"为 {TargetName} 分配和管理权限";

        public string TargetName
        {
            get => _targetName;
            set
            {
                _targetName = value;
                OnPropertyChanged(nameof(TargetName));
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        public string TargetDescription
        {
            get => _targetDescription;
            set
            {
                _targetDescription = value;
                OnPropertyChanged(nameof(TargetDescription));
            }
        }

        public PackIconKind TargetIcon
        {
            get
            {
                return _target switch
                {
                    User user => PackIconKind.Account,
                    Role role => PackIconKind.ShieldAccount,
                    _ => PackIconKind.Shield
                };
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterPermissions();
            }
        }

        public PermissionType? SelectedPermissionType
        {
            get => _selectedPermissionType;
            set
            {
                _selectedPermissionType = value;
                OnPropertyChanged(nameof(SelectedPermissionType));
                FilterPermissions();
            }
        }

        public int GrantedPermissionsCount
        {
            get => _grantedPermissionsCount;
            set
            {
                _grantedPermissionsCount = value;
                OnPropertyChanged(nameof(GrantedPermissionsCount));
            }
        }

        public int TotalPermissionsCount
        {
            get => _totalPermissionsCount;
            set
            {
                _totalPermissionsCount = value;
                OnPropertyChanged(nameof(TotalPermissionsCount));
            }
        }

        public ObservableCollection<PermissionTypeFilter> PermissionTypes { get; set; } = new ObservableCollection<PermissionTypeFilter>();
        public ObservableCollection<PermissionGroup> PermissionGroups { get; set; } = new ObservableCollection<PermissionGroup>();
        public ObservableCollection<PermissionGroup> FilteredPermissionGroups { get; set; } = new ObservableCollection<PermissionGroup>();

        #endregion

        #region 初始化方法

        private void InitializeData()
        {
            // 初始化权限类型筛选
            PermissionTypes.Clear();
            PermissionTypes.Add(new PermissionTypeFilter { Name = "全部类型", Value = null });
            PermissionTypes.Add(new PermissionTypeFilter { Name = "菜单权限", Value = PermissionType.Menu });
            PermissionTypes.Add(new PermissionTypeFilter { Name = "按钮权限", Value = PermissionType.Button });
            PermissionTypes.Add(new PermissionTypeFilter { Name = "功能权限", Value = PermissionType.Function });
            PermissionTypes.Add(new PermissionTypeFilter { Name = "数据权限", Value = PermissionType.Data });

            // 初始化权限数据
            var permissionGroups = new List<PermissionGroup>
            {
                new PermissionGroup
                {
                    GroupName = "系统管理",
                    Permissions = new ObservableCollection<ExcelProcessor.Models.PermissionItem>
                    {
                        new ExcelProcessor.Models.PermissionItem { Id = 1, Name = "系统概览", Description = "查看系统概览信息", Type = PermissionType.Menu, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 2, Name = "系统监控", Description = "查看系统监控信息", Type = PermissionType.Menu, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 3, Name = "系统备份", Description = "执行系统备份", Type = PermissionType.Function, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 4, Name = "系统恢复", Description = "执行系统恢复", Type = PermissionType.Function, IsGranted = false }
                    }
                },
                new PermissionGroup
                {
                    GroupName = "用户管理",
                    Permissions = new ObservableCollection<ExcelProcessor.Models.PermissionItem>
                    {
                        new ExcelProcessor.Models.PermissionItem { Id = 5, Name = "查看用户", Description = "查看用户列表", Type = PermissionType.Menu, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 6, Name = "创建用户", Description = "创建新用户", Type = PermissionType.Button, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 7, Name = "编辑用户", Description = "编辑用户信息", Type = PermissionType.Button, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 8, Name = "删除用户", Description = "删除用户", Type = PermissionType.Button, IsGranted = false }
                    }
                },
                new PermissionGroup
                {
                    GroupName = "权限管理",
                    Permissions = new ObservableCollection<ExcelProcessor.Models.PermissionItem>
                    {
                        new ExcelProcessor.Models.PermissionItem { Id = 9, Name = "查看权限", Description = "查看权限列表", Type = PermissionType.Menu, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 10, Name = "分配权限", Description = "为用户分配权限", Type = PermissionType.Function, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 11, Name = "撤销权限", Description = "撤销用户权限", Type = PermissionType.Function, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 12, Name = "角色管理", Description = "管理角色", Type = PermissionType.Menu, IsGranted = false }
                    }
                },
                new PermissionGroup
                {
                    GroupName = "文件处理",
                    Permissions = new ObservableCollection<ExcelProcessor.Models.PermissionItem>
                    {
                        new ExcelProcessor.Models.PermissionItem { Id = 13, Name = "上传文件", Description = "上传Excel文件", Type = PermissionType.Button, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 14, Name = "下载文件", Description = "下载处理结果", Type = PermissionType.Button, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 15, Name = "删除文件", Description = "删除文件", Type = PermissionType.Button, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 16, Name = "处理文件", Description = "处理Excel文件", Type = PermissionType.Function, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 17, Name = "导出文件", Description = "导出处理结果", Type = PermissionType.Function, IsGranted = false }
                    }
                },
                new PermissionGroup
                {
                    GroupName = "数据管理",
                    Permissions = new ObservableCollection<ExcelProcessor.Models.PermissionItem>
                    {
                        new ExcelProcessor.Models.PermissionItem { Id = 18, Name = "查看数据", Description = "查看数据列表", Type = PermissionType.Menu, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 19, Name = "导入数据", Description = "导入数据", Type = PermissionType.Function, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 20, Name = "导出数据", Description = "导出数据", Type = PermissionType.Function, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 21, Name = "删除数据", Description = "删除数据", Type = PermissionType.Function, IsGranted = false }
                    }
                },
                new PermissionGroup
                {
                    GroupName = "系统设置",
                    Permissions = new ObservableCollection<ExcelProcessor.Models.PermissionItem>
                    {
                        new ExcelProcessor.Models.PermissionItem { Id = 22, Name = "查看设置", Description = "查看系统设置", Type = PermissionType.Menu, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 23, Name = "编辑设置", Description = "编辑系统设置", Type = PermissionType.Function, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 24, Name = "重置设置", Description = "重置系统设置", Type = PermissionType.Function, IsGranted = false }
                    }
                },
                new PermissionGroup
                {
                    GroupName = "日志管理",
                    Permissions = new ObservableCollection<ExcelProcessor.Models.PermissionItem>
                    {
                        new ExcelProcessor.Models.PermissionItem { Id = 25, Name = "查看日志", Description = "查看系统日志", Type = PermissionType.Menu, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 26, Name = "导出日志", Description = "导出系统日志", Type = PermissionType.Function, IsGranted = false },
                        new ExcelProcessor.Models.PermissionItem { Id = 27, Name = "清除日志", Description = "清除系统日志", Type = PermissionType.Function, IsGranted = false }
                    }
                }
            };

            PermissionGroups.Clear();
            foreach (var group in permissionGroups)
            {
                PermissionGroups.Add(group);
            }

            // 设置默认筛选
            SelectedPermissionType = null;
        }

        private void LoadTargetInfo()
        {
            switch (_target)
            {
                case User user:
                    TargetName = user.DisplayName;
                    TargetDescription = $"用户：{user.Username} | 角色：{user.Role} | 状态：{user.Status}";
                    break;
                case Role role:
                    TargetName = role.Name;
                    TargetDescription = $"角色：{role.Code} | {role.Description}";
                    break;
                default:
                    TargetName = "未知目标";
                    TargetDescription = "无法识别的目标类型";
                    break;
            }
        }

        private void LoadPermissions()
        {
            // TODO: 从数据库或配置中加载当前目标的权限
            // 这里模拟加载权限数据
            if (_target is User user)
            {
                // 根据用户角色设置默认权限
                SetDefaultPermissionsByRole(user.Role);
            }
            else if (_target is Role role)
            {
                // 根据角色设置默认权限
                SetDefaultPermissionsByRole(Enum.Parse<UserRole>(role.Code));
            }

            FilteredPermissionGroups.Clear();
            foreach (var group in PermissionGroups)
            {
                FilteredPermissionGroups.Add(group);
            }
        }

        private void SetDefaultPermissionsByRole(UserRole role)
        {
            // 根据角色设置默认权限
            switch (role)
            {
                case UserRole.SuperAdmin:
                    // 超级管理员拥有所有权限
                    foreach (var group in PermissionGroups)
                    {
                        foreach (var permission in group.Permissions)
                        {
                            permission.IsGranted = true;
                        }
                    }
                    break;
                case UserRole.Admin:
                    // 管理员拥有大部分权限
                    foreach (var group in PermissionGroups)
                    {
                        foreach (var permission in group.Permissions)
                        {
                            // 除了系统恢复等危险操作，其他都授权
                            permission.IsGranted = permission.Name != "系统恢复" && 
                                                  permission.Name != "清除日志";
                        }
                    }
                    break;
                case UserRole.User:
                    // 普通用户拥有基本权限
                    foreach (var group in PermissionGroups)
                    {
                        foreach (var permission in group.Permissions)
                        {
                            permission.IsGranted = permission.Type == PermissionType.Menu ||
                                                  permission.Name.Contains("查看") ||
                                                  permission.Name.Contains("上传") ||
                                                  permission.Name.Contains("下载") ||
                                                  permission.Name.Contains("处理");
                        }
                    }
                    break;
                case UserRole.ReadOnly:
                    // 只读用户只有查看权限
                    foreach (var group in PermissionGroups)
                    {
                        foreach (var permission in group.Permissions)
                        {
                            permission.IsGranted = permission.Type == PermissionType.Menu ||
                                                  permission.Name.Contains("查看");
                        }
                    }
                    break;
            }
        }

        #endregion

        #region 事件处理

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var group in FilteredPermissionGroups)
            {
                foreach (var permission in group.Permissions)
                {
                    permission.IsGranted = true;
                }
            }
            UpdatePermissionCounts();
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var group in FilteredPermissionGroups)
            {
                foreach (var permission in group.Permissions)
                {
                    permission.IsGranted = false;
                }
            }
            UpdatePermissionCounts();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: 保存权限配置到数据库
                MessageBox.Show("权限配置已成功保存。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存权限配置时发生错误：{ex.Message}", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        #region 辅助方法

        private void FilterPermissions()
        {
            FilteredPermissionGroups.Clear();

            foreach (var group in PermissionGroups)
            {
                var filteredGroup = new PermissionGroup
                {
                    GroupName = group.GroupName,
                    Permissions = new ObservableCollection<ExcelProcessor.Models.PermissionItem>()
                };

                foreach (var permission in group.Permissions)
                {
                    var matchesSearch = string.IsNullOrWhiteSpace(SearchText) ||
                                       permission.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                       permission.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

                    var matchesType = SelectedPermissionType == null || permission.Type == SelectedPermissionType.Value;

                    if (matchesSearch && matchesType)
                    {
                        filteredGroup.Permissions.Add(permission);
                    }
                }

                if (filteredGroup.Permissions.Count > 0)
                {
                    FilteredPermissionGroups.Add(filteredGroup);
                }
            }

            UpdatePermissionCounts();
        }

        private void UpdatePermissionCounts()
        {
            var total = 0;
            var granted = 0;

            foreach (var group in PermissionGroups)
            {
                foreach (var permission in group.Permissions)
                {
                    total++;
                    if (permission.IsGranted)
                    {
                        granted++;
                    }
                }
            }

            TotalPermissionsCount = total;
            GrantedPermissionsCount = granted;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    // 权限类型筛选模型
    public class PermissionTypeFilter
    {
        public string Name { get; set; } = string.Empty;
        public PermissionType? Value { get; set; }
    }
} 