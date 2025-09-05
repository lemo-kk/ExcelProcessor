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
    public partial class RoleEditDialog : Window, INotifyPropertyChanged
    {
        private Role _role;
        private bool _isEditMode;
        private ObservableCollection<PermissionTreeNode> _permissionTree;

        public event PropertyChangedEventHandler PropertyChanged;

        public RoleEditDialog(Role role = null)
        {
            InitializeComponent();
            DataContext = this;

            _isEditMode = role != null;
            _role = role ?? new Role
            {
                Id = 0,
                Code = "",
                Name = "",
                Description = "",
                Type = RoleType.Custom,
                IsSystem = false,
                IsEnabled = true,
                SortOrder = 0,
                CreatedTime = DateTime.Now,
                UpdatedTime = DateTime.Now
            };

            InitializePermissionTree();
            LoadRoleData();
        }

        #region 属性

        public string DialogTitle => _isEditMode ? "编辑角色" : "添加角色";

        public Role Role
        {
            get => _role;
            set
            {
                _role = value;
                OnPropertyChanged(nameof(Role));
            }
        }

        public List<RoleType> RoleTypes => Enum.GetValues(typeof(RoleType)).Cast<RoleType>().ToList();

        public ObservableCollection<PermissionTreeNode> PermissionTree
        {
            get => _permissionTree;
            set
            {
                _permissionTree = value;
                OnPropertyChanged(nameof(PermissionTree));
            }
        }

        #endregion

        #region 初始化方法

        private void LoadRoleData()
        {
            // 数据绑定会自动更新UI
            PermissionsTreeView.ItemsSource = PermissionTree;
        }

        private void InitializePermissionTree()
        {
            PermissionTree = new ObservableCollection<PermissionTreeNode>();

            // 系统管理模块
            var systemManagement = new PermissionTreeNode
            {
                Name = "系统管理",
                Icon = "Settings",
                IsGranted = false,
                Children = new ObservableCollection<PermissionTreeNode>
                {
                    new PermissionTreeNode
                    {
                        Name = "用户管理",
                        Icon = "AccountGroup",
                        PermissionCode = "UserManagement",
                        IsGranted = false
                    },
                    new PermissionTreeNode
                    {
                        Name = "角色管理",
                        Icon = "AccountMultiple",
                        PermissionCode = "RoleManagement",
                        IsGranted = false
                    },
                    new PermissionTreeNode
                    {
                        Name = "权限管理",
                        Icon = "ShieldAccount",
                        PermissionCode = "PermissionManagement",
                        IsGranted = false
                    },
                    new PermissionTreeNode
                    {
                        Name = "系统设置",
                        Icon = "Cog",
                        PermissionCode = "SystemSettings",
                        IsGranted = false
                    }
                }
            };

            // 文件处理模块
            var fileProcessing = new PermissionTreeNode
            {
                Name = "文件处理",
                Icon = "FileExcel",
                IsGranted = false,
                Children = new ObservableCollection<PermissionTreeNode>
                {
                    new PermissionTreeNode
                    {
                        Name = "文件上传",
                        Icon = "Upload",
                        PermissionCode = "FileUpload",
                        IsGranted = false
                    },
                    new PermissionTreeNode
                    {
                        Name = "文件处理",
                        Icon = "FileCog",
                        PermissionCode = "FileProcess",
                        IsGranted = false
                    },
                    new PermissionTreeNode
                    {
                        Name = "处理历史",
                        Icon = "History",
                        PermissionCode = "ProcessHistory",
                        IsGranted = false
                    },
                    new PermissionTreeNode
                    {
                        Name = "批量处理",
                        Icon = "FileMultiple",
                        PermissionCode = "BatchProcess",
                        IsGranted = false
                    }
                }
            };

            // 数据管理模块
            var dataManagement = new PermissionTreeNode
            {
                Name = "数据管理",
                Icon = "Database",
                IsGranted = false,
                Children = new ObservableCollection<PermissionTreeNode>
                {
                    new PermissionTreeNode
                    {
                        Name = "数据查看",
                        Icon = "Eye",
                        PermissionCode = "DataView",
                        IsGranted = false
                    },
                    new PermissionTreeNode
                    {
                        Name = "数据导出",
                        Icon = "Download",
                        PermissionCode = "DataExport",
                        IsGranted = false
                    },
                    new PermissionTreeNode
                    {
                        Name = "数据分析",
                        Icon = "ChartLine",
                        PermissionCode = "DataAnalysis",
                        IsGranted = false
                    },
                    new PermissionTreeNode
                    {
                        Name = "数据清理",
                        Icon = "Broom",
                        PermissionCode = "DataClean",
                        IsGranted = false
                    }
                }
            };

            // 报表管理模块
            var reportManagement = new PermissionTreeNode
            {
                Name = "报表管理",
                Icon = "FileChart",
                IsGranted = false,
                Children = new ObservableCollection<PermissionTreeNode>
                {
                    new PermissionTreeNode
                    {
                        Name = "报表生成",
                        Icon = "FilePlus",
                        PermissionCode = "ReportGenerate",
                        IsGranted = false
                    },
                    new PermissionTreeNode
                    {
                        Name = "报表查看",
                        Icon = "FileSearch",
                        PermissionCode = "ReportView",
                        IsGranted = false
                    },
                    new PermissionTreeNode
                    {
                        Name = "报表导出",
                        Icon = "FileExport",
                        PermissionCode = "ReportExport",
                        IsGranted = false
                    },
                    new PermissionTreeNode
                    {
                        Name = "报表模板",
                        Icon = "FileTemplate",
                        PermissionCode = "ReportTemplate",
                        IsGranted = false
                    }
                }
            };

            // 审计日志模块
            var auditLog = new PermissionTreeNode
            {
                Name = "审计日志",
                Icon = "ClipboardList",
                IsGranted = false,
                Children = new ObservableCollection<PermissionTreeNode>
                {
                    new PermissionTreeNode
                    {
                        Name = "操作日志",
                        Icon = "ClipboardText",
                        PermissionCode = "OperationLog",
                        IsGranted = false
                    },
                    new PermissionTreeNode
                    {
                        Name = "登录日志",
                        Icon = "Login",
                        PermissionCode = "LoginLog",
                        IsGranted = false
                    },
                    new PermissionTreeNode
                    {
                        Name = "系统日志",
                        Icon = "Monitor",
                        PermissionCode = "SystemLog",
                        IsGranted = false
                    }
                }
            };

            PermissionTree.Add(systemManagement);
            PermissionTree.Add(fileProcessing);
            PermissionTree.Add(dataManagement);
            PermissionTree.Add(reportManagement);
            PermissionTree.Add(auditLog);

            // 如果是编辑模式，加载现有权限
            if (_isEditMode && _role.Permissions != null)
            {
                LoadExistingPermissions();
            }
        }

        private void LoadExistingPermissions()
        {
            // 这里应该根据角色的现有权限来设置复选框状态
            // 由于是模拟数据，我们根据角色类型设置一些默认权限
            foreach (var module in PermissionTree)
            {
                foreach (var permission in module.Children)
                {
                    // 根据角色类型设置默认权限
                    permission.IsGranted = _role.Type switch
                    {
                        RoleType.System => true, // 系统角色拥有所有权限
                        RoleType.Custom => permission.PermissionCode switch
                        {
                            "UserManagement" => _role.Code == "Admin",
                            "RoleManagement" => _role.Code == "Admin",
                            "PermissionManagement" => _role.Code == "Admin",
                            "SystemSettings" => _role.Code == "Admin",
                            "FileUpload" => _role.Code == "FileProcessor",
                            "FileProcess" => _role.Code == "FileProcessor",
                            "ProcessHistory" => _role.Code == "FileProcessor",
                            "BatchProcess" => _role.Code == "FileProcessor",
                            "DataView" => _role.Code == "DataManager" || _role.Code == "ReadOnly",
                            "DataExport" => _role.Code == "DataManager",
                            "DataAnalysis" => _role.Code == "DataManager",
                            "DataClean" => _role.Code == "DataManager",
                            "ReportGenerate" => _role.Code == "DataManager",
                            "ReportView" => _role.Code == "DataManager" || _role.Code == "ReadOnly",
                            "ReportExport" => _role.Code == "DataManager",
                            "ReportTemplate" => _role.Code == "DataManager",
                            "OperationLog" => _role.Code == "Auditor",
                            "LoginLog" => _role.Code == "Auditor",
                            "SystemLog" => _role.Code == "Auditor",
                            _ => false
                        },
                        _ => false
                    };
                }
            }
        }

        #endregion

        #region 事件处理

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            SetAllPermissions(true);
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            SetAllPermissions(false);
        }

        private void ExpandAllButton_Click(object sender, RoutedEventArgs e)
        {
            ExpandAllTreeViewItems(PermissionsTreeView);
        }

        private void CollapseAllButton_Click(object sender, RoutedEventArgs e)
        {
            CollapseAllTreeViewItems(PermissionsTreeView);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ValidateInput())
                {
                    // 保存权限设置
                    SavePermissions();
                    
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void SetAllPermissions(bool isGranted)
        {
            foreach (var module in PermissionTree)
            {
                module.IsGranted = isGranted;
                foreach (var permission in module.Children)
                {
                    permission.IsGranted = isGranted;
                }
            }
        }

        private void ExpandAllTreeViewItems(TreeView treeView)
        {
            foreach (var item in treeView.Items)
            {
                var treeViewItem = treeView.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (treeViewItem != null)
                {
                    treeViewItem.IsExpanded = true;
                }
            }
        }

        private void CollapseAllTreeViewItems(TreeView treeView)
        {
            foreach (var item in treeView.Items)
            {
                var treeViewItem = treeView.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (treeViewItem != null)
                {
                    treeViewItem.IsExpanded = false;
                }
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(_role.Code))
            {
                Extensions.MessageBoxExtensions.Show("请输入角色代码", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                RoleCodeTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_role.Name))
            {
                Extensions.MessageBoxExtensions.Show("请输入角色名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                RoleNameTextBox.Focus();
                return false;
            }

            return true;
        }

        private void SavePermissions()
        {
            // 收集选中的权限
            var selectedPermissions = new List<RolePermission>();

            foreach (var module in PermissionTree)
            {
                foreach (var permission in module.Children)
                {
                    if (permission.IsGranted && !string.IsNullOrEmpty(permission.PermissionCode))
                    {
                        selectedPermissions.Add(new RolePermission
                        {
                            RoleId = _role.Id,
                            PermissionId = GetPermissionId(permission.PermissionCode),
                            IsGranted = true,
                            GrantedTime = DateTime.Now
                        });
                    }
                }
            }

            _role.Permissions = selectedPermissions;
        }

        private int GetPermissionId(string permissionCode)
        {
            // 这里应该从数据库或权限服务中获取权限ID
            // 现在使用模拟的ID映射
            return permissionCode switch
            {
                "UserManagement" => 1,
                "RoleManagement" => 2,
                "PermissionManagement" => 3,
                "SystemSettings" => 4,
                "FileUpload" => 5,
                "FileProcess" => 6,
                "ProcessHistory" => 7,
                "BatchProcess" => 8,
                "DataView" => 9,
                "DataExport" => 10,
                "DataAnalysis" => 11,
                "DataClean" => 12,
                "ReportGenerate" => 13,
                "ReportView" => 14,
                "ReportExport" => 15,
                "ReportTemplate" => 16,
                "OperationLog" => 17,
                "LoginLog" => 18,
                "SystemLog" => 19,
                _ => 0
            };
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// 权限树节点类
    /// </summary>
    public class PermissionTreeNode : INotifyPropertyChanged
    {
        private string _name;
        private string _icon;
        private string _permissionCode;
        private bool _isGranted;
        private ObservableCollection<PermissionTreeNode> _children;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged(nameof(Icon));
            }
        }

        public string PermissionCode
        {
            get => _permissionCode;
            set
            {
                _permissionCode = value;
                OnPropertyChanged(nameof(PermissionCode));
            }
        }

        public bool IsGranted
        {
            get => _isGranted;
            set
            {
                _isGranted = value;
                OnPropertyChanged(nameof(IsGranted));
            }
        }

        public ObservableCollection<PermissionTreeNode> Children
        {
            get => _children;
            set
            {
                _children = value;
                OnPropertyChanged(nameof(Children));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 