using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelProcessor.Models;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 系统配置服务接口
    /// </summary>
    public interface ISystemConfigService
    {
        /// <summary>
        /// 获取所有系统配置
        /// </summary>
        Task<List<SystemConfig>> GetAllConfigsAsync();

        /// <summary>
        /// 根据键获取配置值
        /// </summary>
        Task<string?> GetConfigValueAsync(string key);

        /// <summary>
        /// 根据键获取配置值（带默认值）
        /// </summary>
        Task<string> GetConfigValueAsync(string key, string defaultValue);

        /// <summary>
        /// 根据键获取布尔配置值
        /// </summary>
        Task<bool> GetBoolConfigAsync(string key, bool defaultValue = false);

        /// <summary>
        /// 根据键获取整数配置值
        /// </summary>
        Task<int> GetIntConfigAsync(string key, int defaultValue = 0);

        /// <summary>
        /// 设置配置值
        /// </summary>
        Task<bool> SetConfigValueAsync(string key, string value, string? description = null);

        /// <summary>
        /// 设置布尔配置值
        /// </summary>
        Task<bool> SetBoolConfigAsync(string key, bool value, string? description = null);

        /// <summary>
        /// 设置整数配置值
        /// </summary>
        Task<bool> SetIntConfigAsync(string key, int value, string? description = null);

        /// <summary>
        /// 删除配置
        /// </summary>
        Task<bool> DeleteConfigAsync(string key);

        /// <summary>
        /// 检查配置是否存在
        /// </summary>
        Task<bool> ConfigExistsAsync(string key);

        /// <summary>
        /// 批量更新配置
        /// </summary>
        Task<bool> BatchUpdateConfigsAsync(Dictionary<string, string> configs);

        /// <summary>
        /// 获取系统是否启用登录
        /// </summary>
        Task<bool> IsLoginEnabledAsync();

        /// <summary>
        /// 设置系统是否启用登录
        /// </summary>
        Task<bool> SetLoginEnabledAsync(bool enabled);

        /// <summary>
        /// 获取系统设置
        /// </summary>
        Task<SystemSettings> GetSystemSettings();

        /// <summary>
        /// 保存系统设置
        /// </summary>
        Task<bool> SaveSystemSettings(SystemSettings settings);

        /// <summary>
        /// 重置到默认设置
        /// </summary>
        Task<bool> ResetToDefaults();
    }
} 