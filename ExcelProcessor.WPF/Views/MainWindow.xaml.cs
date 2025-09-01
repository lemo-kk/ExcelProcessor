using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ExcelProcessor.WPF.Controls;
using ExcelProcessor.Models;
using ExcelProcessor.WPF.Windows;
using ExcelProcessor.Core.Services;

namespace ExcelProcessor.WPF.Views
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Button _currentActiveButton;
        private readonly ILogger<MainWindow> _logger;
        private bool _isCompactMode = false;
        private User _currentUser;
        private bool _isLoginRequired = true;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            _currentActiveButton = HomeButton;
            
            // 初始化日志（这里简化处理，实际项目中应该通过依赖注入）
            var loggerFactory = LoggerFactory.Create(builder => 
                builder.AddConsole().AddDebug());
            _logger = loggerFactory.CreateLogger<MainWindow>();
            
            // 初始化弹出窗管理器
            DialogManager.Initialize(DialogContainer);
            
            // 检测屏幕分辨率并应用自适应设置
            ApplyScreenAdaptation();
            
            // 初始化用户状态
            InitializeUserState();
            
            LoadHomePage();
        }

        #region 用户状态属性

        public string CurrentUserDisplayName
        {
            get => _currentUser?.DisplayName ?? "未登录";
        }

        public string CurrentUserRole
        {
            get => _currentUser?.Role.ToString() ?? "未知";
        }

        public bool IsLoginRequired
        {
            get => _isLoginRequired;
            set
            {
                _isLoginRequired = value;
                OnPropertyChanged(nameof(IsLoginRequired));
            }
        }

        #endregion

        #region 用户状态管理

        private void InitializeUserState()
        {
            // 检查是否需要登录
            IsLoginRequired = false; // 当前阶段不需要登录

            if (IsLoginRequired)
            {
                // 显示登录对话框
                ShowLoginDialog();
            }
            else
            {
                // 使用默认用户
                SetCurrentUser(new User
                {
                    Id = 1,
                    Username = "admin",
                    DisplayName = "系统管理员",
                    Role = UserRole.SuperAdmin,
                    Status = UserStatus.Active
                });
            }
        }

        private void ShowLoginDialog()
        {
            try
            {
                var loginWindow = new LoginWindow();
                
                // 只有在MainWindow已经显示的情况下才设置Owner
                if (this.IsLoaded && this.IsVisible)
                {
                    loginWindow.Owner = this;
                    loginWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    loginWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                if (loginWindow.ShowDialog() == true)
                {
                    var user = loginWindow.LoggedInUser;
                    if (user != null)
                    {
                        SetCurrentUser(user);
                        _logger.LogInformation($"用户 {user.Username} 登录成功");
                    }
                    else
                    {
                        MessageBox.Show("登录失败，请检查用户名和密码。", "登录失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                        ShowLoginDialog(); // 重新显示登录对话框
                    }
                }
                else
                {
                    // 用户取消登录，退出应用程序
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示登录对话框时发生错误");
                MessageBox.Show($"登录过程中发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void SetCurrentUser(User user)
        {
            _currentUser = user;
            OnPropertyChanged(nameof(CurrentUserDisplayName));
            OnPropertyChanged(nameof(CurrentUserRole));
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "确定要退出登录吗？",
                    "确认退出",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _logger.LogInformation($"用户 {_currentUser?.Username} 退出登录");
                    _currentUser = null;
                    OnPropertyChanged(nameof(CurrentUserDisplayName));
                    OnPropertyChanged(nameof(CurrentUserRole));

                    if (IsLoginRequired)
                    {
                        ShowLoginDialog();
                    }
                    else
                    {
                        // 如果不要求登录，使用默认用户
                        SetCurrentUser(new User
                        {
                            Id = 1,
                            Username = "guest",
                            DisplayName = "访客用户",
                            Role = UserRole.ReadOnly,
                            Status = UserStatus.Active
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "退出登录时发生错误");
                MessageBox.Show($"退出登录时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // 更新按钮样式
                UpdateNavigationButtonStyle(button);

                // 根据Tag加载对应页面
                string pageTag = button.Tag?.ToString();
                LoadPage(pageTag);
            }
        }

        private void UpdateNavigationButtonStyle(Button activeButton)
        {
            // 重置所有按钮样式
            ResetAllNavigationButtons();

            // 设置当前按钮为激活状态
            activeButton.Style = FindResource("ActiveNavigationButtonStyle") as Style;
            _currentActiveButton = activeButton;
        }

        private void ResetAllNavigationButtons()
        {
            var buttons = new[] { HomeButton, ExcelImportButton, SqlManagementButton, JobManagementButton, DataSourceButton, ImportExportButton, SystemSettingsButton };
            
            foreach (var button in buttons)
            {
                button.Style = FindResource("NavigationButtonStyle") as Style;
            }
        }

        private void LoadPage(string pageTag)
        {
            try
            {
                switch (pageTag)
                {
                    case "Home":
                        LoadHomePage();
                        break;
                    case "ExcelImport":
                        LoadExcelImportPage();
                        break;

                    case "SqlManagement":
                        LoadSqlManagementPage();
                        break;
                    case "JobManagement":
                        LoadJobManagementPage();
                        break;
                    case "DataSource":
                        LoadDataSourcePage();
                        break;
                    case "ImportExport":
                        LoadImportExportPage();
                        break;
                    case "SystemSettings":
                        LoadSystemSettingsPage();
                        break;
                    default:
                        LoadHomePage();
                        break;
                }

                // 记录日志到后台
                _logger.LogInformation("已切换到 {PageTag} 页面", pageTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "页面加载失败: {ErrorMessage}", ex.Message);
            }
        }

        private void LoadHomePage()
        {
            var homePage = new Pages.HomePage();
            MainContentFrame.Navigate(homePage);
        }

        private void LoadExcelImportPage()
        {
            try
            {
                var excelConfigService = App.Services.GetRequiredService<IExcelConfigService>();
                var excelImportPage = new Pages.ExcelImportPage(excelConfigService);
                MainContentFrame.Navigate(excelImportPage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载Excel导入页面失败");
                MessageBox.Show($"加载Excel导入页面失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void LoadSqlManagementPage()
        {
            var sqlManagementPage = App.Services.GetRequiredService<Controls.SqlManagementPage>();
            MainContentFrame.Navigate(sqlManagementPage);
        }

        private void LoadJobManagementPage()
        {
            var jobManagementPage = new Pages.JobManagementPage();
            MainContentFrame.Navigate(jobManagementPage);
        }

        private void LoadJobConfigPage()
        {
            MessageBox.Show("此功能暂未实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadDataSourcePage()
        {
            var dataSourcePage = App.Services.GetRequiredService<Pages.DataSourcePage>();
            MainContentFrame.Navigate(dataSourcePage);
        }

        private void LoadImportExportPage()
        {
            MessageBox.Show("此功能暂未实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadSystemSettingsPage()
        {
            var systemSettingsPage = new Pages.SystemSettingsPage();
            MainContentFrame.Navigate(systemSettingsPage);
        }

        /// <summary>
        /// 记录日志信息到后台日志系统
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="isError">是否为错误日志</param>
        public void LogMessage(string message, bool isError = false)
        {
            if (isError)
            {
                _logger.LogError("{Message}", message);
            }
            else
            {
                _logger.LogInformation("{Message}", message);
            }
        }

        /// <summary>
        /// 显示弹出窗
        /// </summary>
        /// <param name="dialog">弹出窗控件</param>
        /// <param name="onSave">保存回调</param>
        /// <param name="onTest">测试回调</param>
        /// <param name="onCancel">取消回调</param>
        public void ShowDialog(ConfigDetailDialog dialog, Action onSave = null, Action onTest = null, Action onCancel = null)
        {
            try
            {
                Console.WriteLine("=== MainWindow.ShowDialog 被调用 ===");
                
                if (dialog == null)
                {
                    Console.WriteLine("对话框对象为空");
                    return;
                }
                
                if (DialogOverlay == null)
                {
                    Console.WriteLine("DialogOverlay为空");
                    return;
                }
                
            DialogOverlay.Visibility = Visibility.Visible;
            DialogManager.ShowDialog(dialog, onSave, onTest, onCancel);
                
                Console.WriteLine("对话框显示成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MainWindow.ShowDialog 出错: {ex.Message}");
                MessageBox.Show($"显示对话框时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 关闭弹出窗
        /// </summary>
        public void CloseDialog()
        {
            DialogManager.CloseDialog();
            DialogOverlay.Visibility = Visibility.Collapsed;
        }

        private void ApplyScreenAdaptation()
        {
            try
            {
                // 获取主屏幕信息
                var screenHeight = SystemParameters.PrimaryScreenHeight;
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                
                // 检测是否为笔记本屏幕（高度小于1080或宽度小于1920）
                _isCompactMode = screenHeight < 1080 || screenWidth < 1920;
                
                if (_isCompactMode)
                {
                    ApplyCompactMode();
                    _logger.LogInformation("检测到笔记本屏幕，已启用紧凑模式。屏幕分辨率: {Width}x{Height}", screenWidth, screenHeight);
                }
                else
                {
                    ApplyNormalMode();
                    _logger.LogInformation("检测到桌面屏幕，使用标准模式。屏幕分辨率: {Width}x{Height}", screenWidth, screenHeight);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用屏幕自适应设置时出错");
            }
        }

        private void ApplyCompactMode()
        {
            // 设置紧凑模式的窗口属性
            this.MinHeight = 500;
            this.MinWidth = 800;
            
            // 调整字体大小
            this.FontSize = 12;
            
            // 调整导航栏高度
            if (MainGrid.RowDefinitions.Count > 0)
            {
                MainGrid.RowDefinitions[0].Height = new GridLength(65);
            }
            
            // 调整内容区域的内边距
            if (MainContentFrame != null)
            {
                MainContentFrame.Margin = new Thickness(8);
            }
            
            // 调整导航按钮样式为紧凑模式
            ApplyCompactNavigationStyles();
        }

        private void ApplyNormalMode()
        {
            // 设置标准模式的窗口属性
            this.MinHeight = 600;
            this.MinWidth = 900;
            
            // 恢复标准字体大小
            this.FontSize = 14;
            
            // 恢复标准导航栏高度
            if (MainGrid.RowDefinitions.Count > 0)
            {
                MainGrid.RowDefinitions[0].Height = new GridLength(80);
            }
            
            // 恢复标准内容区域内边距
            if (MainContentFrame != null)
            {
                MainContentFrame.Margin = new Thickness(12);
            }
            
            // 恢复标准导航按钮样式
            ApplyNormalNavigationStyles();
        }

        private void ApplyCompactNavigationStyles()
        {
            // 动态调整导航按钮样式
            var buttons = new[] { HomeButton, ExcelImportButton, SqlManagementButton, 
                                JobManagementButton, DataSourceButton, ImportExportButton, 
                                SystemSettingsButton };
            
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.FontSize = 13;
                    button.Padding = new Thickness(15, 12, 15, 12);
                    button.MinWidth = 50;
                    button.MinHeight = 45;
                }
            }
        }
        
        private void ApplyNormalNavigationStyles()
        {
            // 恢复标准导航按钮样式
            var buttons = new[] { HomeButton, ExcelImportButton, SqlManagementButton, 
                                JobManagementButton, DataSourceButton, ImportExportButton, 
                                SystemSettingsButton };
            
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.FontSize = 15;
                    button.Padding = new Thickness(20, 15, 20, 15);
                    button.MinWidth = 100;
                    button.MinHeight = 50;
                }
            }
        }

        public bool IsCompactMode => _isCompactMode;



        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 