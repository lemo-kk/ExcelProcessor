using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelProcessor.Models;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// 作业配置数据访问层接口
    /// </summary>
    public interface IJobRepository
    {
        /// <summary>
        /// 获取所有作业配置
        /// </summary>
        /// <returns>作业配置列表</returns>
        Task<List<JobConfig>> GetAllAsync();

        /// <summary>
        /// 根据ID获取作业配置
        /// </summary>
        /// <param name="id">作业ID</param>
        /// <returns>作业配置</returns>
        Task<JobConfig?> GetByIdAsync(string id);

        /// <summary>
        /// 根据名称获取作业配置
        /// </summary>
        /// <param name="name">作业名称</param>
        /// <returns>作业配置</returns>
        Task<JobConfig?> GetByNameAsync(string name);

        /// <summary>
        /// 根据类型获取作业配置列表
        /// </summary>
        /// <param name="type">作业类型</param>
        /// <returns>作业配置列表</returns>
        Task<List<JobConfig>> GetByTypeAsync(string type);

        /// <summary>
        /// 根据分类获取作业配置列表
        /// </summary>
        /// <param name="category">作业分类</param>
        /// <returns>作业配置列表</returns>
        Task<List<JobConfig>> GetByCategoryAsync(string category);

        /// <summary>
        /// 根据状态获取作业配置列表
        /// </summary>
        /// <param name="status">作业状态</param>
        /// <returns>作业配置列表</returns>
        Task<List<JobConfig>> GetByStatusAsync(JobStatus status);

        /// <summary>
        /// 搜索作业配置
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <returns>作业配置列表</returns>
        Task<List<JobConfig>> SearchAsync(string keyword);

        /// <summary>
        /// 创建作业配置
        /// </summary>
        /// <param name="jobConfig">作业配置</param>
        /// <returns>创建结果</returns>
        Task<bool> CreateAsync(JobConfig jobConfig);

        /// <summary>
        /// 更新作业配置
        /// </summary>
        /// <param name="jobConfig">作业配置</param>
        /// <returns>更新结果</returns>
        Task<bool> UpdateAsync(JobConfig jobConfig);

        /// <summary>
        /// 删除作业配置
        /// </summary>
        /// <param name="id">作业ID</param>
        /// <returns>删除结果</returns>
        Task<bool> DeleteAsync(string id);

        /// <summary>
        /// 批量删除作业配置
        /// </summary>
        /// <param name="ids">作业ID列表</param>
        /// <returns>删除结果</returns>
        Task<bool> BatchDeleteAsync(List<string> ids);

        /// <summary>
        /// 检查作业名称是否存在
        /// </summary>
        /// <param name="name">作业名称</param>
        /// <param name="excludeId">排除的作业ID</param>
        /// <returns>是否存在</returns>
        Task<bool> ExistsByNameAsync(string name, string? excludeId = null);

        /// <summary>
        /// 获取作业数量
        /// </summary>
        /// <returns>作业数量</returns>
        Task<int> GetCountAsync();

        /// <summary>
        /// 获取启用的作业数量
        /// </summary>
        /// <returns>启用的作业数量</returns>
        Task<int> GetEnabledCountAsync();
    }
} 