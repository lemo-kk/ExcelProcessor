using ExcelProcessor.Models;
using ExcelProcessor.Data.Repositories;

namespace ExcelProcessor.Core.Repositories
{
    /// <summary>
    /// 作业步骤执行仓储接口
    /// </summary>
    public interface IJobStepExecutionRepository : IRepository<JobStepExecution>
    {
        /// <summary>
        /// 根据作业执行ID获取步骤执行记录
        /// </summary>
        /// <param name="jobExecutionId">作业执行ID</param>
        /// <returns>步骤执行记录列表</returns>
        Task<IEnumerable<JobStepExecution>> GetByJobExecutionIdAsync(int jobExecutionId);

        /// <summary>
        /// 根据作业步骤ID获取执行记录
        /// </summary>
        /// <param name="jobStepId">作业步骤ID</param>
        /// <returns>执行记录列表</returns>
        Task<IEnumerable<JobStepExecution>> GetByJobStepIdAsync(int jobStepId);

        /// <summary>
        /// 获取最近的步骤执行记录
        /// </summary>
        /// <param name="limit">限制数量</param>
        /// <returns>执行记录列表</returns>
        Task<IEnumerable<JobStepExecution>> GetRecentExecutionsAsync(int limit = 100);

        /// <summary>
        /// 删除过期的执行记录
        /// </summary>
        /// <param name="daysToKeep">保留天数</param>
        /// <returns>删除的记录数量</returns>
        Task<int> DeleteExpiredExecutionsAsync(int daysToKeep = 30);
    }
} 