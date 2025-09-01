using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using ExcelProcessor.Models;

namespace ExcelProcessor.WPF.Dialogs
{
    public partial class UserEditDialog : Window, INotifyPropertyChanged
    {
        private User _user;
        private bool _isEditMode;
        private Role _selectedRole;
        private ObservableCollection<Role> _availableRoles;

        public event PropertyChangedEventHandler PropertyChanged;

        public UserEditDialog(User user = null)
        {
            InitializeComponent();
            DataContext = this;

            _isEditMode = user != null;
            _user = user ?? new User
            {
                Id = 0,
                Username = "",
                DisplayName = "",
                Email = "",
                Role = UserRole.User,
                Status = UserStatus.Active,
                IsEnabled = true,
                CreatedTime = DateTime.Now,
                UpdatedTime = DateTime.Now
            };

            InitializeRoles();
            LoadUserData();
        }

        #region 属性

        public string DialogTitle => _isEditMode ? "编辑用户" : "添加用户";

        public User User
        {
            get => _user;
            set
            {
                _user = value;
                OnPropertyChanged(nameof(User));
            }
        }

        public bool IsEditMode => _isEditMode;

        public bool IsUsernameEditable => !_isEditMode; // 编辑模式下用户名不可修改

        public bool IsPasswordVisible => !_isEditMode; // 编辑模式下不显示密码字段

        public bool IsRoleDescriptionVisible => _selectedRole != null && !string.IsNullOrEmpty(_selectedRole.Description);

        public Role SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged(nameof(SelectedRole));
                OnPropertyChanged(nameof(IsRoleDescriptionVisible));
                
                if (_selectedRole != null)
                {
                    // 将角色信息同步到用户对象
                    _user.Role = GetUserRoleFromRole(_selectedRole);
                }
            }
        }

        public ObservableCollection<Role> AvailableRoles
        {
            get => _availableRoles;
            set
            {
                _availableRoles = value;
                OnPropertyChanged(nameof(AvailableRoles));
            }
        }

        public List<UserStatus> UserStatuses => Enum.GetValues(typeof(UserStatus)).Cast<UserStatus>().ToList();

        #endregion

        #region 初始化方法

        private void InitializeRoles()
        {
            // 模拟角色数据
            AvailableRoles = new ObservableCollection<Role>
            {
                new Role
                {
                    Id = 1,
                    Code = "SuperAdmin",
                    Name = "超级管理员",
                    Description = "拥有系统所有权限的最高管理员",
                    Type = RoleType.System,
                    IsSystem = true,
                    IsEnabled = true
                },
                new Role
                {
                    Id = 2,
                    Code = "Admin",
                    Name = "系统管理员",
                    Description = "负责系统管理和用户管理",
                    Type = RoleType.System,
                    IsSystem = true,
                    IsEnabled = true
                },
                new Role
                {
                    Id = 3,
                    Code = "FileProcessor",
                    Name = "文件处理员",
                    Description = "负责Excel文件的处理和分析",
                    Type = RoleType.Custom,
                    IsSystem = false,
                    IsEnabled = true
                },
                new Role
                {
                    Id = 4,
                    Code = "DataManager",
                    Name = "数据管理员",
                    Description = "负责数据管理和维护",
                    Type = RoleType.Custom,
                    IsSystem = false,
                    IsEnabled = true
                },
                new Role
                {
                    Id = 5,
                    Code = "ReadOnly",
                    Name = "只读用户",
                    Description = "只能查看数据，不能修改",
                    Type = RoleType.Custom,
                    IsSystem = false,
                    IsEnabled = true
                },
                new Role
                {
                    Id = 6,
                    Code = "Auditor",
                    Name = "审计员",
                    Description = "负责系统审计和日志查看",
                    Type = RoleType.Custom,
                    IsSystem = false,
                    IsEnabled = true
                }
            };
        }

        private void LoadUserData()
        {
            if (_isEditMode)
            {
                // 编辑模式：根据用户的角色找到对应的Role对象
                var role = AvailableRoles.FirstOrDefault(r => GetUserRoleFromRole(r) == _user.Role);
                if (role != null)
                {
                    SelectedRole = role;
                }
            }
            else
            {
                // 新增模式：默认选择普通用户角色
                var defaultRole = AvailableRoles.FirstOrDefault(r => r.Code == "ReadOnly");
                if (defaultRole != null)
                {
                    SelectedRole = defaultRole;
                }
            }
        }

        #endregion

        #region 事件处理

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ValidateInput())
                {
                    // 保存用户数据
                    SaveUserData();
                    
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        #region 辅助方法

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(_user.Username))
            {
                MessageBox.Show("请输入用户名", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_user.DisplayName))
            {
                MessageBox.Show("请输入显示名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                DisplayNameTextBox.Focus();
                return false;
            }

            if (!_isEditMode)
            {
                // 新增模式需要验证密码
                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    MessageBox.Show("请输入密码", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return false;
                }

                if (PasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    MessageBox.Show("两次输入的密码不一致", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConfirmPasswordBox.Focus();
                    return false;
                }

                if (PasswordBox.Password.Length < 6)
                {
                    MessageBox.Show("密码长度不能少于6位", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return false;
                }
            }

            if (_selectedRole == null)
            {
                MessageBox.Show("请选择用户角色", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                RoleComboBox.Focus();
                return false;
            }

            // 验证邮箱格式
            if (!string.IsNullOrWhiteSpace(_user.Email))
            {
                try
                {
                    var email = new System.Net.Mail.MailAddress(_user.Email);
                }
                catch
                {
                    MessageBox.Show("邮箱格式不正确", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    EmailTextBox.Focus();
                    return false;
                }
            }

            return true;
        }

        private void SaveUserData()
        {
            if (!_isEditMode)
            {
                // 新增模式：设置密码哈希
                _user.PasswordHash = HashPassword(PasswordBox.Password);
                _user.CreatedTime = DateTime.Now;
            }

            _user.UpdatedTime = DateTime.Now;
            
            // 根据选择的角色设置用户角色
            _user.Role = GetUserRoleFromRole(_selectedRole);
        }

        private string HashPassword(string password)
        {
            // 这里应该使用安全的密码哈希算法
            // 现在使用简单的哈希作为示例
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private UserRole GetUserRoleFromRole(Role role)
        {
            return role.Code switch
            {
                "SuperAdmin" => UserRole.SuperAdmin,
                "Admin" => UserRole.Admin,
                "FileProcessor" => UserRole.FileProcessor,
                "DataManager" => UserRole.DataManager,
                "ReadOnly" => UserRole.ReadOnly,
                "Auditor" => UserRole.Auditor,
                _ => UserRole.User
            };
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 