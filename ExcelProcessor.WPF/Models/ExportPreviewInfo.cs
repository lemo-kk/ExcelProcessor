using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExcelProcessor.WPF.Models
{
    /// <summary>
    /// 导出预览信息
    /// </summary>
    public class ExportPreviewInfo : INotifyPropertyChanged
    {
        private string _jobName;
        private string _jobDescription;
        private DateTime _jobCreatedAt;
        private bool _jobIsEnabled;
        private int _excelConfigCount;
        private int _sqlScriptCount;
        private int _dataSourceCount;
        private int _fieldMappingCount;
        private int _jobStepCount;
        private long _estimatedPackageSize;
        private TimeSpan _estimatedExportTime;
        private List<string> _dependencies;
        private List<string> _warnings;
        private List<string> _recommendations;

        public ExportPreviewInfo()
        {
            Dependencies = new List<string>();
            Warnings = new List<string>();
            Recommendations = new List<string>();
        }

        /// <summary>
        /// 作业名称
        /// </summary>
        public string JobName
        {
            get => _jobName;
            set
            {
                _jobName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 作业描述
        /// </summary>
        public string JobDescription
        {
            get => _jobDescription;
            set
            {
                _jobDescription = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 作业创建时间
        /// </summary>
        public DateTime JobCreatedAt
        {
            get => _jobCreatedAt;
            set
            {
                _jobCreatedAt = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 作业是否启用
        /// </summary>
        public bool JobIsEnabled
        {
            get => _jobIsEnabled;
            set
            {
                _jobIsEnabled = value;
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
        /// 作业步骤数量
        /// </summary>
        public int JobStepCount
        {
            get => _jobStepCount;
            set
            {
                _jobStepCount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 预估包大小（字节）
        /// </summary>
        public long EstimatedPackageSize
        {
            get => _estimatedPackageSize;
            set
            {
                _estimatedPackageSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 预估导出时间
        /// </summary>
        public TimeSpan EstimatedExportTime
        {
            get => _estimatedExportTime;
            set
            {
                _estimatedExportTime = value;
                OnPropertyChanged();
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
        /// 格式化后的包大小
        /// </summary>
        public string FormattedPackageSize
        {
            get
            {
                if (_estimatedPackageSize < 1024)
                    return $"{_estimatedPackageSize} B";
                else if (_estimatedPackageSize < 1024 * 1024)
                    return $"{_estimatedPackageSize / 1024.0:F1} KB";
                else
                    return $"{_estimatedPackageSize / (1024.0 * 1024.0):F1} MB";
            }
        }

        /// <summary>
        /// 格式化后的导出时间
        /// </summary>
        public string FormattedExportTime
        {
            get
            {
                if (_estimatedExportTime.TotalSeconds < 60)
                    return $"{_estimatedExportTime.TotalSeconds:F0} 秒";
                else if (_estimatedExportTime.TotalMinutes < 60)
                    return $"{_estimatedExportTime.TotalMinutes:F1} 分钟";
                else
                    return $"{_estimatedExportTime.TotalHours:F1} 小时";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 