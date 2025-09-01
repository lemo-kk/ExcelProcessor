using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 数据库表服务接口
    /// </summary>
    public interface IDatabaseTableService
    {
        /// <summary>
        /// 根据数据源名称获取所有表名
        /// </summary>
        /// <param name="dataSourceName">数据源名称</param>
        /// <returns>表名列表</returns>
        Task<List<string>> GetTableNamesAsync(string dataSourceName);

        /// <summary>
        /// 根据数据源名称和搜索关键词获取匹配的表名
        /// </summary>
        /// <param name="dataSourceName">数据源名称</param>
        /// <param name="searchKeyword">搜索关键词</param>
        /// <returns>匹配的表名列表</returns>
        Task<List<string>> SearchTableNamesAsync(string dataSourceName, string searchKeyword);
    }
} 