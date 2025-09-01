using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// 作业步骤执行记录
    /// </summary>
    public class JobStepExecution : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _executionId = string.Empty;
        private string _stepId = string.Empty;
        private string _stepName = string.Empty;
        private JobStepType _stepType = JobStepType.ExcelImport;
        private int _stepOrder = 0;
        private JobStatus _status = JobStatus.Pending;
        private DateTime _startTime = DateTime.Now;
        private DateTime? _endTime;
        private TimeSpan? _duration;
        private string? _errorMessage;
        private string? _resultMessage;
        private Dictionary<string, object> _parameters = new();
        private Dictionary<string, object> _results = new();
        private DateTime _createdAt = DateTime.Now;
        private DateTime _updatedAt = DateTime.Now;

        /// <summary>
        /// 步骤执行ID
        /// </summary>
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// 执行记录ID
        /// </summary>
        public string ExecutionId
        {
            get => _executionId;
            set => SetProperty(ref _executionId, value);
        }

        /// <summary>
        /// 步骤ID
        /// </summary>
        public string StepId
        {
            get => _stepId;
            set => SetProperty(ref _stepId, value);
        }

        /// <summary>
        /// 步骤名称
        /// </summary>
        public string StepName
        {
            get => _stepName;
            set => SetProperty(ref _stepName, value);
        }

        /// <summary>
        /// 步骤类型
        /// </summary>
        public JobStepType StepType
        {
            get => _stepType;
            set => SetProperty(ref _stepType, value);
        }

        /// <summary>
        /// 步骤顺序
        /// </summary>
        public int StepOrder
        {
            get => _stepOrder;
            set => SetProperty(ref _stepOrder, value);
        }

        /// <summary>
        /// 执行状态
        /// </summary>
        public JobStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime
        {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        /// <summary>
        /// 执行时长
        /// </summary>
        public TimeSpan? Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// 结果信息
        /// </summary>
        public string? ResultMessage
        {
            get => _resultMessage;
            set => SetProperty(ref _resultMessage, value);
        }

        /// <summary>
        /// 执行参数
        /// </summary>
        public Dictionary<string, object> Parameters
        {
            get => _parameters;
            set => SetProperty(ref _parameters, value);
        }

        /// <summary>
        /// 执行结果
        /// </summary>
        public Dictionary<string, object> Results
        {
            get => _results;
            set => SetProperty(ref _results, value);
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