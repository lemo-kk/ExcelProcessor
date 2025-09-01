using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelProcessor.Models;

namespace ExcelProcessor.Core.Repositories
{
    /// <summary>
    /// 系统配置仓储接口
    /// </summary>
    public interface ISystemConfigRepository
    {
        /// <summary>
        /// 获取所有配置
        /// </summary>
        Task<IEnumerable<SystemConfig>> GetAllAsync();

        /// <summary>
        /// 根据键获取配置
        /// </summary>
        Task<SystemConfig?> GetByKeyAsync(string key);

        /// <summary>
        /// 添加配置
        /// </summary>
        Task<bool> AddAsync(SystemConfig config);

        /// <summary>
        /// 更新配置
        /// </summary>
        Task<bool> UpdateAsync(SystemConfig config);

        /// <summary>
        /// 删除配置
        /// </summary>
        Task<bool> DeleteAsync(string key);

        /// <summary>
        /// 检查配置是否存在
        /// </summary>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// 设置或更新配置
        /// </summary>
        Task<bool> SetOrUpdateAsync(SystemConfig config);
    }
} 