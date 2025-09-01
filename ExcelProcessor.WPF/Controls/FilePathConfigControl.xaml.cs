using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ExcelProcessor.WPF.Controls
{
    /// <summary>
    /// 文件路径配置控件
    /// </summary>
    public partial class FilePathConfigControl : UserControl, INotifyPropertyChanged
    {
        // 文件路径属性 - 只使用相对路径
        private string _inputPath = "./data/input";
        private string _outputPath = "./data/output";
        private string _templatePath = "./data/templates";

        public event PropertyChangedEventHandler PropertyChanged;

        public FilePathConfigControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        #region 属性

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

        public string TemplatePath
        {
            get => _templatePath;
            set
            {
                _templatePath = value;
                OnPropertyChanged(nameof(TemplatePath));
            }
        }

        #endregion

        #region 事件处理

        private void BrowseInputPathButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择输入文件目录",
                ShowNewFolderButton = true
            };

            // 设置初始目录
            if (!string.IsNullOrEmpty(InputPath))
            {
                var absolutePath = ToAbsolutePath(InputPath);
                if (Directory.Exists(absolutePath))
                {
                    folderDialog.InitialDirectory = absolutePath;
                }
                else
                {
                    // 如果相对路径对应的文件夹不存在，使用默认路径
                    var defaultPath = Path.Combine(GetApplicationRootPath(), "data", "input");
                    if (Directory.Exists(defaultPath))
                    {
                        folderDialog.InitialDirectory = defaultPath;
                    }
                }
            }
            else
            {
                // 如果InputPath为空，使用默认路径
                var defaultPath = Path.Combine(GetApplicationRootPath(), "data", "input");
                if (Directory.Exists(defaultPath))
                {
                    folderDialog.InitialDirectory = defaultPath;
                }
            }

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                InputPath = ToRelativePath(folderDialog.SelectedPath);
            }
        }

        private void BrowseOutputPathButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择输出文件目录",
                ShowNewFolderButton = true
            };

            // 设置初始目录
            if (!string.IsNullOrEmpty(OutputPath))
            {
                var absolutePath = ToAbsolutePath(OutputPath);
                if (Directory.Exists(absolutePath))
                {
                    folderDialog.InitialDirectory = absolutePath;
                }
                else
                {
                    // 如果相对路径对应的文件夹不存在，使用默认路径
                    var defaultPath = Path.Combine(GetApplicationRootPath(), "data", "output");
                    if (Directory.Exists(defaultPath))
                    {
                        folderDialog.InitialDirectory = defaultPath;
                    }
                }
            }
            else
            {
                // 如果OutputPath为空，使用默认路径
                var defaultPath = Path.Combine(GetApplicationRootPath(), "data", "output");
                if (Directory.Exists(defaultPath))
                {
                    folderDialog.InitialDirectory = defaultPath;
                }
            }

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputPath = ToRelativePath(folderDialog.SelectedPath);
            }
        }

        private void BrowseTemplatePathButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择模板文件目录",
                ShowNewFolderButton = true
            };

            // 设置初始目录
            if (!string.IsNullOrEmpty(TemplatePath))
            {
                var absolutePath = ToAbsolutePath(TemplatePath);
                if (Directory.Exists(absolutePath))
                {
                    folderDialog.InitialDirectory = absolutePath;
                }
                else
                {
                    // 如果相对路径对应的文件夹不存在，使用默认路径
                    var defaultPath = Path.Combine(GetApplicationRootPath(), "data", "templates");
                    if (Directory.Exists(defaultPath))
                    {
                        folderDialog.InitialDirectory = defaultPath;
                    }
                }
            }
            else
            {
                // 如果TemplatePath为空，使用默认路径
                var defaultPath = Path.Combine(GetApplicationRootPath(), "data", "templates");
                if (Directory.Exists(defaultPath))
                {
                    folderDialog.InitialDirectory = defaultPath;
                }
            }

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TemplatePath = ToRelativePath(folderDialog.SelectedPath);
            }
        }

        private void CreateDefaultFoldersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var appRoot = GetApplicationRootPath();
                var folders = new[]
                {
                    Path.Combine(appRoot, "data", "input"),
                    Path.Combine(appRoot, "data", "output"),
                    Path.Combine(appRoot, "data", "templates"),
                    Path.Combine(appRoot, "data", "temp"),
                    Path.Combine(appRoot, "logs"),
                    Path.Combine(appRoot, "config")
                };

                foreach (var folder in folders)
                {
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                }

                MessageBox.Show("默认目录结构创建成功！", "创建成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建目录失败：{ex.Message}", "创建失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenInputFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var absolutePath = ToAbsolutePath(InputPath);
                if (Directory.Exists(absolutePath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", absolutePath);
                }
                else
                {
                    MessageBox.Show("输入目录不存在，请先创建目录！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开目录失败：{ex.Message}", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenOutputFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var absolutePath = ToAbsolutePath(OutputPath);
                if (Directory.Exists(absolutePath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", absolutePath);
                }
                else
                {
                    MessageBox.Show("输出目录不存在，请先创建目录！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开目录失败：{ex.Message}", "操作失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 辅助方法

        private string GetApplicationRootPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private string ToRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return string.Empty;

            try
            {
                var rootPath = GetApplicationRootPath();
                if (absolutePath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                {
                    var relativePath = absolutePath.Substring(rootPath.Length);
                    return relativePath.TrimStart('\\', '/');
                }
                return absolutePath;
            }
            catch
            {
                return absolutePath;
            }
        }

        private string ToAbsolutePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return string.Empty;

            try
            {
                if (Path.IsPathRooted(relativePath))
                    return relativePath;

                return Path.Combine(GetApplicationRootPath(), relativePath);
            }
            catch
            {
                return relativePath;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 