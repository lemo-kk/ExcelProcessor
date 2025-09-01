using ExcelProcessor.Models;
using ExcelProcessor.Data.Repositories;

namespace ExcelProcessor.Core.Repositories
{
    /// <summary>
    /// Excel配置仓储接口
    /// </summary>
    public interface IExcelConfigRepository : IRepository<ExcelConfig>
    {
        /// <summary>
        /// 根据配置名称获取配置
        /// </summary>
        /// <param name="configName">配置名称</param>
        /// <returns>Excel配置</returns>
        Task<ExcelConfig?> GetByNameAsync(string configName);

        /// <summary>
        /// 检查配置名称是否存在
        /// </summary>
        /// <param name="configName">配置名称</param>
        /// <returns>是否存在</returns>
        Task<bool> NameExistsAsync(string configName);

        /// <summary>
        /// 根据数据源获取配置
        /// </summary>
        /// <param name="dataSourceId">数据源ID</param>
        /// <returns>Excel配置列表</returns>
        Task<IEnumerable<ExcelConfig>> GetByDataSourceAsync(string dataSourceId);

        /// <summary>
        /// 获取启用的配置
        /// </summary>
        /// <returns>启用的配置列表</returns>
        Task<IEnumerable<ExcelConfig>> GetEnabledConfigsAsync();
    }
} 