using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExcelProcessor.WPF.Models
{
    /// <summary>
    /// 导入预览信息
    /// </summary>
    public class ImportPreviewInfo : INotifyPropertyChanged
    {
        private string _packageName;
        private string _packageVersion;
        private DateTime _packageCreatedAt;
        private string _packageDescription;
        private long _packageSize;
        private string _sourceJobName;
        private string _sourceJobDescription;
        private int _excelConfigCount;
        private int _sqlScriptCount;
        private int _dataSourceCount;
        private int _fieldMappingCount;
        private List<string> _conflicts;
        private List<string> _dependencies;
        private List<string> _warnings;
        private List<string> _recommendations;
        private TimeSpan _estimatedImportTime;
        private bool _hasConflicts;
        private bool _canImport;

        public ImportPreviewInfo()
        {
            Conflicts = new List<string>();
            Dependencies = new List<string>();
            Warnings = new List<string>();
            Recommendations = new List<string>();
        }

        /// <summary>
        /// 包名称
        /// </summary>
        public string PackageName
        {
            get => _packageName;
            set
            {
                _packageName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 包版本
        /// </summary>
        public string PackageVersion
        {
            get => _packageVersion;
            set
            {
                _packageVersion = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 包创建时间
        /// </summary>
        public DateTime PackageCreatedAt
        {
            get => _packageCreatedAt;
            set
            {
                _packageCreatedAt = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 包描述
        /// </summary>
        public string PackageDescription
        {
            get => _packageDescription;
            set
            {
                _packageDescription = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 包大小（字节）
        /// </summary>
        public long PackageSize
        {
            get => _packageSize;
            set
            {
                _packageSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 源作业名称
        /// </summary>
        public string SourceJobName
        {
            get => _sourceJobName;
            set
            {
                _sourceJobName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 源作业描述
        /// </summary>
        public string SourceJobDescription
        {
            get => _sourceJobDescription;
            set
            {
                _sourceJobDescription = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Excel配置数量
        /// </summary>
        public int ExcelConfigCount
        {
            get => _excelConfigCount;
            set
            {
                _excelConfigCount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// SQL脚本数量
        /// </summary>
        public int SqlScriptCount
        {
            get => _sqlScriptCount;
            set
            {
                _sqlScriptCount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 数据源数量
        /// </summary>
        public int DataSourceCount
        {
            get => _dataSourceCount;
            set
            {
                _dataSourceCount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 字段映射数量
        /// </summary>
        public int FieldMappingCount
        {
            get => _fieldMappingCount;
            set
            {
                _fieldMappingCount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 冲突列表
        /// </summary>
        public List<string> Conflicts
        {
            get => _conflicts;
            set
            {
                _conflicts = value;
                OnPropertyChanged();
                UpdateConflictStatus();
            }
        }

        /// <summary>
        /// 依赖关系
        /// </summary>
        public List<string> Dependencies
        {
            get => _dependencies;
            set
            {
                _dependencies = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 警告信息
        /// </summary>
        public List<string> Warnings
        {
            get => _warnings;
            set
            {
                _warnings = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 建议信息
        /// </summary>
        public List<string> Recommendations
        {
            get => _recommendations;
            set
            {
                _recommendations = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 预估导入时间
        /// </summary>
        public TimeSpan EstimatedImportTime
        {
            get => _estimatedImportTime;
            set
            {
                _estimatedImportTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否有冲突
        /// </summary>
        public bool HasConflicts
        {
            get => _hasConflicts;
            set
            {
                _hasConflicts = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否可以导入
        /// </summary>
        public bool CanImport
        {
            get => _canImport;
            set
            {
                _canImport = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 格式化后的包大小
        /// </summary>
        public string FormattedPackageSize
        {
            get
            {
                if (_packageSize < 1024)
                    return $"{_packageSize} B";
                else if (_packageSize < 1024 * 1024)
                    return $"{_packageSize / 1024.0:F1} KB";
                else
                    return $"{_packageSize / (1024.0 * 1024.0):F1} MB";
            }
        }

        /// <summary>
        /// 格式化后的导入时间
        /// </summary>
        public string FormattedImportTime
        {
            get
            {
                if (_estimatedImportTime.TotalSeconds < 60)
                    return $"{_estimatedImportTime.TotalSeconds:F0} 秒";
                else if (_estimatedImportTime.TotalMinutes < 60)
                    return $"{_estimatedImportTime.TotalMinutes:F1} 分钟";
                else
                    return $"{_estimatedImportTime.TotalHours:F1} 小时";
            }
        }

        /// <summary>
        /// 冲突状态文本
        /// </summary>
        public string ConflictStatusText
        {
            get
            {
                if (_hasConflicts)
                    return $"发现 {_conflicts.Count} 个潜在冲突";
                else
                    return "未发现冲突";
            }
        }

        /// <summary>
        /// 冲突状态颜色
        /// </summary>
        public string ConflictStatusColor
        {
            get
            {
                if (_hasConflicts)
                    return "#FF6B6B"; // 红色
                else
                    return "#4CAF50"; // 绿色
            }
        }

        /// <summary>
        /// 更新冲突状态
        /// </summary>
        private void UpdateConflictStatus()
        {
            HasConflicts = _conflicts != null && _conflicts.Count > 0;
            CanImport = !HasConflicts || _conflicts.Count <= 2; // 允许少量冲突
            OnPropertyChanged(nameof(ConflictStatusText));
            OnPropertyChanged(nameof(ConflictStatusColor));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 