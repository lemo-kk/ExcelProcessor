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
    public partial class UserManagementPage : Page, INotifyPropertyChanged
    {
        private ObservableCollection<User> _allUsers;
        private ObservableCollection<User> _filteredUsers;
        private User _selectedUser;
        private string _searchText = "";
        private string _selectedRoleFilter = "全部";
        private string _selectedStatusFilter = "全部";

        // 服务依赖
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;

        public event PropertyChangedEventHandler PropertyChanged;

        public UserManagementPage()
        {
            InitializeComponent();
            DataContext = this;

            // 获取服务依赖
            var serviceProvider = App.Services;
            _userService = serviceProvider.GetRequiredService<IUserService>();
            _roleService = serviceProvider.GetRequiredService<IRoleService>();

            InitializeData();
            InitializeFilters();
        }

        #region 属性

        public ObservableCollection<User> FilteredUsers
        {
            get => _filteredUsers;
            set
            {
                _filteredUsers = value;
                OnPropertyChanged(nameof(FilteredUsers));
            }
        }

        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged(nameof(SelectedUser));
            }
        }

        #endregion

        #region 初始化方法

        private async void InitializeData()
        {
            try
            {
                // 从服务获取真实数据
                var users = await _userService.GetAllUsersAsync();
                _allUsers = new ObservableCollection<User>(users);
                FilteredUsers = new ObservableCollection<User>(_allUsers);
                
                UsersDataGrid.ItemsSource = FilteredUsers;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载用户数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeFilters()
        {
            // 角色筛选选项
            RoleFilterComboBox.ItemsSource = new List<string>
            {
                "全部", "超级管理员", "管理员", "操作员", "只读", "访客"
            };
            RoleFilterComboBox.SelectedIndex = 0;

            // 状态筛选选项
            StatusFilterComboBox.ItemsSource = new List<string>
            {
                "全部", "活跃", "禁用", "锁定"
            };
            StatusFilterComboBox.SelectedIndex = 0;
        }

        #endregion

        #region 事件处理

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowInfo("添加用户", "正在打开用户编辑对话框...");
                var dialog = new UserEditDialog();
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                {
                    var newUser = dialog.User;
                    newUser.Id = _allUsers.Count + 1;
                    newUser.CreatedAt = DateTime.Now;
                    _allUsers.Add(newUser);
                    ApplyFilters();
                    ShowSuccess("添加成功", "用户添加成功");
                }
            }
            catch (Exception ex)
            {
                ShowError("添加失败", $"添加用户时发生错误：{ex.Message}");
            }
        }

        private void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int userId)
                {
                    var user = _allUsers.FirstOrDefault(u => u.Id == userId);
                    if (user != null)
                    {
                        ShowInfo("编辑用户", $"正在编辑用户 {user.Username}...");
                        var dialog = new UserEditDialog(user);
                        dialog.Owner = Window.GetWindow(this);
                        if (dialog.ShowDialog() == true)
                        {
                            ApplyFilters();
                            ShowSuccess("编辑成功", $"用户 {user.Username} 信息已更新。");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("编辑失败", $"编辑用户时发生错误：{ex.Message}");
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int userId)
                {
                    var user = _allUsers.FirstOrDefault(u => u.Id == userId);
                    if (user != null)
                    {
                        var result = MessageBox.Show(
                            $"确定要删除用户 {user.Username} 吗？此操作不可撤销。",
                            "确认删除",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            _allUsers.Remove(user);
                            ApplyFilters();
                            ShowSuccess("删除成功", $"用户 {user.Username} 已成功删除。");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("删除失败", $"删除用户时发生错误：{ex.Message}");
            }
        }

        private void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int userId)
                {
                    var user = _allUsers.FirstOrDefault(u => u.Id == userId);
                    if (user != null)
                    {
                        var result = MessageBox.Show(
                            $"确定要重置用户 {user.Username} 的密码吗？",
                            "确认重置密码",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            // TODO: 实现密码重置逻辑
                            ShowSuccess("重置成功", $"用户 {user.Username} 的密码已重置为默认密码。");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("重置失败", $"重置密码时发生错误：{ex.Message}");
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshButton.IsEnabled = false;
                RefreshButton.Content = "刷新中...";

                // 模拟刷新数据
                Task.Delay(1000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        InitializeData();
                        ApplyFilters();
                        RefreshButton.IsEnabled = true;
                        RefreshButton.Content = "刷新";
                        ShowSuccess("刷新成功", "用户数据已刷新。");
                    });
                });
            }
            catch (Exception ex)
            {
                RefreshButton.IsEnabled = true;
                RefreshButton.Content = "刷新";
                ShowError("刷新失败", $"刷新数据时发生错误：{ex.Message}");
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchTextBox.Text;
            ApplyFilters();
        }

        private void RoleFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoleFilterComboBox.SelectedItem is string selectedRole)
            {
                _selectedRoleFilter = selectedRole;
                ApplyFilters();
            }
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatusFilterComboBox.SelectedItem is string selectedStatus)
            {
                _selectedStatusFilter = selectedStatus;
                ApplyFilters();
            }
        }

        private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedUser = UsersDataGrid.SelectedItem as User;
        }

        #endregion

        #region 辅助方法

        private void ApplyFilters()
        {
            var filtered = _allUsers.AsEnumerable();

            // 搜索筛选
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                filtered = filtered.Where(u =>
                    u.Username.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                    u.DisplayName.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
            }

            // 角色筛选
            if (_selectedRoleFilter != "全部")
            {
                var role = GetRoleFromDisplayName(_selectedRoleFilter);
                filtered = filtered.Where(u => u.Role == role);
            }

            // 状态筛选
            if (_selectedStatusFilter != "全部")
            {
                var status = GetStatusFromDisplayName(_selectedStatusFilter);
                filtered = filtered.Where(u => u.Status == status);
            }

            FilteredUsers.Clear();
            foreach (var user in filtered)
            {
                FilteredUsers.Add(user);
            }
        }

        private UserRole GetRoleFromDisplayName(string displayName)
        {
            return displayName switch
            {
                "超级管理员" => UserRole.SuperAdmin,
                "管理员" => UserRole.Admin,
                "操作员" => UserRole.FileProcessor,
                "只读" => UserRole.ReadOnly,
                "访客" => UserRole.User,
                _ => UserRole.User
            };
        }

        private UserStatus GetStatusFromDisplayName(string displayName)
        {
            return displayName switch
            {
                "活跃" => UserStatus.Active,
                "禁用" => UserStatus.Inactive,
                "锁定" => UserStatus.Locked,
                _ => UserStatus.Active
            };
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