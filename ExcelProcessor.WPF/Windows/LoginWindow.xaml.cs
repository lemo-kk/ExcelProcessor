using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ExcelProcessor.Models;
using ExcelProcessor.WPF.Views;
using ExcelProcessor.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelProcessor.WPF.Windows
{
    public partial class LoginWindow : Window, INotifyPropertyChanged
    {
        private string _username = string.Empty;
        private string _statusMessage = string.Empty;
        private Brush _statusColor = Brushes.Gray;
        private bool _isFormValid = false;
        private bool _rememberPassword = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public LoginWindow()
        {
            InitializeComponent();
            DataContext = this;

            // 设置焦点到用户名输入框
            Loaded += (s, e) => UsernameTextBox.Focus();
            Loaded += (s, e) => LoadSavedCredentials();
        }

        #region 属性

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

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public Brush StatusColor
        {
            get => _statusColor;
            set
            {
                _statusColor = value;
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public bool IsFormValid
        {
            get => _isFormValid;
            set
            {
                _isFormValid = value;
                OnPropertyChanged(nameof(IsFormValid));
            }
        }

        public bool RememberPassword
        {
            get => _rememberPassword;
            set
            {
                _rememberPassword = value;
                OnPropertyChanged(nameof(RememberPassword));
            }
        }

        public User LoggedInUser { get; private set; }

        #endregion

        #region 事件处理

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            PerformLogin();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void UsernameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PasswordBox.Focus();
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformLogin();
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidateForm();
        }

        #endregion

        #region 登录逻辑

        private async void PerformLogin()
        {
            try
            {
                if (!ValidateForm())
                {
                    ShowStatus("请填写完整的登录信息", Brushes.Orange);
                    return;
                }

                // 禁用登录按钮
                LoginButton.IsEnabled = false;
                LoginButton.Content = "登录中...";
                ShowStatus("正在验证用户信息...", Brushes.Blue);

                // 通过 DI 获取认证服务并调用真实登录
                var authService = App.Services.GetRequiredService<IAuthService>();
                var result = await authService.LoginAsync(Username, PasswordBox.Password);

                if (result.Success && result.User != null)
                {
                    LoggedInUser = result.User;
                    ShowStatus(result.Message, Brushes.Green);

                    // 保存记住密码设置（如启用）
                    if (RememberPassword)
                    {
                        SaveLoginCredentials();
                    }

                    // 可选：缓存当前用户到应用属性
                    Application.Current.Properties["CurrentUser"] = result.User;

                    // 兜底：若当前没有主窗体，则直接创建并显示
                    if (Application.Current.MainWindow == null)
                    {
                        var mainWindow = new MainWindow();
                        Application.Current.MainWindow = mainWindow;
                        mainWindow.Show();
                    }

                    DialogResult = true;
                    Close();
                }
                else
                {
                    var baseMsg = "密码错误";
                    var adminHint = Username != null && Username.Equals("admin", StringComparison.OrdinalIgnoreCase)
                        ? "，管理员默认密码：admin123"
                        : "。管理员账号 admin 默认密码：admin123";
                    ShowStatus(baseMsg + adminHint, Brushes.Red);
                    PasswordBox.Clear();
                    PasswordBox.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"登录过程中发生错误：{ex.Message}", Brushes.Red);
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Content = "登录";
            }
        }

        private void SaveLoginCredentials()
        {
            try
            {
                // TODO: 保存登录凭据到配置文件或安全的存储位置
                System.Diagnostics.Debug.WriteLine("记住密码功能暂时禁用");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存登录凭据时发生错误：{ex.Message}");
            }
        }

        private void LoadSavedCredentials()
        {
            try
            {
                // TODO: 从配置文件加载保存的登录凭据
                System.Diagnostics.Debug.WriteLine("记住密码功能暂时禁用");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载保存的登录凭据时发生错误：{ex.Message}");
            }
        }

        #endregion

        #region 辅助方法

        private bool ValidateForm()
        {
            var isValid = !string.IsNullOrWhiteSpace(Username) && 
                         !string.IsNullOrWhiteSpace(PasswordBox.Password);

            IsFormValid = isValid;
            return isValid;
        }

        private void ShowStatus(string message, Brush color)
        {
            StatusMessage = message;
            StatusColor = color;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 