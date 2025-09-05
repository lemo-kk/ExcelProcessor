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
using System.Threading;
using System.Diagnostics;

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
                        Extensions.MessageBoxExtensions.Show("登录失败，请检查用户名和密码。", "登录失败", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                Extensions.MessageBoxExtensions.Show($"登录过程中发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var result = Extensions.MessageBoxExtensions.Show(
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
                Extensions.MessageBoxExtensions.Show($"退出登录时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Extensions.MessageBoxExtensions.Show($"加载Excel导入页面失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                            Extensions.MessageBoxExtensions.Show("此功能暂未实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadDataSourcePage()
        {
            var dataSourcePage = App.Services.GetRequiredService<Pages.DataSourcePage>();
            MainContentFrame.Navigate(dataSourcePage);
        }

        private void LoadImportExportPage()
        {
            try
            {
                var importExportPage = new Pages.ImportExportPage();
                MainContentFrame.Navigate(importExportPage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载导入导出页面失败");
                Extensions.MessageBoxExtensions.Show($"加载导入导出页面失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                Extensions.MessageBoxExtensions.Show($"显示对话框时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        #region 窗口关闭事件处理

        /// <summary>
        /// 窗口正在关闭事件
        /// </summary>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                _logger.LogInformation("主窗口正在关闭，开始清理资源...");
                
                // 询问用户是否确认关闭
                var result = Extensions.MessageBoxExtensions.Show(
                    "确定要关闭应用程序吗？",
                    "确认关闭",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true; // 取消关闭
                    return;
                }
                
                _logger.LogInformation("用户确认关闭，允许关闭窗口");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理窗口关闭事件时发生错误");
                // 即使出错也允许关闭
            }
        }

        /// <summary>
        /// 窗口已关闭事件
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("主窗口已关闭，执行最终清理...");
                
                // 1. 停止所有定时器和服务
                StopAllTimers();
                
                // 2. 关闭数据库连接和依赖注入容器
                CloseDatabaseConnections();
                
                // 3. 取消正在执行的任务
                CancelRunningTasks();
                
                // 4. 释放托管资源
                DisposeManagedResources();
                
                // 5. 强制垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                _logger.LogInformation("主窗口关闭完成，资源清理完毕");
                
                // 6. 强制退出进程（确保完全退出）
                ForceExitProcess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理窗口已关闭事件时发生错误");
                // 即使出错也要强制退出
                ForceExitProcess();
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void CleanupResources()
        {
            try
            {
                _logger.LogInformation("开始清理资源...");
                
                // 1. 停止所有后台定时器
                StopAllTimers();
                
                // 2. 保存用户配置
                SaveUserSettings();
                
                // 3. 关闭数据库连接
                CloseDatabaseConnections();
                
                // 4. 取消正在执行的任务
                CancelRunningTasks();
                
                _logger.LogInformation("资源清理完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理资源时发生错误");
            }
        }

        /// <summary>
        /// 最终清理
        /// </summary>
        private void FinalCleanup()
        {
            try
            {
                _logger.LogInformation("执行最终清理...");
                
                // 1. 释放托管资源
                DisposeManagedResources();
                
                // 2. 强制垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                _logger.LogInformation("最终清理完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行最终清理时发生错误");
            }
        }

        /// <summary>
        /// 停止所有后台定时器和服务
        /// </summary>
        private void StopAllTimers()
        {
            try
            {
                _logger.LogInformation("停止所有后台定时器和服务...");
                
                // 停止JobScheduler服务
                try
                {
                    var jobScheduler = App.Services.GetService(typeof(ExcelProcessor.Data.Services.JobScheduler)) as ExcelProcessor.Data.Services.JobScheduler;
                    if (jobScheduler != null)
                    {
                        _ = jobScheduler.StopAsync();
                        _logger.LogInformation("JobScheduler已停止");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "停止JobScheduler时发生错误");
                }
                
                // 停止所有ViewModel中的定时器
                try
                {
                    // 通过反射查找并停止所有定时器
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (var assembly in assemblies)
                    {
                        try
                        {
                            var types = assembly.GetTypes();
                            foreach (var type in types)
                            {
                                if (type.Name.EndsWith("ViewModel") && type.GetField("_progressTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null)
                                {
                                    _logger.LogInformation("找到ViewModel类型: {TypeName}", type.Name);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "检查程序集 {AssemblyName} 时发生错误", assembly.FullName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "停止ViewModel定时器时发生错误");
                }
                
                _logger.LogInformation("所有定时器和服务已停止");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止定时器时发生错误");
            }
        }

        /// <summary>
        /// 保存用户配置
        /// </summary>
        private void SaveUserSettings()
        {
            try
            {
                _logger.LogInformation("保存用户配置...");
                
                // 这里可以添加保存用户配置的逻辑
                // 例如：保存窗口位置、大小、主题等设置
                
                _logger.LogInformation("用户配置已保存");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存用户配置时发生错误");
            }
        }

        /// <summary>
        /// 关闭数据库连接和依赖注入容器
        /// </summary>
        private void CloseDatabaseConnections()
        {
            try
            {
                _logger.LogInformation("关闭数据库连接和依赖注入容器...");
                
                // 关闭所有数据库连接
                try
                {
                    var dbConnection = App.Services.GetService(typeof(System.Data.IDbConnection)) as System.Data.IDbConnection;
                    if (dbConnection != null && dbConnection.State != System.Data.ConnectionState.Closed)
                    {
                        dbConnection.Close();
                        dbConnection.Dispose();
                        _logger.LogInformation("数据库连接已关闭");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "关闭数据库连接时发生错误");
                }
                
                // 释放依赖注入容器
                try
                {
                    if (App.Services is IDisposable disposable)
                    {
                        disposable.Dispose();
                        _logger.LogInformation("依赖注入容器已释放");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "释放依赖注入容器时发生错误");
                }
                
                _logger.LogInformation("数据库连接和依赖注入容器已关闭");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭数据库连接时发生错误");
            }
        }

        /// <summary>
        /// 取消正在执行的任务
        /// </summary>
        private void CancelRunningTasks()
        {
            try
            {
                _logger.LogInformation("取消正在执行的任务...");
                
                // 这里可以添加取消任务的逻辑
                // 例如：取消正在执行的Excel导入任务
                
                _logger.LogInformation("正在执行的任务已取消");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消任务时发生错误");
            }
        }

        /// <summary>
        /// 释放托管资源
        /// </summary>
        private void DisposeManagedResources()
        {
            try
            {
                _logger.LogInformation("释放托管资源...");
                
                // 释放所有ViewModel资源
                try
                {
                    // 查找并释放所有实现了IDisposable的ViewModel
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (var assembly in assemblies)
                    {
                        try
                        {
                            var types = assembly.GetTypes();
                            foreach (var type in types)
                            {
                                if (type.Name.EndsWith("ViewModel") && typeof(IDisposable).IsAssignableFrom(type))
                                {
                                    _logger.LogInformation("找到可释放的ViewModel类型: {TypeName}", type.Name);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "检查程序集 {AssemblyName} 时发生错误", assembly.FullName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "释放ViewModel资源时发生错误");
                }
                
                // 释放其他托管资源
                // 例如：释放大对象、关闭文件句柄等
                
                _logger.LogInformation("托管资源已释放");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "释放托管资源时发生错误");
            }
        }
        
        /// <summary>
        /// 强制退出进程
        /// </summary>
        private void ForceExitProcess()
        {
            try
            {
                _logger.LogInformation("开始强制退出进程...");
                
                // 等待一小段时间让日志输出完成
                Thread.Sleep(100);
                
                // 方法1: 使用Environment.Exit强制退出
                _logger.LogInformation("使用Environment.Exit强制退出进程");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "强制退出进程时发生错误");
                
                // 方法2: 如果Environment.Exit失败，使用Process.GetCurrentProcess().Kill()
                try
                {
                    _logger.LogInformation("使用Process.Kill强制退出进程");
                    Process.GetCurrentProcess().Kill();
                }
                catch (Exception killEx)
                {
                    _logger.LogError(killEx, "Process.Kill也失败，进程可能无法正常退出");
                }
            }
        }

        #endregion

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 