using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelProcessor.Models;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 作业服务接口
    /// </summary>
    public interface IJobService
    {
        #region 作业配置管理

        /// <summary>
        /// 获取所有作业配置
        /// </summary>
        /// <returns>作业配置列表</returns>
        Task<List<JobConfig>> GetAllJobsAsync();

        /// <summary>
        /// 根据ID获取作业配置
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <returns>作业配置</returns>
        Task<JobConfig?> GetJobByIdAsync(string jobId);

        /// <summary>
        /// 根据名称获取作业配置
        /// </summary>
        /// <param name="jobName">作业名称</param>
        /// <returns>作业配置</returns>
        Task<JobConfig?> GetJobByNameAsync(string jobName);

        /// <summary>
        /// 根据类型获取作业配置列表
        /// </summary>
        /// <param name="jobType">作业类型</param>
        /// <returns>作业配置列表</returns>
        Task<List<JobConfig>> GetJobsByTypeAsync(string jobType);

        /// <summary>
        /// 根据分类获取作业配置列表
        /// </summary>
        /// <param name="category">作业分类</param>
        /// <returns>作业配置列表</returns>
        Task<List<JobConfig>> GetJobsByCategoryAsync(string category);

        /// <summary>
        /// 根据状态获取作业配置列表
        /// </summary>
        /// <param name="status">作业状态</param>
        /// <returns>作业配置列表</returns>
        Task<List<JobConfig>> GetJobsByStatusAsync(JobStatus status);

        /// <summary>
        /// 搜索作业配置
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <returns>作业配置列表</returns>
        Task<List<JobConfig>> SearchJobsAsync(string keyword);

        /// <summary>
        /// 创建作业配置
        /// </summary>
        /// <param name="jobConfig">作业配置</param>
        /// <returns>创建结果</returns>
        Task<(bool success, string message)> CreateJobAsync(JobConfig jobConfig);

        /// <summary>
        /// 更新作业配置
        /// </summary>
        /// <param name="jobConfig">作业配置</param>
        /// <returns>更新结果</returns>
        Task<(bool success, string message)> UpdateJobAsync(JobConfig jobConfig);

        /// <summary>
        /// 删除作业配置
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <returns>删除结果</returns>
        Task<(bool success, string message)> DeleteJobAsync(string jobId);

        /// <summary>
        /// 批量删除作业配置
        /// </summary>
        /// <param name="jobIds">作业ID列表</param>
        /// <returns>删除结果</returns>
        Task<(bool success, string message)> BatchDeleteJobsAsync(List<string> jobIds);

        /// <summary>
        /// 复制作业配置
        /// </summary>
        /// <param name="jobId">源作业ID</param>
        /// <param name="newJobName">新作业名称</param>
        /// <returns>复制结果</returns>
        Task<(bool success, string message, JobConfig? newJob)> CopyJobAsync(string jobId, string newJobName);

        /// <summary>
        /// 启用/禁用作业
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <param name="isEnabled">是否启用</param>
        /// <returns>操作结果</returns>
        Task<(bool success, string message)> SetJobEnabledAsync(string jobId, bool isEnabled);

        #endregion

        #region 作业执行管理

        /// <summary>
        /// 手动执行作业
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <param name="parameters">执行参数</param>
        /// <param name="executedBy">执行人</param>
        /// <returns>执行结果</returns>
        Task<(bool success, string message, string executionId)> ExecuteJobAsync(string jobId, Dictionary<string, object>? parameters = null, string executedBy = "");

        /// <summary>
        /// 停止作业执行
        /// </summary>
        /// <param name="executionId">执行ID</param>
        /// <returns>停止结果</returns>
        Task<(bool success, string message)> StopJobExecutionAsync(string executionId);

        /// <summary>
        /// 暂停作业执行
        /// </summary>
        /// <param name="executionId">执行ID</param>
        /// <returns>暂停结果</returns>
        Task<(bool success, string message)> PauseJobExecutionAsync(string executionId);

        /// <summary>
        /// 恢复作业执行
        /// </summary>
        /// <param name="executionId">执行ID</param>
        /// <returns>恢复结果</returns>
        Task<(bool success, string message)> ResumeJobExecutionAsync(string executionId);

        /// <summary>
        /// 重试作业执行
        /// </summary>
        /// <param name="executionId">执行ID</param>
        /// <returns>重试结果</returns>
        Task<(bool success, string message, string newExecutionId)> RetryJobExecutionAsync(string executionId);

        #endregion

        #region 作业执行记录

        /// <summary>
        /// 获取作业执行记录
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <param name="page">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>执行记录列表</returns>
        Task<(List<JobExecution> executions, int totalCount)> GetJobExecutionsAsync(string jobId, int page = 1, int pageSize = 20);

        /// <summary>
        /// 获取执行记录详情
        /// </summary>
        /// <param name="executionId">执行ID</param>
        /// <returns>执行记录</returns>
        Task<JobExecution?> GetJobExecutionAsync(string executionId);

        /// <summary>
        /// 获取执行步骤记录
        /// </summary>
        /// <param name="executionId">执行ID</param>
        /// <returns>步骤执行记录列表</returns>
        Task<List<JobStepExecution>> GetJobStepExecutionsAsync(string executionId);

        /// <summary>
        /// 删除执行记录
        /// </summary>
        /// <param name="executionId">执行ID</param>
        /// <returns>删除结果</returns>
        Task<(bool success, string message)> DeleteJobExecutionAsync(string executionId);

        /// <summary>
        /// 清理过期执行记录
        /// </summary>
        /// <param name="daysToKeep">保留天数</param>
        /// <returns>清理结果</returns>
        Task<(bool success, string message, int deletedCount)> CleanupExpiredExecutionsAsync(int daysToKeep = 30);

        #endregion

        #region 作业统计

        /// <summary>
        /// 获取作业统计信息
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <returns>统计信息</returns>
        Task<JobStatistics?> GetJobStatisticsAsync(string jobId);

        /// <summary>
        /// 获取所有作业的统计信息
        /// </summary>
        /// <returns>统计信息列表</returns>
        Task<List<JobStatistics>> GetAllJobStatisticsAsync();

        /// <summary>
        /// 更新作业统计信息
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <returns>更新结果</returns>
        Task<(bool success, string message)> UpdateJobStatisticsAsync(string jobId);

        #endregion

        #region 作业调度

        /// <summary>
        /// 启动作业调度器
        /// </summary>
        /// <returns>启动结果</returns>
        Task<(bool success, string message)> StartSchedulerAsync();

        /// <summary>
        /// 停止作业调度器
        /// </summary>
        /// <returns>停止结果</returns>
        Task<(bool success, string message)> StopSchedulerAsync();

        /// <summary>
        /// 暂停作业调度器
        /// </summary>
        /// <returns>暂停结果</returns>
        Task<(bool success, string message)> PauseSchedulerAsync();

        /// <summary>
        /// 恢复作业调度器
        /// </summary>
        /// <returns>恢复结果</returns>
        Task<(bool success, string message)> ResumeSchedulerAsync();

        /// <summary>
        /// 获取调度器状态
        /// </summary>
        /// <returns>调度器状态</returns>
        Task<(bool isRunning, bool isPaused, DateTime? lastRunTime)> GetSchedulerStatusAsync();

        /// <summary>
        /// 添加定时作业
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <param name="cronExpression">Cron表达式</param>
        /// <returns>添加结果</returns>
        Task<(bool success, string message)> AddScheduledJobAsync(string jobId, string cronExpression);

        /// <summary>
        /// 移除定时作业
        /// </summary>
        /// <param name="jobId">作业ID</param>
        /// <returns>移除结果</returns>
        Task<(bool success, string message)> RemoveScheduledJobAsync(string jobId);

        /// <summary>
        /// 获取定时作业列表
        /// </summary>
        Task<List<(string jobId, string cronExpression, DateTime? nextRunTime)>> GetScheduledJobsAsync();

        /// <summary>
        /// 加载现有调度作业到调度器
        /// </summary>
        Task<(bool success, string message)> LoadScheduledJobsAsync();

        #endregion

        #region 作业监控

        /// <summary>
        /// 获取正在运行的作业
        /// </summary>
        /// <returns>运行中的作业列表</returns>
        Task<List<JobExecution>> GetRunningJobsAsync();

        /// <summary>
        /// 获取作业执行进度
        /// </summary>
        /// <param name="executionId">执行ID</param>
        /// <returns>执行进度</returns>
        Task<(int progress, string currentStep, DateTime startTime, DateTime? estimatedEndTime)> GetJobProgressAsync(string executionId);

        /// <summary>
        /// 订阅作业执行事件
        /// </summary>
        /// <param name="callback">事件回调</param>
        void SubscribeToJobEvents(Action<JobExecution> callback);

        /// <summary>
        /// 取消订阅作业执行事件
        /// </summary>
        /// <param name="callback">事件回调</param>
        void UnsubscribeFromJobEvents(Action<JobExecution> callback);

        #endregion

        #region 作业导入导出

        /// <summary>
        /// 导出作业配置
        /// </summary>
        /// <param name="jobIds">作业ID列表</param>
        /// <param name="filePath">导出文件路径</param>
        /// <returns>导出结果</returns>
        Task<(bool success, string message)> ExportJobsAsync(List<string> jobIds, string filePath);

        /// <summary>
        /// 导入作业配置
        /// </summary>
        /// <param name="filePath">导入文件路径</param>
        /// <param name="overwrite">是否覆盖现有配置</param>
        /// <returns>导入结果</returns>
        Task<(bool success, string message, List<JobConfig> importedJobs)> ImportJobsAsync(string filePath, bool overwrite = false);

        /// <summary>
        /// 验证作业配置
        /// </summary>
        /// <param name="jobConfig">作业配置</param>
        /// <returns>验证结果</returns>
        Task<(bool isValid, List<string> errors)> ValidateJobConfigAsync(JobConfig jobConfig);

        #endregion
    }
} 