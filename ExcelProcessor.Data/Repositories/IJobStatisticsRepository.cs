using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelProcessor.Models;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// 作业统计信息数据访问层接口
    /// </summary>
    public interface IJobStatisticsRepository
    {
        /// <summary>
        /// 获取所有统计信息
        /// </summary>
        /// <returns>统计信息列表</returns>
        Task<List<JobStatistics>> GetAllAsync();

        /// <summary>
        /// 根据作业ID获取统计信息
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <returns>统计信息</returns>
        Task<JobStatistics?> GetByJobIdAsync(string jobId);

        /// <summary>
        /// 创建统计信息
        /// </summary>
        /// <param name="statistics">统计信息</param>
        /// <returns>创建结果</returns>
        Task<bool> CreateAsync(JobStatistics statistics);

        /// <summary>
        /// 更新统计信息
        /// </summary>
        /// <param name="statistics">统计信息</param>
        /// <returns>更新结果</returns>
        Task<bool> UpdateAsync(JobStatistics statistics);

        /// <summary>
        /// 删除统计信息
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <returns>删除结果</returns>
        Task<bool> DeleteAsync(string jobId);

        /// <summary>
        /// 批量删除统计信息
        /// </summary>
        /// <param name="jobIds">作业ID列表</param>
        /// <returns>删除结果</returns>
        Task<bool> BatchDeleteAsync(List<string> jobIds);

        /// <summary>
        /// 获取统计信息数量
        /// </summary>
        /// <returns>统计信息数量</returns>
        Task<int> GetCountAsync();

        /// <summary>
        /// 清理过期的统计信息
        /// </summary>
        /// <param name="daysToKeep">保留天数</param>
        /// <returns>清理的记录数量</returns>
        Task<int> CleanupExpiredAsync(int daysToKeep = 90);
    }
} 