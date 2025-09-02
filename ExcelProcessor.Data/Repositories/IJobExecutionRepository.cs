using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelProcessor.Models;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// 作业执行记录数据访问层接口
    /// </summary>
    public interface IJobExecutionRepository
    {
        /// <summary>
        /// 获取所有执行记录
        /// </summary>
        /// <returns>执行记录列表</returns>
        Task<List<JobExecution>> GetAllAsync();

        /// <summary>
        /// 根据ID获取执行记录
        /// </summary>
        /// <param name="id">执行ID</param>
        /// <returns>执行记录</returns>
        Task<JobExecution?> GetByIdAsync(string id);

        /// <summary>
        /// 根据作业ID获取执行记录
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <param name="page">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>执行记录列表和总数</returns>
        Task<(List<JobExecution> executions, int totalCount)> GetByJobIdAsync(string jobId, int page = 1, int pageSize = 20);

        /// <summary>
        /// 根据作业ID获取所有执行记录
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <returns>执行记录列表</returns>
        Task<List<JobExecution>> GetAllByJobIdAsync(string jobId);

        /// <summary>
        /// 根据作业ID获取最新的执行记录
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <returns>最新的执行记录</returns>
        Task<JobExecution?> GetLatestByJobIdAsync(string jobId);

        /// <summary>
        /// 获取正在运行的执行记录
        /// </summary>
        /// <returns>执行记录列表</returns>
        Task<List<JobExecution>> GetRunningAsync();

        /// <summary>
        /// 根据状态获取执行记录
        /// </summary>
        /// <param name="status">执行状态</param>
        /// <returns>执行记录列表</returns>
        Task<List<JobExecution>> GetByStatusAsync(JobStatus status);

        /// <summary>
        /// 根据时间范围获取执行记录
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>执行记录列表</returns>
        Task<List<JobExecution>> GetByTimeRangeAsync(DateTime startTime, DateTime endTime);

        /// <summary>
        /// 根据执行人获取执行记录
        /// </summary>
        /// <param name="executedBy">执行人</param>
        /// <returns>执行记录列表</returns>
        Task<List<JobExecution>> GetByExecutedByAsync(string executedBy);

        /// <summary>
        /// 创建执行记录
        /// </summary>
        /// <param name="execution">执行记录</param>
        /// <returns>创建结果</returns>
        Task<bool> CreateAsync(JobExecution execution);

        /// <summary>
        /// 更新执行记录
        /// </summary>
        /// <param name="execution">执行记录</param>
        /// <returns>更新结果</returns>
        Task<bool> UpdateAsync(JobExecution execution);

        /// <summary>
        /// 删除执行记录
        /// </summary>
        /// <param name="id">执行ID</param>
        /// <returns>删除结果</returns>
        Task<bool> DeleteAsync(string id);

        /// <summary>
        /// 批量删除执行记录
        /// </summary>
        /// <param name="ids">执行ID列表</param>
        /// <returns>删除结果</returns>
        Task<bool> BatchDeleteAsync(List<string> ids);

        /// <summary>
        /// 删除过期的执行记录
        /// </summary>
        /// <param name="cutoffDate">截止日期</param>
        /// <returns>删除的记录数量</returns>
        Task<int> DeleteExpiredAsync(DateTime cutoffDate);

        /// <summary>
        /// 获取执行记录数量
        /// </summary>
        /// <returns>执行记录数量</returns>
        Task<int> GetCountAsync();

        /// <summary>
        /// 根据作业ID获取执行记录数量
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <returns>执行记录数量</returns>
        Task<int> GetCountByJobIdAsync(string jobId);

        /// <summary>
        /// 根据状态获取执行记录数量
        /// </summary>
        /// <param name="status">执行状态</param>
        /// <returns>执行记录数量</returns>
        Task<int> GetCountByStatusAsync(JobStatus status);

        #region 步骤执行记录

        /// <summary>
        /// 获取步骤执行记录
        /// </summary>
        /// <param name="executionId">执行ID</param>
        /// <returns>步骤执行记录列表</returns>
        Task<List<JobStepExecution>> GetStepExecutionsAsync(string executionId);

        /// <summary>
        /// 根据ID获取步骤执行记录
        /// </summary>
        /// <param name="id">步骤执行ID</param>
        /// <returns>步骤执行记录</returns>
        Task<JobStepExecution?> GetStepExecutionByIdAsync(string id);

        /// <summary>
        /// 创建步骤执行记录
        /// </summary>
        /// <param name="stepExecution">步骤执行记录</param>
        /// <returns>创建结果</returns>
        Task<bool> CreateStepExecutionAsync(JobStepExecution stepExecution);

        /// <summary>
        /// 更新步骤执行记录
        /// </summary>
        /// <param name="stepExecution">步骤执行记录</param>
        /// <returns>更新结果</returns>
        Task<bool> UpdateStepExecutionAsync(JobStepExecution stepExecution);

        /// <summary>
        /// 删除步骤执行记录
        /// </summary>
        /// <param name="id">步骤执行ID</param>
        /// <returns>删除结果</returns>
        Task<bool> DeleteStepExecutionAsync(string id);

        /// <summary>
        /// 根据执行ID删除步骤执行记录
        /// </summary>
        /// <param name="executionId">执行ID</param>
        /// <returns>删除结果</returns>
        Task<bool> DeleteStepExecutionsByExecutionIdAsync(string executionId);

        #endregion
    }
} 