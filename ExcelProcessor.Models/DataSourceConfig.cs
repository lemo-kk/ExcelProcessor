using System;
using System.ComponentModel;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// 数据源配置模型
    /// </summary>
    public class DataSourceConfig : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _type = string.Empty;
        private string _description = string.Empty;
        private string _connectionString = string.Empty;
        private bool _isConnected;
        private string _status = string.Empty;
        private DateTime _lastTestTime;
        private bool _isEnabled;
        private bool _isDefault;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// 数据源名称
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
        
        /// <summary>
        /// 数据源类型 (SQLite, MySQL, SQLServer, PostgreSQL, Oracle)
        /// </summary>
        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }
        
        /// <summary>
        /// 数据源描述
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }
        
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString
        {
            get => _connectionString;
            set
            {
                _connectionString = value;
                OnPropertyChanged(nameof(ConnectionString));
            }
        }
        
        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
            }
        }
        
        /// <summary>
        /// 连接状态
        /// </summary>
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }
        
        /// <summary>
        /// 最后测试时间
        /// </summary>
        public DateTime LastTestTime
        {
            get => _lastTestTime;
            set
            {
                _lastTestTime = value;
                OnPropertyChanged(nameof(LastTestTime));
            }
        }
        
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        /// <summary>
        /// 是否为默认数据库
        /// </summary>
        public bool IsDefault
        {
            get => _isDefault;
            set
            {
                _isDefault = value;
                OnPropertyChanged(nameof(IsDefault));
            }
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedTime { get; set; } = DateTime.Now;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 克隆数据源配置
        /// </summary>
        public DataSourceConfig Clone()
        {
            return new DataSourceConfig
            {
                Id = this.Id,
                Name = this.Name,
                Type = this.Type,
                Description = this.Description,
                ConnectionString = this.ConnectionString,
                IsConnected = this.IsConnected,
                Status = this.Status,
                LastTestTime = this.LastTestTime,
                IsEnabled = this.IsEnabled,
                IsDefault = this.IsDefault,
                CreatedTime = this.CreatedTime,
                UpdatedTime = this.UpdatedTime
            };
        }
    }
} 