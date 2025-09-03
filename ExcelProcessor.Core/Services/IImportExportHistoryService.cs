using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 导入导出历史记录服务接口
    /// </summary>
    public interface IImportExportHistoryService
    {
        #region 历史记录查询

        /// <summary>
        /// 获取所有导入导出历史记录
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>历史记录列表和总数</returns>
        Task<(List<ImportExportHistory> histories, int totalCount)> GetHistoryAsync(int page = 1, int pageSize = 20);

        /// <summary>
        /// 根据操作类型获取历史记录
        /// </summary>
        /// <param name="operationType">操作类型</param>
        /// <param name="page">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>历史记录列表和总数</returns>
        Task<(List<ImportExportHistory> histories, int totalCount)> GetHistoryByTypeAsync(string operationType, int page = 1, int pageSize = 20);

        /// <summary>
        /// 根据时间范围获取历史记录
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="page">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>历史记录列表和总数</returns>
        Task<(List<ImportExportHistory> histories, int totalCount)> GetHistoryByTimeRangeAsync(DateTime startTime, DateTime endTime, int page = 1, int pageSize = 20);

        /// <summary>
        /// 搜索历史记录
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <param name="page">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>历史记录列表和总数</returns>
        Task<(List<ImportExportHistory> histories, int totalCount)> SearchHistoryAsync(string keyword, int page = 1, int pageSize = 20);

        #endregion

        #region 历史记录管理

        /// <summary>
        /// 添加历史记录
        /// </summary>
        /// <param name="history">历史记录</param>
        /// <returns>是否添加成功</returns>
        Task<bool> AddHistoryAsync(ImportExportHistory history);

        /// <summary>
        /// 更新历史记录
        /// </summary>
        /// <param name="history">历史记录</param>
        /// <returns>是否更新成功</returns>
        Task<bool> UpdateHistoryAsync(ImportExportHistory history);

        /// <summary>
        /// 删除历史记录
        /// </summary>
        /// <param name="id">记录ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteHistoryAsync(string id);

        /// <summary>
        /// 批量删除历史记录
        /// </summary>
        /// <param name="ids">记录ID列表</param>
        /// <returns>删除成功的数量</returns>
        Task<int> BatchDeleteHistoryAsync(List<string> ids);

        /// <summary>
        /// 清空所有历史记录
        /// </summary>
        /// <returns>是否清空成功</returns>
        Task<bool> ClearAllHistoryAsync();

        #endregion

        #region 历史记录导出

        /// <summary>
        /// 导出历史记录到文件
        /// </summary>
        /// <param name="filePath">导出文件路径</param>
        /// <param name="format">导出格式（CSV, JSON, XML）</param>
        /// <param name="startTime">开始时间（可选）</param>
        /// <param name="endTime">结束时间（可选）</param>
        /// <returns>导出结果</returns>
        Task<(bool success, string message)> ExportHistoryToFileAsync(string filePath, string format = "CSV", DateTime? startTime = null, DateTime? endTime = null);

        /// <summary>
        /// 导出历史记录到Excel
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <param name="startTime">开始时间（可选）</param>
        /// <param name="endTime">结束时间（可选）</param>
        /// <returns>导出结果</returns>
        Task<(bool success, string message)> ExportHistoryToExcelAsync(string filePath, DateTime? startTime = null, DateTime? endTime = null);

        #endregion

        #region 统计信息

        /// <summary>
        /// 获取历史记录统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        Task<ImportExportStatistics> GetStatisticsAsync();

        /// <summary>
        /// 获取指定时间范围的统计信息
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>统计信息</returns>
        Task<ImportExportStatistics> GetStatisticsByTimeRangeAsync(DateTime startTime, DateTime endTime);

        #endregion
    }

    /// <summary>
    /// 导入导出历史记录
    /// </summary>
    public class ImportExportHistory
    {
        /// <summary>
        /// 记录ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime OperationTime { get; set; }

        /// <summary>
        /// 操作类型（Import, Export, Validate, Preview）
        /// </summary>
        public string OperationType { get; set; } = string.Empty;

        /// <summary>
        /// 操作状态（Success, Failed, InProgress, Cancelled）
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 作业名称（如果是作业相关操作）
        /// </summary>
        public string? JobName { get; set; }

        /// <summary>
        /// 配置文件路径
        /// </summary>
        public string? PackagePath { get; set; }

        /// <summary>
        /// 操作描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 操作用户
        /// </summary>
        public string OperatedBy { get; set; } = string.Empty;

        /// <summary>
        /// 是否可以从历史记录重新导入
        /// </summary>
        public bool CanReImport { get; set; }

        /// <summary>
        /// 操作结果消息
        /// </summary>
        public string? ResultMessage { get; set; }

        /// <summary>
        /// 操作耗时（毫秒）
        /// </summary>
        public long DurationMs { get; set; }

        /// <summary>
        /// 相关文件大小（字节）
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// 操作详情（JSON格式）
        /// </summary>
        public string? Details { get; set; }
    }

    /// <summary>
    /// 导入导出统计信息
    /// </summary>
    public class ImportExportStatistics
    {
        /// <summary>
        /// 总操作次数
        /// </summary>
        public int TotalOperations { get; set; }

        /// <summary>
        /// 成功操作次数
        /// </summary>
        public int SuccessfulOperations { get; set; }

        /// <summary>
        /// 失败操作次数
        /// </summary>
        public int FailedOperations { get; set; }

        /// <summary>
        /// 导入操作次数
        /// </summary>
        public int ImportOperations { get; set; }

        /// <summary>
        /// 导出操作次数
        /// </summary>
        public int ExportOperations { get; set; }

        /// <summary>
        /// 验证操作次数
        /// </summary>
        public int ValidationOperations { get; set; }

        /// <summary>
        /// 预览操作次数
        /// </summary>
        public int PreviewOperations { get; set; }

        /// <summary>
        /// 平均操作耗时（毫秒）
        /// </summary>
        public double AverageDurationMs { get; set; }

        /// <summary>
        /// 总文件大小（字节）
        /// </summary>
        public long TotalFileSize { get; set; }

        /// <summary>
        /// 最近操作时间
        /// </summary>
        public DateTime? LastOperationTime { get; set; }

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations * 100 : 0;
    }
} 