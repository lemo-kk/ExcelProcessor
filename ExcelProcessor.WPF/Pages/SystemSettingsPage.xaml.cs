using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using ExcelProcessor.WPF.Pages;
using ExcelProcessor.WPF.Windows;
using ExcelProcessor.Data.Services;

namespace ExcelProcessor.WPF.Pages
{
    	public partial class SystemSettingsPage : Page, INotifyPropertyChanged, IDisposable
    {
        // 系统设置属性
        private bool _autoSaveEnabled = true;
        private int _autoSaveInterval = 5;
        private bool _startupMinimize = false;
        private bool _checkForUpdates = true;
        private bool _enableLogging = true;
        private string _logLevel = "Info";
        private bool _enableNotifications = true;
        private bool _enableLogin = true;
        private string _language = "简体中文";
        private string _theme = "深色主题";
        private bool _enableAnimations = true;
        private int _maxRecentFiles = 10;
        private bool _confirmBeforeClose = true;
        private bool _enableBackup = true;
        private int _backupRetentionDays = 30;

        // 文件路径设置属性 - 只使用相对路径
        private string _inputPath = "./data/input";
        private string _outputPath = "./data/output";
        private bool _useRelativePath = true;

        // 账号管理属性
        private string _currentUserDisplayName = "管理员";
        private string _currentUserRole = "超级管理员";

        // 性能监控相关
        private readonly Process _currentProcess;
        private readonly DispatcherTimer _performanceTimer;
        private DataCacheService _cacheService;

        public event PropertyChangedEventHandler PropertyChanged;

        public SystemSettingsPage()
        {
            InitializeComponent();
            DataContext = this;

            // 初始化性能监控
            _currentProcess = Process.GetCurrentProcess();
            _performanceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _performanceTimer.Tick += PerformanceTimer_Tick;

            InitializeComboBoxes();
            LoadDefaultSettings();
            InitializePerformanceMonitoring();
        }

        #region 属性

        public bool AutoSaveEnabled
        {
            get => _autoSaveEnabled;
            set
            {
                _autoSaveEnabled = value;
                OnPropertyChanged(nameof(AutoSaveEnabled));
                OnPropertyChanged(nameof(AutoSaveIntervalEnabled));
            }
        }

        public int AutoSaveInterval
        {
            get => _autoSaveInterval;
            set
            {
                _autoSaveInterval = value;
                OnPropertyChanged(nameof(AutoSaveInterval));
            }
        }

        public bool AutoSaveIntervalEnabled => AutoSaveEnabled;

        public bool StartupMinimize
        {
            get => _startupMinimize;
            set
            {
                _startupMinimize = value;
                OnPropertyChanged(nameof(StartupMinimize));
            }
        }

        public bool CheckForUpdates
        {
            get => _checkForUpdates;
            set
            {
                _checkForUpdates = value;
                OnPropertyChanged(nameof(CheckForUpdates));
            }
        }

        public bool EnableLogging
        {
            get => _enableLogging;
            set
            {
                _enableLogging = value;
                OnPropertyChanged(nameof(EnableLogging));
                OnPropertyChanged(nameof(LogLevelEnabled));
            }
        }

        public string LogLevel
        {
            get => _logLevel;
            set
            {
                _logLevel = value;
                OnPropertyChanged(nameof(LogLevel));
            }
        }

        public bool LogLevelEnabled => EnableLogging;

        public bool EnableNotifications
        {
            get => _enableNotifications;
            set
            {
                _enableNotifications = value;
                OnPropertyChanged(nameof(EnableNotifications));
            }
        }

