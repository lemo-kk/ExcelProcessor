using ExcelProcessor.Models;
using ExcelProcessor.Data.Repositories;

namespace ExcelProcessor.Core.Repositories
{
    /// <summary>
    /// 数据源仓储接口
    /// </summary>
    public interface IDataSourceRepository : IRepository<DataSourceConfig>
    {
        /// <summary>
        /// 根据名称获取数据源
        /// </summary>
        /// <param name="name">数据源名称</param>
        /// <returns>数据源配置</returns>
        Task<DataSourceConfig?> GetByNameAsync(string name);

        /// <summary>
        /// 检查数据源名称是否存在
        /// </summary>
        /// <param name="name">数据源名称</param>
        /// <returns>是否存在</returns>
        Task<bool> NameExistsAsync(string name);

        /// <summary>
        /// 获取启用的数据源
        /// </summary>
        /// <returns>启用的数据源列表</returns>
        Task<IEnumerable<DataSourceConfig>> GetEnabledDataSourcesAsync();

        /// <summary>
        /// 根据类型获取数据源
        /// </summary>
        /// <param name="type">数据源类型</param>
        /// <returns>数据源列表</returns>
        Task<IEnumerable<DataSourceConfig>> GetByTypeAsync(string type);
    }
} 