namespace ExcelProcessor.Core.Interfaces
{
    /// <summary>
    /// 导入进度回调接口
    /// </summary>
    public interface IImportProgressCallback
    {
        /// <summary>
        /// 更新进度
        /// </summary>
        /// <param name="progress">进度百分比 (0-100)</param>
        /// <param name="message">进度消息</param>
        void UpdateProgress(double progress, string message);

        /// <summary>
        /// 更新统计信息
        /// </summary>
        /// <param name="totalRows">总行数</param>
        /// <param name="processedRows">已处理行数</param>
        /// <param name="successRows">成功行数</param>
        /// <param name="failedRows">失败行数</param>
        void UpdateStatistics(int totalRows, int processedRows, int successRows, int failedRows);

        /// <summary>
        /// 更新当前处理行信息
        /// </summary>
        /// <param name="currentRow">当前处理行号</param>
        /// <param name="totalRows">总行数</param>
        void UpdateCurrentRow(int currentRow, int totalRows);

        /// <summary>
        /// 更新批次信息
        /// </summary>
        /// <param name="batchNumber">当前批次号</param>
        /// <param name="batchSize">批次大小</param>
        /// <param name="totalBatches">总批次数</param>
        void UpdateBatchInfo(int batchNumber, int batchSize, int totalBatches);

        /// <summary>
        /// 设置状态信息
        /// </summary>
        /// <param name="status">状态信息</param>
        void SetStatus(string status);
    }
} 