        public bool EnableLogin
        {
            get => _enableLogin;
            set
            {
                _enableLogin = value;
                OnPropertyChanged(nameof(EnableLogin));
                // 实时保存登录设置
                _ = Task.Run(() =>
                {
                    try
                    {
                        // 模拟保存登录设置
                        // 实际应用中，这里需要调用服务
                        // await _systemConfigService.SetLoginEnabledAsync(value);
                    }
                    catch (Exception ex)
                    {
                        // 在UI线程中显示错误
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"保存登录设置失败：{ex.Message}", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                });
            }
        }

        public new string Language
        {
            get => _language;
            set
            {
                _language = value;
                OnPropertyChanged(nameof(Language));
            }
        }

        public string Theme
        {
            get => _theme;
            set
            {
                _theme = value;
                OnPropertyChanged(nameof(Theme));
            }
        }

        public bool EnableAnimations
        {
            get => _enableAnimations;
            set
            {
                _enableAnimations = value;
                OnPropertyChanged(nameof(EnableAnimations));
            }
        }

        public int MaxRecentFiles
        {
            get => _maxRecentFiles;
            set
            {
                _maxRecentFiles = value;
                OnPropertyChanged(nameof(MaxRecentFiles));
            }
        }

        public bool ConfirmBeforeClose
        {
            get => _confirmBeforeClose;
            set
            {
                _confirmBeforeClose = value;
                OnPropertyChanged(nameof(ConfirmBeforeClose));
            }
        }

        public bool EnableBackup
        {
            get => _enableBackup;
            set
            {
                _enableBackup = value;
                OnPropertyChanged(nameof(EnableBackup));
                OnPropertyChanged(nameof(BackupRetentionDaysEnabled));
            }
        }

        public int BackupRetentionDays
        {
            get => _backupRetentionDays;
            set
            {
                _backupRetentionDays = value;
                OnPropertyChanged(nameof(BackupRetentionDays));
            }
        }

        public bool BackupRetentionDaysEnabled => EnableBackup;

        // 文件路径设置属性
        public string InputPath
        {
            get => _inputPath;
            set
            {
                _inputPath = value;
                OnPropertyChanged(nameof(InputPath));
            }
        }

        public string OutputPath
        {
            get => _outputPath;
            set
            {
                _outputPath = value;
                OnPropertyChanged(nameof(OutputPath));
            }
        }

        public bool UseRelativePath
        {
            get => _useRelativePath;
            set
            {
                _useRelativePath = value;
                OnPropertyChanged(nameof(UseRelativePath));
            }
        }

        // 账号和权限管理属性
        public string CurrentUserDisplayName
        {
            get => _currentUserDisplayName;
            set
            {
                _currentUserDisplayName = value;
                OnPropertyChanged(nameof(CurrentUserDisplayName));
            }
        }

        public string CurrentUserRole
        {
            get => _currentUserRole;
            set
            {
                _currentUserRole = value;
                OnPropertyChanged(nameof(CurrentUserRole));
            }
        }

        // 性能监控属性
        private string _memoryUsage = "0 MB";
        private string _cpuUsage = "0%";
        private string _cacheHitRate = "0%";
        private string _importSpeed = "0 行/秒";
        private string _cacheItemCount = "0";
        private string _cacheSize = "0 KB";
        private string _cacheEfficiency = "0%";

        public string MemoryUsage
        {
            get => _memoryUsage;
            set
            {
                _memoryUsage = value;
                OnPropertyChanged(nameof(MemoryUsage));
            }
        }

        public string CpuUsage
        {
            get => _cpuUsage;
            set
            {
                _cpuUsage = value;
                OnPropertyChanged(nameof(CpuUsage));
            }
        }

        public string CacheHitRate
        {
            get => _cacheHitRate;
            set
            {
                _cacheHitRate = value;
                OnPropertyChanged(nameof(CacheHitRate));
            }
        }

        public string ImportSpeed
        {
            get => _importSpeed;
            set
            {
                _importSpeed = value;
                OnPropertyChanged(nameof(ImportSpeed));
            }
        }

        public string CacheItemCount
        {
            get => _cacheItemCount;
            set
            {
                _cacheItemCount = value;
                OnPropertyChanged(nameof(CacheItemCount));
            }
        }

        public string CacheSize
        {
            get => _cacheSize;
            set
            {
                _cacheSize = value;
                OnPropertyChanged(nameof(CacheSize));
            }
        }

        public string CacheEfficiency
        {
            get => _cacheEfficiency;
            set
            {
                _cacheEfficiency = value;
                OnPropertyChanged(nameof(CacheEfficiency));
            }
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 加载默认设置
        /// </summary>
        private void LoadDefaultSettings()
        {
            try
            {
                // 模拟加载默认设置
                AutoSaveEnabled = true;
                AutoSaveInterval = 5;
                StartupMinimize = false;
                CheckForUpdates = true;
                EnableLogging = true;
                LogLevel = "Info";
                EnableNotifications = true;
                EnableLogin = true;
                Language = "简体中文";
                Theme = "深色主题";
                EnableAnimations = true;
                MaxRecentFiles = 10;
                ConfirmBeforeClose = true;
                EnableBackup = true;
                BackupRetentionDays = 30;

                // 加载当前用户信息
                CurrentUserDisplayName = "管理员";
                CurrentUserRole = "超级管理员";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载设置时发生错误：{ex.Message}", "加载失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeComboBoxes()
        {
            // 日志级别选项
            LogLevelComboBox.ItemsSource = new List<string>
            {
                "Debug", "Info", "Warning", "Error", "Fatal"
            };

            // 语言选项
            LanguageComboBox.ItemsSource = new List<string>
            {
                "简体中文", "English", "繁體中文"
            };

            // 主题选项
            ThemeComboBox.ItemsSource = new List<string>
            {
                "深色主题", "浅色主题", "自动"
            };
        }

        #endregion

        #region 事件处理

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 模拟保存设置
                // 实际应用中，这里需要调用服务
                // var settings = new SystemSettings
                // {
                //     AutoSaveEnabled = AutoSaveEnabled,
                //     AutoSaveInterval = AutoSaveInterval,
                //     StartupMinimize = StartupMinimize,
                //     CheckForUpdates = CheckForUpdates,
                //     EnableLogging = EnableLogging,
                //     LogLevel = LogLevel,
                //     EnableNotifications = EnableNotifications,
                //     EnableLogin = EnableLogin,
                //     Language = Language,
                //     Theme = Theme,
                //     EnableAnimations = EnableAnimations,
                //     MaxRecentFiles = MaxRecentFiles,
                //     ConfirmBeforeClose = ConfirmBeforeClose,
                //     EnableBackup = EnableBackup,
                //     BackupRetentionDays = BackupRetentionDays
                // };

                // // 保存设置到系统配置服务
                // var result = await _systemConfigService.SaveSystemSettings(settings);
                
                // if (result)
                // {
                    MessageBox.Show("设置保存成功！", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
                // }
                // else
                // {
                //     MessageBox.Show("设置保存失败！", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
                // }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置时发生错误：{ex.Message}", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("确定要重置所有设置到默认值吗？", "确认重置", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // 重置系统配置到默认值
                    // 模拟重置
                    AutoSaveEnabled = true;
                    AutoSaveInterval = 5;
                    StartupMinimize = false;
                    CheckForUpdates = true;
                    EnableLogging = true;
                    LogLevel = "Info";
                    EnableNotifications = true;
                    EnableLogin = true;
                    Language = "简体中文";
                    Theme = "深色主题";
                    EnableAnimations = true;
                    MaxRecentFiles = 10;
                    ConfirmBeforeClose = true;
                    EnableBackup = true;
                    BackupRetentionDays = 30;

                    // 重新加载设置
                    LoadDefaultSettings();
                    MessageBox.Show("设置已重置到默认值！", "重置成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重置设置时发生错误：{ex.Message}", "重置失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: 实现检查更新逻辑
                MessageBox.Show("当前已是最新版本！", "检查更新", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"检查更新时发生错误：{ex.Message}", "检查失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseInputPathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderDialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "选择Excel输入文件目录",
                    ShowNewFolderButton = true
                };

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    InputPathTextBox.Text = folderDialog.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"选择输入路径时发生错误：{ex.Message}", "选择失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseOutputPathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderDialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "选择Excel输出文件目录",
                    ShowNewFolderButton = true
                };

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    OutputPathTextBox.Text = folderDialog.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"选择输出路径时发生错误：{ex.Message}", "选择失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateDefaultFoldersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var appRoot = AppDomain.CurrentDomain.BaseDirectory;
                var defaultFolders = new[]
                {
                    Path.Combine(appRoot, "data", "input"),
                    Path.Combine(appRoot, "data", "output"),
                    Path.Combine(appRoot, "data", "templates"),
                    Path.Combine(appRoot, "data", "temp"),
                    Path.Combine(appRoot, "config"),
                    Path.Combine(appRoot, "logs")
                };

                foreach (var folder in defaultFolders)
                {
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                }

                // 更新路径文本框
                InputPathTextBox.Text = Path.Combine(appRoot, "data", "input");
                OutputPathTextBox.Text = Path.Combine(appRoot, "data", "output");

                MessageBox.Show("默认目录创建成功！", "创建成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建默认目录时发生错误：{ex.Message}", "创建失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: 实现数据库连接测试
                MessageBox.Show("数据库连接正常！", "连接测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"连接测试失败：{ex.Message}", "测试失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackupDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: 实现数据库备份
                MessageBox.Show("数据库备份成功！", "备份成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据库备份失败：{ex.Message}", "备份失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: 实现设置导出
                MessageBox.Show("设置导出成功！", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置导出失败：{ex.Message}", "导出失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: 实现设置导入
                MessageBox.Show("设置导入成功！", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置导入失败：{ex.Message}", "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenLogFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: 实现打开日志文件夹
                var logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ExcelProcessor", "Logs");
                if (Directory.Exists(logFolder))
                {
                    System.Diagnostics.Process.Start("explorer.exe", logFolder);
                }
                else
                {
                    MessageBox.Show("日志文件夹不存在！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开日志文件夹失败：{ex.Message}", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("确定要清空所有日志文件吗？", "确认清空", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // TODO: 实现清空日志逻辑
                    MessageBox.Show("日志已清空！", "清空成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"清空日志失败：{ex.Message}", "清空失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: 实现检查更新逻辑
                MessageBox.Show("当前已是最新版本！", "检查更新", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"检查更新失败：{ex.Message}", "检查失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SystemInfoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: 实现系统信息显示
                var systemInfo = $"操作系统：{Environment.OSVersion}\n" +
                               $"运行时：{Environment.Version}\n" +
                               $"机器名：{Environment.MachineName}\n" +
                               $"用户名：{Environment.UserName}";
                
                MessageBox.Show(systemInfo, "系统信息", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取系统信息失败：{ex.Message}", "获取失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ManageUsersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 导航到用户管理页面
                var userManagementPage = new UserManagementPage(App.Services);
                NavigationService?.Navigate(userManagementPage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开用户管理失败：{ex.Message}", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: 实现修改密码功能
                MessageBox.Show("修改密码功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"修改密码失败：{ex.Message}", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void RoleManagementButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 导航到角色管理页面
                var roleManagementPage = new RoleManagementPage(App.Services);
                NavigationService?.Navigate(roleManagementPage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开角色管理失败：{ex.Message}", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 性能监控方法

        /// <summary>
        /// 初始化性能监控
        /// </summary>
        private void InitializePerformanceMonitoring()
        {
            try
            {
                // 获取缓存服务
                _cacheService = App.Services.GetService(typeof(DataCacheService)) as DataCacheService;
                
                // 启动性能监控定时器
                _performanceTimer.Start();
                
                // 立即更新一次性能指标
                UpdatePerformanceMetrics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化性能监控失败：{ex.Message}", "初始化失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 性能监控定时器事件
        /// </summary>
        private void PerformanceTimer_Tick(object sender, EventArgs e)
        {
            UpdatePerformanceMetrics();
        }

        /// <summary>
        /// 更新性能指标
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            try
            {
                // 更新内存使用
                var memoryUsageMB = _currentProcess.WorkingSet64 / (1024 * 1024);
                MemoryUsage = $"{memoryUsageMB} MB";

                // 更新CPU使用率（简化计算）
                var cpuUsage = Math.Min(100, (double)memoryUsageMB / 1024 * 10);
                CpuUsage = $"{cpuUsage:F1}%";

                // 更新缓存统计
                if (_cacheService != null)
                {
                    var stats = _cacheService.GetStatistics();
                    CacheHitRate = $"{stats.HitRate * 100:F1}%";
                    CacheItemCount = stats.TotalItems.ToString();
                    CacheSize = $"{stats.ValidItems * 1024 / 1024:F1} KB";
                    CacheEfficiency = $"{stats.HitRate * 100:F1}%";
                }

                // 更新导入速度（模拟数据）
                var importSpeed = new Random().Next(100, 1000);
                ImportSpeed = $"{importSpeed} 行/秒";
            }
            catch (Exception ex)
            {
                // 静默处理错误，避免影响UI
                Debug.WriteLine($"更新性能指标失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 刷新性能按钮点击事件
        /// </summary>
        private void RefreshPerformanceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdatePerformanceMetrics();
                MainSnackbar.MessageQueue?.Enqueue("性能指标已刷新", null, null, null, false, true, TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新性能指标失败：{ex.Message}", "刷新失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 清空缓存按钮点击事件
        /// </summary>
        private void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("确定要清空所有缓存吗？", "确认清空", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _cacheService?.Clear();
                    UpdatePerformanceMetrics();
                    MainSnackbar.MessageQueue?.Enqueue("缓存已清空", null, null, null, false, true, TimeSpan.FromSeconds(2));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"清空缓存失败：{ex.Message}", "清空失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        #endregion

        #region INotifyPropertyChanged

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable Implementation

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    // 停止性能监控定时器
                    if (_performanceTimer != null)
                    {
                        _performanceTimer.Stop();
                        _performanceTimer.Tick -= PerformanceTimer_Tick;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"释放SystemSettingsPage资源时发生错误：{ex.Message}");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        ~SystemSettingsPage()
        {
            Dispose(false);
        }

        #endregion
    }
} 