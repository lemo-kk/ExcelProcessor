using System;
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
using System.Windows.Input;
using System.Collections.Generic;

namespace ExcelProcessor.WPF.Pages
{
    /// <summary>
    /// 用户管理页面
    /// </summary>
    public partial class UserManagementPage : Page, INotifyPropertyChanged
    {
        private readonly IUserService _userService;
        private readonly IServiceProvider _serviceProvider;
        private ObservableCollection<User> _users;
        private User _selectedUser;
        private string _searchText = string.Empty;
        private bool _isLoading = false;
        private int _totalUsers = 0;
        private int _adminCount = 0;
        private int _userCount = 0;
        private int _newUsersThisMonth = 0;
        private UserRole _selectedRoleFilter = UserRole.All;
        private UserStatus _selectedStatusFilter = UserStatus.All;
        private bool _showOnlyEnabled = false;
        private string _sortColumn = "CreatedTime";
        private bool _sortAscending = false;
        private List<User> _allUsers = new List<User>();
        private List<User> _selectedUsers = new List<User>();
        private bool _isAllSelected = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public UserManagementPage(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            DataContext = this;

            _serviceProvider = serviceProvider;
            _userService = serviceProvider.GetRequiredService<IUserService>();

            _users = new ObservableCollection<User>();

            LoadUsersAsync();
        }

        #region 属性

