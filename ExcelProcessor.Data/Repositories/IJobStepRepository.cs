using ExcelProcessor.Models;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// 作业步骤数据访问层接口
    /// </summary>
    public interface IJobStepRepository
    {
        /// <summary>
        /// 获取作业的所有步骤
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <returns>步骤列表</returns>
        Task<List<JobStep>> GetByJobIdAsync(string jobId);

        /// <summary>
        /// 根据ID获取步骤
        /// </summary>
        /// <param name="id">步骤ID</param>
        /// <returns>步骤信息</returns>
        Task<JobStep?> GetByIdAsync(string id);

        /// <summary>
        /// 创建步骤
        /// </summary>
        /// <param name="step">步骤信息</param>
        /// <returns>是否成功</returns>
        Task<bool> CreateAsync(JobStep step);

        /// <summary>
        /// 更新步骤
        /// </summary>
        /// <param name="step">步骤信息</param>
        /// <returns>是否成功</returns>
        Task<bool> UpdateAsync(JobStep step);

        /// <summary>
        /// 删除步骤
        /// </summary>
        /// <param name="id">步骤ID</param>
        /// <returns>是否成功</returns>
        Task<bool> DeleteAsync(string id);

        /// <summary>
        /// 删除作业的所有步骤
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <returns>是否成功</returns>
        Task<bool> DeleteByJobIdAsync(string jobId);

        /// <summary>
        /// 批量创建步骤
        /// </summary>
        /// <param name="steps">步骤列表</param>
        /// <returns>是否成功</returns>
        Task<bool> CreateBatchAsync(List<JobStep> steps);

        /// <summary>
        /// 更新步骤顺序
        /// </summary>
        /// <param name="steps">步骤列表（包含新的顺序）</param>
        /// <returns>是否成功</returns>
        Task<bool> UpdateOrderAsync(List<JobStep> steps);
    }
} 