using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExcelProcessor.Models;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 作业执行引擎接口
    /// </summary>
    public interface IJobExecutionEngine
    {
        #region 作业执行

        /// <summary>
        /// 执行作业
        /// </summary>
        /// <param name="jobConfig">作业配置</param>
        /// <param name="parameters">执行参数</param>
        /// <param name="executedBy">执行人</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行结果</returns>
        Task<JobExecutionResult> ExecuteJobAsync(JobConfig jobConfig, Dictionary<string, object>? parameters = null, string executedBy = "", CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行单个步骤
        /// </summary>
        /// <param name="step">作业步骤</param>
        /// <param name="context">执行上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>步骤执行结果</returns>
        Task<StepExecutionResult> ExecuteStepAsync(JobStep step, JobExecutionContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// 验证作业配置
        /// </summary>
        /// <param name="jobConfig">作业配置</param>
        /// <returns>验证结果</returns>
        Task<ValidationResult> ValidateJobConfigAsync(JobConfig jobConfig);

        /// <summary>
        /// 验证步骤配置
        /// </summary>
        /// <param name="step">作业步骤</param>
        /// <returns>验证结果</returns>
        Task<ValidationResult> ValidateStepConfigAsync(JobStep step);

        #endregion

        #region 执行上下文

        /// <summary>
        /// 创建执行上下文
        /// </summary>
        /// <param name="jobConfig">作业配置</param>
        /// <param name="parameters">执行参数</param>
        /// <param name="executedBy">执行人</param>
        /// <returns>执行上下文</returns>
        JobExecutionContext CreateExecutionContext(JobConfig jobConfig, Dictionary<string, object>? parameters = null, string executedBy = "");

        /// <summary>
        /// 获取执行上下文
        /// </summary>
        /// <param name="executionId">执行ID</param>
        /// <returns>执行上下文</returns>
        JobExecutionContext? GetExecutionContext(string executionId);

        /// <summary>
        /// 清理执行上下文
        /// </summary>
        /// <param name="executionId">执行ID</param>
        void CleanupExecutionContext(string executionId);

        #endregion

        #region 执行监控

        /// <summary>
        /// 获取执行进度
        /// </summary>
        /// <param name="executionId">执行ID</param>
        /// <returns>执行进度</returns>
        Task<ExecutionProgress> GetExecutionProgressAsync(string executionId);

        /// <summary>
        /// 更新执行进度
        /// </summary>
        /// <param name="executionId">执行ID</param>
        /// <param name="progress">进度信息</param>
        Task UpdateExecutionProgressAsync(string executionId, ExecutionProgress progress);

        /// <summary>
        /// 订阅执行事件
        /// </summary>
        /// <param name="callback">事件回调</param>
        void SubscribeToExecutionEvents(Action<ExecutionEvent> callback);

        /// <summary>
        /// 取消订阅执行事件
        /// </summary>
        /// <param name="callback">事件回调</param>
        void UnsubscribeFromExecutionEvents(Action<ExecutionEvent> callback);

        #endregion

        #region 错误处理

        /// <summary>
        /// 处理执行错误
        /// </summary>
        /// <param name="context">执行上下文</param>
        /// <param name="error">错误信息</param>
        /// <param name="step">当前步骤</param>
        /// <returns>错误处理结果</returns>
        Task<ErrorHandlingResult> HandleExecutionErrorAsync(JobExecutionContext context, Exception error, JobStep? step = null);

        /// <summary>
        /// 重试步骤执行
        /// </summary>
        /// <param name="step">作业步骤</param>
        /// <param name="context">执行上下文</param>
        /// <param name="retryCount">重试次数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>重试结果</returns>
        Task<StepExecutionResult> RetryStepExecutionAsync(JobStep step, JobExecutionContext context, int retryCount, CancellationToken cancellationToken = default);

        #endregion
    }

    /// <summary>
    /// 作业执行结果
    /// </summary>
    public class JobExecutionResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 执行ID
        /// </summary>
        public string ExecutionId { get; set; } = string.Empty;

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 执行时长（秒）
        /// </summary>
        public double DurationSeconds { get; set; }

        /// <summary>
        /// 执行结果数据
        /// </summary>
        public Dictionary<string, object> ResultData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 错误详情
        /// </summary>
        public string? ErrorDetails { get; set; }

        /// <summary>
        /// 步骤执行结果列表
        /// </summary>
        public List<StepExecutionResult> StepResults { get; set; } = new List<StepExecutionResult>();

        /// <summary>
        /// 执行日志
        /// </summary>
        public List<string> ExecutionLogs { get; set; } = new List<string>();
    }

    /// <summary>
    /// 步骤执行结果
    /// </summary>
    public class StepExecutionResult
    {
        /// <summary>
        /// 步骤ID
        /// </summary>
        public string StepId { get; set; } = string.Empty;

        /// <summary>
        /// 步骤名称
        /// </summary>
        public string StepName { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 执行时长（秒）
        /// </summary>
        public double DurationSeconds { get; set; }

        /// <summary>
        /// 输出数据
        /// </summary>
        public Dictionary<string, object> OutputData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 错误详情
        /// </summary>
        public string? ErrorDetails { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 执行日志
        /// </summary>
        public List<string> ExecutionLogs { get; set; } = new List<string>();
    }

    /// <summary>
    /// 作业执行上下文
    /// </summary>
    public class JobExecutionContext
    {
        /// <summary>
        /// 执行ID
        /// </summary>
        public string ExecutionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 作业配置
        /// </summary>
        public JobConfig JobConfig { get; set; } = new JobConfig();

        /// <summary>
        /// 执行参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 执行人
        /// </summary>
        public string ExecutedBy { get; set; } = string.Empty;

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 取消令牌
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// 变量存储
        /// </summary>
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 执行日志
        /// </summary>
        public List<string> Logs { get; set; } = new List<string>();

        /// <summary>
        /// 当前步骤索引
        /// </summary>
        public int CurrentStepIndex { get; set; } = -1;

        /// <summary>
        /// 执行进度
        /// </summary>
        public int Progress { get; set; } = 0;

        /// <summary>
        /// 是否已取消
        /// </summary>
        public bool IsCancelled => CancellationToken.IsCancellationRequested;

        /// <summary>
        /// 添加日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void AddLog(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            Logs.Add(logEntry);
        }

        /// <summary>
        /// 设置变量
        /// </summary>
        /// <param name="name">变量名</param>
        /// <param name="value">变量值</param>
        public void SetVariable(string name, object value)
        {
            Variables[name] = value;
        }

        /// <summary>
        /// 获取变量
        /// </summary>
        /// <param name="name">变量名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>变量值</returns>
        public T? GetVariable<T>(string name, T? defaultValue = default)
        {
            if (Variables.TryGetValue(name, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// 执行进度
    /// </summary>
    public class ExecutionProgress
    {
        /// <summary>
        /// 总进度（0-100）
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// 当前步骤
        /// </summary>
        public string CurrentStep { get; set; } = string.Empty;

        /// <summary>
        /// 当前步骤索引
        /// </summary>
        public int CurrentStepIndex { get; set; }

        /// <summary>
        /// 总步骤数
        /// </summary>
        public int TotalSteps { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 预计结束时间
        /// </summary>
        public DateTime? EstimatedEndTime { get; set; }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 执行事件
    /// </summary>
    public class ExecutionEvent
    {
        /// <summary>
        /// 执行ID
        /// </summary>
        public string ExecutionId { get; set; } = string.Empty;

        /// <summary>
        /// 事件类型
        /// </summary>
        public ExecutionEventType EventType { get; set; }

        /// <summary>
        /// 事件时间
        /// </summary>
        public DateTime EventTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 事件数据
        /// </summary>
        public Dictionary<string, object> EventData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 执行事件类型
    /// </summary>
    public enum ExecutionEventType
    {
        /// <summary>
        /// 作业开始
        /// </summary>
        JobStarted = 0,

        /// <summary>
        /// 作业完成
        /// </summary>
        JobCompleted = 1,

        /// <summary>
        /// 作业失败
        /// </summary>
        JobFailed = 2,

        /// <summary>
        /// 作业取消
        /// </summary>
        JobCancelled = 3,

        /// <summary>
        /// 步骤开始
        /// </summary>
        StepStarted = 4,

        /// <summary>
        /// 步骤完成
        /// </summary>
        StepCompleted = 5,

        /// <summary>
        /// 步骤失败
        /// </summary>
        StepFailed = 6,

        /// <summary>
        /// 进度更新
        /// </summary>
        ProgressUpdated = 7,

        /// <summary>
        /// 日志记录
        /// </summary>
        LogRecorded = 8
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误列表
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 警告列表
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// 错误处理结果
    /// </summary>
    public class ErrorHandlingResult
    {
        /// <summary>
        /// 是否已处理
        /// </summary>
        public bool IsHandled { get; set; }

        /// <summary>
        /// 是否继续执行
        /// </summary>
        public bool ShouldContinue { get; set; }

        /// <summary>
        /// 处理消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }
    }
} 