using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelProcessor.Models;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 配置引用服务接口
    /// </summary>
    public interface IConfigurationReferenceService
    {
        #region 配置引用管理

        /// <summary>
        /// 创建配置引用
        /// </summary>
        /// <param name="reference">配置引用</param>
        /// <returns>创建结果</returns>
        Task<(bool success, string message)> CreateReferenceAsync(ConfigurationReference reference);

        /// <summary>
        /// 更新配置引用
        /// </summary>
        /// <param name="reference">配置引用</param>
        /// <returns>更新结果</returns>
        Task<(bool success, string message)> UpdateReferenceAsync(ConfigurationReference reference);

        /// <summary>
        /// 删除配置引用
        /// </summary>
        /// <param name="referenceId">引用ID</param>
        /// <returns>删除结果</returns>
        Task<(bool success, string message)> DeleteReferenceAsync(string referenceId);

        /// <summary>
        /// 获取配置引用
        /// </summary>
        /// <param name="referenceId">引用ID</param>
        /// <returns>配置引用</returns>
        Task<ConfigurationReference?> GetReferenceByIdAsync(string referenceId);

        /// <summary>
        /// 获取所有配置引用
        /// </summary>
        /// <returns>配置引用列表</returns>
        Task<List<ConfigurationReference>> GetAllReferencesAsync();

        /// <summary>
        /// 根据类型获取配置引用
        /// </summary>
        /// <param name="type">引用类型</param>
        /// <returns>配置引用列表</returns>
        Task<List<ConfigurationReference>> GetReferencesByTypeAsync(ReferenceType type);

        /// <summary>
        /// 搜索配置引用
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <returns>匹配的配置引用列表</returns>
        Task<List<ConfigurationReference>> SearchReferencesAsync(string keyword);

        #endregion

        #region 配置引用执行

        /// <summary>
        /// 执行配置引用
        /// </summary>
        /// <param name="referenceId">引用ID</param>
        /// <param name="parameters">执行参数</param>
        /// <returns>执行结果</returns>
        Task<ReferenceExecutionResult> ExecuteReferenceAsync(string referenceId, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// 批量执行配置引用
        /// </summary>
        /// <param name="referenceIds">引用ID列表</param>
        /// <param name="parameters">执行参数</param>
        /// <returns>执行结果列表</returns>
        Task<List<ReferenceExecutionResult>> ExecuteReferencesAsync(List<string> referenceIds, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// 验证配置引用
        /// </summary>
        /// <param name="reference">配置引用</param>
        /// <returns>验证结果</returns>
        Task<(bool isValid, List<string> errors)> ValidateReferenceAsync(ConfigurationReference reference);

        #endregion

        #region 配置引用解析

        /// <summary>
        /// 解析配置引用（获取引用的实际配置）
        /// </summary>
        /// <param name="reference">配置引用</param>
        /// <returns>引用的配置对象</returns>
        Task<object?> ResolveReferenceAsync(ConfigurationReference reference);

        /// <summary>
        /// 解析Excel配置引用
        /// </summary>
        /// <param name="reference">配置引用</param>
        /// <returns>Excel配置</returns>
        Task<ExcelConfig?> ResolveExcelConfigReferenceAsync(ConfigurationReference reference);

        /// <summary>
        /// 解析SQL配置引用
        /// </summary>
        /// <param name="reference">配置引用</param>
        /// <returns>SQL配置</returns>
        Task<SqlConfig?> ResolveSqlConfigReferenceAsync(ConfigurationReference reference);

        /// <summary>
        /// 解析数据源配置引用
        /// </summary>
        /// <param name="reference">配置引用</param>
        /// <returns>数据源配置</returns>
        Task<DataSourceConfig?> ResolveDataSourceConfigReferenceAsync(ConfigurationReference reference);

        #endregion
    }
} 