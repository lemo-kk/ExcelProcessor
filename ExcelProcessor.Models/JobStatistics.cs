using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// 作业统计信息
    /// </summary>
    public class JobStatistics : INotifyPropertyChanged
    {
        private string _jobId = string.Empty;
        private string _jobName = string.Empty;
        private int _totalExecutions = 0;
        private int _successfulExecutions = 0;
        private int _failedExecutions = 0;
        private int _cancelledExecutions = 0;
        private double _successRate = 0.0;
        private TimeSpan _averageDuration = TimeSpan.Zero;
        private TimeSpan _totalDuration = TimeSpan.Zero;
        private DateTime _lastExecutionTime = DateTime.MinValue;
        private DateTime _firstExecutionTime = DateTime.MinValue;
        private DateTime _createdAt = DateTime.Now;
        private DateTime _updatedAt = DateTime.Now;

        /// <summary>
        /// 作业ID
        /// </summary>
        public string JobId
        {
            get => _jobId;
            set => SetProperty(ref _jobId, value);
        }

        /// <summary>
        /// 作业名称
        /// </summary>
        public string JobName
        {
            get => _jobName;
            set => SetProperty(ref _jobName, value);
        }

        /// <summary>
        /// 总执行次数
        /// </summary>
        public int TotalExecutions
        {
            get => _totalExecutions;
            set => SetProperty(ref _totalExecutions, value);
        }

        /// <summary>
        /// 成功执行次数
        /// </summary>
        public int SuccessfulExecutions
        {
            get => _successfulExecutions;
            set => SetProperty(ref _successfulExecutions, value);
        }

        /// <summary>
        /// 失败执行次数
        /// </summary>
        public int FailedExecutions
        {
            get => _failedExecutions;
            set => SetProperty(ref _failedExecutions, value);
        }

        /// <summary>
        /// 取消执行次数
        /// </summary>
        public int CancelledExecutions
        {
            get => _cancelledExecutions;
            set => SetProperty(ref _cancelledExecutions, value);
        }

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate
        {
            get => _successRate;
            set => SetProperty(ref _successRate, value);
        }

        /// <summary>
        /// 平均执行时长
        /// </summary>
        public TimeSpan AverageDuration
        {
            get => _averageDuration;
            set => SetProperty(ref _averageDuration, value);
        }

        /// <summary>
        /// 总执行时长
        /// </summary>
        public TimeSpan TotalDuration
        {
            get => _totalDuration;
            set => SetProperty(ref _totalDuration, value);
        }

        /// <summary>
        /// 最后执行时间
        /// </summary>
        public DateTime LastExecutionTime
        {
            get => _lastExecutionTime;
            set => SetProperty(ref _lastExecutionTime, value);
        }

        /// <summary>
        /// 首次执行时间
        /// </summary>
        public DateTime FirstExecutionTime
        {
            get => _firstExecutionTime;
            set => SetProperty(ref _firstExecutionTime, value);
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetProperty(ref _updatedAt, value);
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
} 