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
using ExcelProcessor.WPF.Dialogs;
using ExcelProcessor.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelProcessor.WPF.Pages
{
    public partial class RoleManagementPage : Page, INotifyPropertyChanged
    {
        private ObservableCollection<Role> _allRoles;
        private ObservableCollection<Role> _filteredRoles;
        private Role _selectedRole;
        private string _searchText = "";
        private string _selectedTypeFilter = "全部";

        // 服务依赖
        private readonly IRoleService _roleService;

        public event PropertyChangedEventHandler PropertyChanged;

        public RoleManagementPage()
        {
            InitializeComponent();
            DataContext = this;

            // 获取服务依赖
            var serviceProvider = App.Services;
            _roleService = serviceProvider.GetRequiredService<IRoleService>();

            InitializeData();
            InitializeFilters();
        }

        #region 属性

        public ObservableCollection<Role> FilteredRoles
        {
            get => _filteredRoles;
            set
            {
                _filteredRoles = value;
                OnPropertyChanged(nameof(FilteredRoles));
            }
        }

        public Role SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged(nameof(SelectedRole));
                UpdateRoleDetails();
            }
        }

        #endregion

        #region 初始化方法

        private async void InitializeData()
        {
            try
            {
                // 从服务获取真实数据
                var roles = await _roleService.GetAllRolesAsync();
                _allRoles = new ObservableCollection<Role>(roles);
                FilteredRoles = new ObservableCollection<Role>(_allRoles);
                
                RolesDataGrid.ItemsSource = FilteredRoles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载角色数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeFilters()
        {
            // 角色类型筛选
            RoleTypeFilterComboBox.ItemsSource = new List<string> { "全部", "系统角色", "自定义角色", "临时角色" };
            RoleTypeFilterComboBox.SelectedIndex = 0;
        }

        #endregion

        #region 事件处理

        private void AddRoleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowInfo("添加角色", "正在打开角色编辑对话框...");
                var dialog = new RoleEditDialog();
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                {
                    var newRole = dialog.Role;
                    newRole.Id = _allRoles.Count + 1;
                    newRole.CreatedTime = DateTime.Now;
                    _allRoles.Add(newRole);
                    ApplyFilters();
                    ShowSuccess("添加成功", "角色添加成功");
                }
            }
            catch (Exception ex)
            {
                ShowError("添加失败", $"添加角色时发生错误：{ex.Message}");
            }
        }

        private void EditRoleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var roleId = (int)button.Tag;
                var role = _allRoles.FirstOrDefault(r => r.Id == roleId);

                if (role != null)
                {
                    ShowInfo("编辑角色", $"正在编辑角色：{role.Name}");
                    var dialog = new RoleEditDialog(role);
                    dialog.Owner = Window.GetWindow(this);
                    if (dialog.ShowDialog() == true)
                    {
                        ApplyFilters();
                        ShowSuccess("编辑成功", "角色编辑成功");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("编辑失败", $"编辑角色时发生错误：{ex.Message}");
            }
        }

        private void DeleteRoleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var roleId = (int)button.Tag;
                var role = _allRoles.FirstOrDefault(r => r.Id == roleId);

                if (role != null)
                {
                    if (role.IsSystem)
                    {
                        ShowError("删除失败", "系统角色不能删除");
                        return;
                    }

                    var result = MessageBox.Show($"确定要删除角色 '{role.Name}' 吗？", "确认删除", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _allRoles.Remove(role);
                        ApplyFilters();
                        ShowSuccess("删除成功", "角色删除成功");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("删除失败", $"删除角色时发生错误：{ex.Message}");
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowInfo("刷新", "正在刷新角色数据...");
                InitializeData();
                ShowSuccess("刷新成功", "角色数据已刷新");
            }
            catch (Exception ex)
            {
                ShowError("刷新失败", $"刷新角色数据时发生错误：{ex.Message}");
            }
        }

        private void RoleSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = RoleSearchTextBox.Text;
            ApplyFilters();
        }

        private void RoleTypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedTypeFilter = RoleTypeFilterComboBox.SelectedItem?.ToString() ?? "全部";
            ApplyFilters();
        }

        private void RolesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedRole = RolesDataGrid.SelectedItem as Role;
        }

        #endregion

        #region 辅助方法

        private void ApplyFilters()
        {
            var filtered = _allRoles.AsEnumerable();

            // 搜索筛选
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                filtered = filtered.Where(r => 
                    r.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                    r.Code.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                    (r.Description?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // 类型筛选
            if (_selectedTypeFilter != "全部")
            {
                var roleType = _selectedTypeFilter switch
                {
                    "系统角色" => RoleType.System,
                    "自定义角色" => RoleType.Custom,
                    "临时角色" => RoleType.Temporary,
                    _ => RoleType.Custom
                };
                filtered = filtered.Where(r => r.Type == roleType);
            }

            FilteredRoles = new ObservableCollection<Role>(filtered);
            RolesDataGrid.ItemsSource = FilteredRoles;
        }

        private void UpdateRoleDetails()
        {
            if (_selectedRole != null)
            {
                RoleNameTextBlock.Text = _selectedRole.Name;
                RoleCodeTextBlock.Text = _selectedRole.Code;
                RoleTypeTextBlock.Text = _selectedRole.Type.ToString();
                RoleDescriptionTextBlock.Text = _selectedRole.Description ?? "-";
            }
            else
            {
                RoleNameTextBlock.Text = "未选择角色";
                RoleCodeTextBlock.Text = "-";
                RoleTypeTextBlock.Text = "-";
                RoleDescriptionTextBlock.Text = "-";
            }
        }

        private void ShowSuccess(string title, string message)
        {
            var snackbar = FindName("MainSnackbar") as Snackbar;
            if (snackbar != null)
            {
                snackbar.MessageQueue?.Enqueue(message, null, null, null, false, true, TimeSpan.FromSeconds(3));
            }
        }

        private void ShowError(string title, string message)
        {
            var snackbar = FindName("MainSnackbar") as Snackbar;
            if (snackbar != null)
            {
                snackbar.MessageQueue?.Enqueue(message, null, null, null, false, true, TimeSpan.FromSeconds(5));
            }
        }

        private void ShowInfo(string title, string message)
        {
            var snackbar = FindName("MainSnackbar") as Snackbar;
            if (snackbar != null)
            {
                snackbar.MessageQueue?.Enqueue(message, null, null, null, false, true, TimeSpan.FromSeconds(3));
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 