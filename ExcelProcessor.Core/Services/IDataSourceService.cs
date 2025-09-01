using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelProcessor.Models;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 数据源服务接口
    /// </summary>
    public interface IDataSourceService
    {
        /// <summary>
        /// 获取所有数据源配置
        /// </summary>
        Task<List<DataSourceConfig>> GetAllDataSourcesAsync();

        /// <summary>
        /// 根据ID获取数据源配置
        /// </summary>
        Task<DataSourceConfig> GetDataSourceByIdAsync(string id);

        /// <summary>
        /// 根据名称获取数据源配置
        /// </summary>
        Task<DataSourceConfig> GetDataSourceByNameAsync(string name);

        /// <summary>
        /// 保存数据源配置
        /// </summary>
        Task<bool> SaveDataSourceAsync(DataSourceConfig dataSource);

        /// <summary>
        /// 更新数据源配置
        /// </summary>
        Task<bool> UpdateDataSourceAsync(DataSourceConfig dataSource);

        /// <summary>
        /// 删除数据源配置
        /// </summary>
        Task<bool> DeleteDataSourceAsync(string id);

        /// <summary>
        /// 测试数据源连接
        /// </summary>
        Task<bool> TestConnectionAsync(DataSourceConfig dataSource);

        /// <summary>
        /// 测试数据源连接并返回详细错误信息
        /// </summary>
        Task<(bool isConnected, string errorMessage)> TestConnectionWithDetailsAsync(DataSourceConfig dataSource);

        /// <summary>
        /// 批量测试所有数据源连接
        /// </summary>
        Task<List<DataSourceConfig>> TestAllConnectionsAsync();

        /// <summary>
        /// 检查数据源名称是否已存在
        /// </summary>
        Task<bool> IsDataSourceNameExistsAsync(string name, string? excludeId = null);
    }
} 