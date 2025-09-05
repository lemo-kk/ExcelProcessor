using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using ExcelProcessor.Models;
using ExcelProcessor.Core.Services;
using ExcelProcessor.WPF.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.WPF.Pages
{
    /// <summary>
    /// 角色管理页面
    /// </summary>
    public partial class RoleManagementPage : Page, INotifyPropertyChanged
    {
        private readonly IRoleService _roleService;
        private readonly IServiceProvider _serviceProvider;
        private ObservableCollection<Role> _roles;
        private Role _selectedRole;
        private string _searchText = string.Empty;
        private bool _isLoading = false;
        private int _totalRoles = 0;
        private int _systemRolesCount = 0;
        private int _enabledRolesCount = 0;
        private int _disabledRolesCount = 0;
        private readonly ILogger<RoleManagementPage> _logger;
        private ObservableCollection<PermissionTreeNode> _selectedRolePermissions;

        public event PropertyChangedEventHandler PropertyChanged;

        public RoleManagementPage(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            DataContext = this;
            _serviceProvider = serviceProvider;
            _roleService = serviceProvider.GetRequiredService<IRoleService>();
            _roles = new ObservableCollection<Role>();
            _selectedRolePermissions = new ObservableCollection<PermissionTreeNode>();
            _logger = serviceProvider.GetRequiredService<ILogger<RoleManagementPage>>();
            LoadRolesAsync();
        }

        #region 属性

        public ObservableCollection<Role> Roles
        {
            get => _roles;
            set
            {
                _roles = value;
                OnPropertyChanged(nameof(Roles));
            }
        }

        public Role SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged(nameof(SelectedRole));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                _ = FilterRolesAsync();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public int TotalRoles
        {
            get => _totalRoles;
            set
            {
                _totalRoles = value;
                OnPropertyChanged(nameof(TotalRoles));
            }
        }

        public ObservableCollection<PermissionTreeNode> SelectedRolePermissions
        {
            get => _selectedRolePermissions;
            set
            {
                _selectedRolePermissions = value;
                OnPropertyChanged(nameof(SelectedRolePermissions));
            }
        }

        public int SystemRolesCount
        {
            get => _systemRolesCount;
            set
            {
                _systemRolesCount = value;
                OnPropertyChanged(nameof(SystemRolesCount));
            }
        }

        public int EnabledRolesCount
        {
            get => _enabledRolesCount;
            set
            {
                _enabledRolesCount = value;
                OnPropertyChanged(nameof(EnabledRolesCount));
            }
        }

        public int DisabledRolesCount
        {
            get => _disabledRolesCount;
            set
            {
                _disabledRolesCount = value;
                OnPropertyChanged(nameof(DisabledRolesCount));
            }
        }


        #endregion

        #region 数据加载

        private async Task LoadRolesAsync()
        {
            try
            {
                IsLoading = true;
                var roles = await _roleService.GetAllRolesAsync();
                Roles.Clear();
                foreach (var role in roles)
                {
                    Roles.Add(role);
                }
                
                // 计算统计信息
                TotalRoles = Roles.Count;
                SystemRolesCount = Roles.Count(r => r.IsSystem);
                EnabledRolesCount = Roles.Count(r => r.IsEnabled);
                DisabledRolesCount = Roles.Count(r => !r.IsEnabled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载角色数据失败");
                MessageBox.Show($"加载角色数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task FilterRolesAsync()
        {
            try
            {
                IsLoading = true;
                var filteredRoles = await _roleService.SearchRolesAsync(SearchText);
                Roles.Clear();
                foreach (var role in filteredRoles)
                {
                    Roles.Add(role);
                }
                
                // 计算筛选后的统计信息
                TotalRoles = Roles.Count;
                SystemRolesCount = Roles.Count(r => r.IsSystem);
                EnabledRolesCount = Roles.Count(r => r.IsEnabled);
                DisabledRolesCount = Roles.Count(r => !r.IsEnabled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "筛选角色数据失败");
                MessageBox.Show($"筛选角色数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadSelectedRolePermissionsAsync()
        {
            try
            {
                SelectedRolePermissions.Clear();
                
                if (SelectedRole == null)
                    return;

                // 模拟权限数据 - 实际项目中应该从服务获取
                var permissions = GenerateMockPermissions(SelectedRole);
                
                foreach (var permission in permissions)
                {
                    SelectedRolePermissions.Add(permission);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载角色权限失败");
            }
        }

        private ObservableCollection<PermissionTreeNode> GenerateMockPermissions(Role role)
        {
            var permissions = new ObservableCollection<PermissionTreeNode>();
            
            // 根据角色类型生成不同的权限
            if (role.Type == RoleType.System)
            {
                var systemNode = new PermissionTreeNode
                {
                    Name = "系统管理",
                    PermissionCode = "system",
                    Icon = "Cog",
                    Children = new ObservableCollection<PermissionTreeNode>
                    {
                        new PermissionTreeNode { Name = "用户管理", PermissionCode = "system.user", Icon = "Account" },
                        new PermissionTreeNode { Name = "角色管理", PermissionCode = "system.role", Icon = "Shield" },
                        new PermissionTreeNode { Name = "权限管理", PermissionCode = "system.permission", Icon = "Security" },
                        new PermissionTreeNode { Name = "系统设置", PermissionCode = "system.setting", Icon = "Settings" }
                    }
                };
                permissions.Add(systemNode);
                
                var dataNode = new PermissionTreeNode
                {
                    Name = "数据管理",
                    PermissionCode = "data",
                    Icon = "Database",
                    Children = new ObservableCollection<PermissionTreeNode>
                    {
                        new PermissionTreeNode { Name = "Excel导入", PermissionCode = "data.excel.import", Icon = "FileImport" },
                        new PermissionTreeNode { Name = "Excel导出", PermissionCode = "data.excel.export", Icon = "FileExport" },
                        new PermissionTreeNode { Name = "数据查询", PermissionCode = "data.query", Icon = "Search" },
                        new PermissionTreeNode { Name = "数据统计", PermissionCode = "data.statistics", Icon = "ChartLine" }
                    }
                };
                permissions.Add(dataNode);
            }
            else if (role.Name.Contains("数据管理员"))
            {
                var dataNode = new PermissionTreeNode
                {
                    Name = "数据管理",
                    PermissionCode = "data",
                    Icon = "Database",
                    Children = new ObservableCollection<PermissionTreeNode>
                    {
                        new PermissionTreeNode { Name = "Excel导入", PermissionCode = "data.excel.import", Icon = "FileImport" },
                        new PermissionTreeNode { Name = "Excel导出", PermissionCode = "data.excel.export", Icon = "FileExport" },
                        new PermissionTreeNode { Name = "数据查询", PermissionCode = "data.query", Icon = "Search" }
                    }
                };
                permissions.Add(dataNode);
            }
            else
            {
                var basicNode = new PermissionTreeNode
                {
                    Name = "基础权限",
                    PermissionCode = "basic",
                    Icon = "CheckCircle",
                    Children = new ObservableCollection<PermissionTreeNode>
                    {
                        new PermissionTreeNode { Name = "查看数据", PermissionCode = "basic.view", Icon = "Eye" },
                        new PermissionTreeNode { Name = "导出数据", PermissionCode = "basic.export", Icon = "FileExport" }
                    }
                };
                permissions.Add(basicNode);
            }
            
            return permissions;
        }

        #endregion

        #region 事件处理

        private void RolesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 角色选择变化时的处理
        }

        private void AddRoleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new RoleEditDialog();
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                {
                    _ = LoadRolesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加角色失败");
                MessageBox.Show($"添加角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditRoleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is Role role)
                {
                    var dialog = new RoleEditDialog(role);
                    dialog.Owner = Window.GetWindow(this);
                    if (dialog.ShowDialog() == true)
                    {
                        _ = LoadRolesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "编辑角色失败");
                MessageBox.Show($"编辑角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteRoleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is Role role)
                {
                    var result = MessageBox.Show($"确定要删除角色 '{role.Name}' 吗？", "确认删除", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        // 这里应该调用删除服务
                        MessageBox.Show("删除功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除角色失败");
                MessageBox.Show($"删除角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void CopyRoleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is Role role)
                {
                    var newRole = new Role
                    {
                        Id = 0,
                        Code = $"{role.Code}_Copy",
                        Name = $"{role.Name}_副本",
                        Description = role.Description,
                        Type = role.Type,
                        IsSystem = false,
                        IsEnabled = true,
                        SortOrder = role.SortOrder + 1,
                        CreatedTime = DateTime.Now,
                        UpdatedTime = DateTime.Now,
                        Remarks = role.Remarks
                    };

                    var dialog = new RoleEditDialog(newRole);
                    dialog.Owner = Window.GetWindow(this);
                    if (dialog.ShowDialog() == true)
                    {
                        _ = LoadRolesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制角色失败");
                MessageBox.Show($"复制角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 这里应该实现导出功能
                MessageBox.Show("导出功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出失败");
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SearchText = string.Empty;
                StatusFilterComboBox.SelectedIndex = 0;
                _ = LoadRolesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除筛选失败");
                MessageBox.Show($"清除筛选失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 