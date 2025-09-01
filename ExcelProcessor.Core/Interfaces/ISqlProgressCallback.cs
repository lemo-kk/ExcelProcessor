namespace ExcelProcessor.Core.Interfaces
{
    /// <summary>
    /// SQL执行进度回调接口
    /// </summary>
    public interface ISqlProgressCallback
    {
        /// <summary>
        /// 更新当前操作
        /// </summary>
        /// <param name="operation">当前操作描述</param>
        void UpdateOperation(string operation);

        /// <summary>
        /// 更新详细信息
        /// </summary>
        /// <param name="message">详细信息</param>
        void UpdateDetailMessage(string message);

        /// <summary>
        /// 更新子详细信息
        /// </summary>
        /// <param name="message">子详细信息</param>
        void UpdateSubDetailMessage(string message);

        /// <summary>
        /// 更新进度
        /// </summary>
        /// <param name="progress">进度百分比（0-100）</param>
        void UpdateProgress(double progress);

        /// <summary>
        /// 更新统计信息
        /// </summary>
        /// <param name="processedCount">已处理数量</param>
        /// <param name="totalCount">总数量</param>
        void UpdateStatistics(int processedCount, int totalCount);

        /// <summary>
        /// 更新处理速度
        /// </summary>
        /// <param name="speedText">速度文本</param>
        void UpdateSpeed(string speedText);

        /// <summary>
        /// 更新状态信息
        /// </summary>
        /// <param name="status">状态信息</param>
        void UpdateStatus(string status);

        /// <summary>
        /// 检查是否已取消
        /// </summary>
        /// <returns>是否已取消</returns>
        bool IsCancelled();
    }
} 