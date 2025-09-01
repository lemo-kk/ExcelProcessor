namespace ExcelProcessor.Models
{
    /// <summary>
    /// 作业状态
    /// </summary>
    public enum JobStatus
    {
        /// <summary>
        /// 待执行
        /// </summary>
        Pending = 0,

        /// <summary>
        /// 运行中
        /// </summary>
        Running = 1,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 2,

        /// <summary>
        /// 失败
        /// </summary>
        Failed = 3,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = 4,

        /// <summary>
        /// 已暂停
        /// </summary>
        Paused = 5,

        /// <summary>
        /// 已停止
        /// </summary>
        Stopped = 6,

        /// <summary>
        /// 已跳过
        /// </summary>
        Skipped = 7,

        /// <summary>
        /// 等待中
        /// </summary>
        Waiting = 8,

        /// <summary>
        /// 重试中
        /// </summary>
        Retrying = 9,

        /// <summary>
        /// 超时
        /// </summary>
        Timeout = 10
    }
} 