        public ObservableCollection<User> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged();
            }
        }

        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterUsers();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public int TotalUsers
        {
            get => _totalUsers;
            set
            {
                _totalUsers = value;
                OnPropertyChanged();
            }
        }

        public int AdminCount
        {
            get => _adminCount;
            set
            {
                _adminCount = value;
                OnPropertyChanged();
            }
        }

        public int UserCount
        {
            get => _userCount;
            set
            {
                _userCount = value;
                OnPropertyChanged();
            }
        }

        public int NewUsersThisMonth
        {
            get => _newUsersThisMonth;
            set
            {
                _newUsersThisMonth = value;
                OnPropertyChanged();
            }
        }

        public UserRole SelectedRoleFilter
        {
            get => _selectedRoleFilter;
            set
            {
                _selectedRoleFilter = value;
                OnPropertyChanged();
                FilterUsers();
            }
        }

        public UserStatus SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                _selectedStatusFilter = value;
                OnPropertyChanged();
                FilterUsers();
            }
        }

        public bool ShowOnlyEnabled
        {
            get => _showOnlyEnabled;
            set
            {
                _showOnlyEnabled = value;
                OnPropertyChanged();
                FilterUsers();
            }
        }

        public string SortColumn
        {
            get => _sortColumn;
            set
            {
                _sortColumn = value;
                OnPropertyChanged();
                SortUsers();
            }
        }

        public bool SortAscending
        {
            get => _sortAscending;
            set
            {
                _sortAscending = value;
                OnPropertyChanged();
                SortUsers();
            }
        }

        public List<User> SelectedUsers
        {
            get => _selectedUsers;
            set
            {
                _selectedUsers = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedUsersCount));
                OnPropertyChanged(nameof(CanPerformBulkOperations));
            }
        }

        public int SelectedUsersCount => SelectedUsers?.Count ?? 0;

        public bool IsAllSelected
        {
            get => _isAllSelected;
            set
            {
                _isAllSelected = value;
                OnPropertyChanged();
                if (value)
                {
                    SelectAllUsers();
                }
                else
                {
                    DeselectAllUsers();
                }
            }
        }

        public bool CanPerformBulkOperations => SelectedUsersCount > 0;

        #endregion

        #region 方法

        private async Task LoadUsersAsync()
        {
            try
            {
                IsLoading = true;
                var users = await _userService.GetAllUsersAsync();
                
                _allUsers = users.ToList();
                FilterUsers();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"加载用户数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterUsers()
        {
            if (_allUsers == null) return;

            var filteredUsers = _allUsers.AsEnumerable();

            // 搜索过滤
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filteredUsers = filteredUsers.Where(u => 
                    u.Username.ToLower().Contains(searchLower) ||
                    u.DisplayName.ToLower().Contains(searchLower) ||
                    u.Email.ToLower().Contains(searchLower));
            }

            // 角色过滤
            if (SelectedRoleFilter != UserRole.All)
            {
                filteredUsers = filteredUsers.Where(u => u.Role == SelectedRoleFilter);
            }

            // 状态过滤
            if (SelectedStatusFilter != UserStatus.All)
            {
                filteredUsers = filteredUsers.Where(u => u.Status == SelectedStatusFilter);
            }

            // 启用状态过滤
            if (ShowOnlyEnabled)
            {
                filteredUsers = filteredUsers.Where(u => u.IsEnabled);
            }

            // 排序
            filteredUsers = SortColumn switch
            {
                "Username" => SortAscending ? filteredUsers.OrderBy(u => u.Username) : filteredUsers.OrderByDescending(u => u.Username),
                "DisplayName" => SortAscending ? filteredUsers.OrderBy(u => u.DisplayName) : filteredUsers.OrderByDescending(u => u.DisplayName),
                "Email" => SortAscending ? filteredUsers.OrderBy(u => u.Email) : filteredUsers.OrderByDescending(u => u.Email),
                "Role" => SortAscending ? filteredUsers.OrderBy(u => u.Role) : filteredUsers.OrderByDescending(u => u.Role),
                "Status" => SortAscending ? filteredUsers.OrderBy(u => u.Status) : filteredUsers.OrderByDescending(u => u.Status),
                "CreatedTime" => SortAscending ? filteredUsers.OrderBy(u => u.CreatedTime) : filteredUsers.OrderByDescending(u => u.CreatedTime),
                _ => filteredUsers.OrderByDescending(u => u.CreatedTime)
            };

            Users.Clear();
            foreach (var user in filteredUsers)
            {
                Users.Add(user);
            }
        }

        private void SortUsers()
        {
            FilterUsers();
        }

        private void UpdateStatistics()
        {
            TotalUsers = Users.Count;
            AdminCount = Users.Count(u => u.Role == UserRole.Admin);
            UserCount = Users.Count(u => u.Role == UserRole.User);
            NewUsersThisMonth = Users.Count(u => u.CreatedTime.Month == DateTime.Now.Month && u.CreatedTime.Year == DateTime.Now.Year);
        }

        private async Task AddUserAsync()
        {
            try
            {
                var dialog = new UserEditDialog();
                if (dialog.ShowDialog() == true)
                {
                    await LoadUsersAsync();
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"添加用户失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task EditUserAsync(User user)
        {
            if (user == null) return;

            try
            {
                var dialog = new UserEditDialog(user);
                if (dialog.ShowDialog() == true)
                {
                    await LoadUsersAsync();
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"编辑用户失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteUserAsync(User user)
        {
            if (user == null) return;

            var result = Extensions.MessageBoxExtensions.Show($"确定要删除用户 '{user.Username}' 吗？", "确认删除", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _userService.DeleteUserAsync(user.Id);
                    await LoadUsersAsync();
                    Extensions.MessageBoxExtensions.Show("用户删除成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Extensions.MessageBoxExtensions.Show($"删除用户失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region 事件处理

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            _ = AddUserAsync();
        }

        private void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is User user)
            {
                _ = EditUserAsync(user);
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is User user)
            {
                _ = DeleteUserAsync(user);
            }
        }

        private void ToggleSortDirection_Click(object sender, RoutedEventArgs e)
        {
            SortAscending = !SortAscending;
        }


        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            IsAllSelected = !IsAllSelected;
        }

        private void UserCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is User user)
            {
                ToggleUserSelection(user);
            }
        }

        private void BulkDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            _ = BulkDeleteUsersAsync();
        }

        private void BulkEnableButton_Click(object sender, RoutedEventArgs e)
        {
            _ = BulkEnableUsersAsync();
        }

        private void BulkDisableButton_Click(object sender, RoutedEventArgs e)
        {
            _ = BulkDisableUsersAsync();
        }

        private void SelectAllUsers()
        {
            SelectedUsers = Users.ToList();
        }

        private void DeselectAllUsers()
        {
            SelectedUsers = new List<User>();
        }

        private void ToggleUserSelection(User user)
        {
            if (SelectedUsers.Contains(user))
            {
                SelectedUsers.Remove(user);
            }
            else
            {
                SelectedUsers.Add(user);
            }
            OnPropertyChanged(nameof(SelectedUsers));
            OnPropertyChanged(nameof(SelectedUsersCount));
            OnPropertyChanged(nameof(CanPerformBulkOperations));
        }

        private bool IsUserSelected(User user)
        {
            return SelectedUsers?.Contains(user) ?? false;
        }

        private async Task BulkDeleteUsersAsync()
        {
            if (SelectedUsersCount == 0) return;

            var result = Extensions.MessageBoxExtensions.Show($"确定要删除选中的 {SelectedUsersCount} 个用户吗？", "确认批量删除", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var tasks = SelectedUsers.Select(user => _userService.DeleteUserAsync(user.Id));
                    await Task.WhenAll(tasks);
                    
                    await LoadUsersAsync();
                    SelectedUsers.Clear();
                    Extensions.MessageBoxExtensions.Show($"成功删除 {SelectedUsersCount} 个用户", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Extensions.MessageBoxExtensions.Show($"批量删除用户失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task BulkEnableUsersAsync()
        {
            if (SelectedUsersCount == 0) return;

            try
            {
                var tasks = SelectedUsers.Select(async user =>
                {
                    user.IsEnabled = true;
                    user.UpdatedTime = DateTime.Now;
                    await _userService.UpdateUserAsync(user);
                });
                await Task.WhenAll(tasks);
                
                await LoadUsersAsync();
                SelectedUsers.Clear();
                Extensions.MessageBoxExtensions.Show($"成功启用 {SelectedUsersCount} 个用户", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"批量启用用户失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task BulkDisableUsersAsync()
        {
            if (SelectedUsersCount == 0) return;

            var result = Extensions.MessageBoxExtensions.Show($"确定要禁用选中的 {SelectedUsersCount} 个用户吗？", "确认批量禁用", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var tasks = SelectedUsers.Select(async user =>
                    {
                        user.IsEnabled = false;
                        user.UpdatedTime = DateTime.Now;
                        await _userService.UpdateUserAsync(user);
                    });
                    await Task.WhenAll(tasks);
                    
                    await LoadUsersAsync();
                    SelectedUsers.Clear();
                    Extensions.MessageBoxExtensions.Show($"成功禁用 {SelectedUsersCount} 个用户", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Extensions.MessageBoxExtensions.Show($"批量禁用用户失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 