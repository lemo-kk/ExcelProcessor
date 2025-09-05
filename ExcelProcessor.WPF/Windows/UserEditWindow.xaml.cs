using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using ExcelProcessor.Models;

namespace ExcelProcessor.WPF.Windows
{
    public partial class UserEditWindow : Window, INotifyPropertyChanged
    {
        private User _user;
        private bool _isNewUser;
        private string _username = string.Empty;
        private string _displayName = string.Empty;
        private string _email = string.Empty;
        private Role _selectedRole;
        private UserStatus _selectedStatus = UserStatus.Active;
        private string _remarks = string.Empty;
        private bool _isFormValid = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public UserEditWindow(User user = null)
        {
            InitializeComponent();
            DataContext = this;

            _isNewUser = user == null;
            _user = user ?? new User();

            InitializeData();
            LoadUserData();
            ValidateForm();
        }

        #region 属性

        public string WindowTitle => _isNewUser ? "新增用户" : "编辑用户";
        public string WindowDescription => _isNewUser ? "创建新的用户账户" : "修改用户信息和权限";

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
                ValidateForm();
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged(nameof(DisplayName));
                ValidateForm();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged(nameof(Email));
                ValidateForm();
            }
        }

        public Role SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged(nameof(SelectedRole));
                ValidateForm();
            }
        }

        public UserStatus SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                _selectedStatus = value;
                OnPropertyChanged(nameof(SelectedStatus));
                ValidateForm();
            }
        }

        public string Remarks
        {
            get => _remarks;
            set
            {
                _remarks = value;
                OnPropertyChanged(nameof(Remarks));
                ValidateForm();
            }
        }

        public bool IsNewUser => _isNewUser;
        public bool IsUsernameEditable => _isNewUser;

        public bool IsFormValid
        {
            get => _isFormValid;
            set
            {
                _isFormValid = value;
                OnPropertyChanged(nameof(IsFormValid));
            }
        }

        public ObservableCollection<Role> AvailableRoles { get; set; } = new ObservableCollection<Role>();
        public ObservableCollection<UserStatus> UserStatuses { get; set; } = new ObservableCollection<UserStatus>();

        #endregion

        #region 初始化方法

        private void InitializeData()
        {
            // 初始化可用角色
            var roles = new List<Role>
            {
                new Role { Code = "SuperAdmin", Name = "超级管理员", Description = "拥有所有权限" },
                new Role { Code = "Admin", Name = "管理员", Description = "拥有大部分管理权限" },
                new Role { Code = "User", Name = "普通用户", Description = "基本操作权限" },
                new Role { Code = "ReadOnly", Name = "只读用户", Description = "仅查看权限" },
                new Role { Code = "FileProcessor", Name = "文件处理员", Description = "文件处理相关权限" },
                new Role { Code = "DataManager", Name = "数据管理员", Description = "数据管理相关权限" },
                new Role { Code = "Auditor", Name = "审计员", Description = "审计相关权限" }
            };

            AvailableRoles.Clear();
            foreach (var role in roles)
            {
                AvailableRoles.Add(role);
            }

            // 初始化用户状态
            UserStatuses.Clear();
            UserStatuses.Add(UserStatus.Active);
            UserStatuses.Add(UserStatus.Inactive);
            UserStatuses.Add(UserStatus.Locked);
        }

        private void LoadUserData()
        {
            if (!_isNewUser && _user != null)
            {
                Username = _user.Username;
                DisplayName = _user.DisplayName;
                Email = _user.Email ?? string.Empty;
                SelectedRole = AvailableRoles.FirstOrDefault(r => r.Code == _user.Role.ToString());
                SelectedStatus = _user.Status;
                Remarks = _user.Remarks ?? string.Empty;
            }
            else
            {
                // 新增用户时设置默认值
                SelectedRole = AvailableRoles.FirstOrDefault(r => r.Code == "User");
                SelectedStatus = UserStatus.Active;
            }
        }

        #endregion

        #region 事件处理

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateForm())
                {
                    Extensions.MessageBoxExtensions.Show("请检查表单信息是否正确填写。", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 如果是新增用户，验证密码
                if (_isNewUser)
                {
                    if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                    {
                        Extensions.MessageBoxExtensions.Show("请输入密码。", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                        PasswordBox.Focus();
                        return;
                    }

                    if (PasswordBox.Password != ConfirmPasswordBox.Password)
                    {
                        Extensions.MessageBoxExtensions.Show("两次输入的密码不一致。", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                        ConfirmPasswordBox.Focus();
                        return;
                    }

                    if (PasswordBox.Password.Length < 6)
                    {
                        Extensions.MessageBoxExtensions.Show("密码长度不能少于6位。", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                        PasswordBox.Focus();
                        return;
                    }
                }

                // 更新用户信息
                _user.Username = Username.Trim();
                _user.DisplayName = DisplayName.Trim();
                _user.Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
                _user.Role = Enum.Parse<UserRole>(SelectedRole.Code);
                _user.Status = SelectedStatus;
                _user.Remarks = string.IsNullOrWhiteSpace(Remarks) ? null : Remarks.Trim();

                if (_isNewUser)
                {
                    _user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordBox.Password);
                    _user.CreatedAt = DateTime.Now;
                }

                _user.UpdatedAt = DateTime.Now;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"保存用户信息时发生错误：{ex.Message}", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        #region 辅助方法

        private bool ValidateForm()
        {
            var isValid = true;

            // 验证用户名
            if (string.IsNullOrWhiteSpace(Username))
            {
                isValid = false;
            }
            else if (Username.Length < 3 || Username.Length > 20)
            {
                isValid = false;
            }

            // 验证显示名称
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                isValid = false;
            }
            else if (DisplayName.Length < 2 || DisplayName.Length > 50)
            {
                isValid = false;
            }

            // 验证邮箱格式（如果填写了的话）
            if (!string.IsNullOrWhiteSpace(Email))
            {
                try
                {
                    var addr = new System.Net.Mail.MailAddress(Email);
                    if (addr.Address != Email)
                    {
                        isValid = false;
                    }
                }
                catch
                {
                    isValid = false;
                }
            }

            // 验证角色选择
            if (SelectedRole == null)
            {
                isValid = false;
            }

            IsFormValid = isValid;
            return isValid;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 