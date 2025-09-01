using ExcelProcessor.Models;
using ExcelProcessor.Data.Repositories;

namespace ExcelProcessor.Core.Repositories
{
    /// <summary>
    /// 配置引用仓储接口
    /// </summary>
    public interface IConfigurationReferenceRepository : IRepository<ConfigurationReference>
    {
        /// <summary>
        /// 根据引用名称获取配置引用
        /// </summary>
        /// <param name="referenceName">引用名称</param>
        /// <returns>配置引用</returns>
        Task<ConfigurationReference?> GetByNameAsync(string referenceName);

        /// <summary>
        /// 检查引用名称是否存在
        /// </summary>
        /// <param name="referenceName">引用名称</param>
        /// <returns>是否存在</returns>
        Task<bool> NameExistsAsync(string referenceName);

        /// <summary>
        /// 根据引用类型获取配置引用
        /// </summary>
        /// <param name="referenceType">引用类型</param>
        /// <returns>配置引用列表</returns>
        Task<IEnumerable<ConfigurationReference>> GetByTypeAsync(string referenceType);

        /// <summary>
        /// 根据目标配置获取引用
        /// </summary>
        /// <param name="targetConfigId">目标配置ID</param>
        /// <param name="targetConfigType">目标配置类型</param>
        /// <returns>配置引用列表</returns>
        Task<IEnumerable<ConfigurationReference>> GetByTargetConfigAsync(string targetConfigId, string targetConfigType);
    }
} 