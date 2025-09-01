using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExcelProcessor.Core.Repositories;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// 系统配置服务实现
    /// </summary>
    public class SystemConfigService : ISystemConfigService
    {
        private readonly ISystemConfigRepository _configRepository;
        private readonly ILogger<SystemConfigService> _logger;

        public SystemConfigService(ISystemConfigRepository configRepository, ILogger<SystemConfigService> logger)
        {
            _configRepository = configRepository;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有系统配置
        /// </summary>
        public async Task<List<SystemConfig>> GetAllConfigsAsync()
        {
            try
            {
                _logger.LogInformation("获取所有系统配置");
                var configs = await _configRepository.GetAllAsync();
                return configs.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有系统配置失败");
                return new List<SystemConfig>();
            }
        }

        /// <summary>
        /// 根据键获取配置值
        /// </summary>
        public async Task<string?> GetConfigValueAsync(string key)
        {
            try
            {
                _logger.LogInformation("获取配置值: {Key}", key);
                var config = await _configRepository.GetByKeyAsync(key);
                return config?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取配置值失败: {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// 根据键获取配置值（带默认值）
        /// </summary>
        public async Task<string> GetConfigValueAsync(string key, string defaultValue)
        {
            var value = await GetConfigValueAsync(key);
            return value ?? defaultValue;
        }

        /// <summary>
        /// 根据键获取布尔配置值
        /// </summary>
        public async Task<bool> GetBoolConfigAsync(string key, bool defaultValue = false)
        {
            try
            {
                var value = await GetConfigValueAsync(key);
                if (string.IsNullOrEmpty(value))
                    return defaultValue;

                return bool.TryParse(value, out bool result) ? result : defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取布尔配置值失败: {Key}", key);
                return defaultValue;
            }
        }

        /// <summary>
        /// 根据键获取整数配置值
        /// </summary>
        public async Task<int> GetIntConfigAsync(string key, int defaultValue = 0)
        {
            try
            {
                var value = await GetConfigValueAsync(key);
                if (string.IsNullOrEmpty(value))
                    return defaultValue;

                return int.TryParse(value, out int result) ? result : defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取整数配置值失败: {Key}", key);
                return defaultValue;
            }
        }

        /// <summary>
        /// 设置配置值
        /// </summary>
        public async Task<bool> SetConfigValueAsync(string key, string value, string? description = null)
        {
            try
            {
                _logger.LogInformation("设置配置值: {Key} = {Value}", key, value);
                
                var config = new SystemConfig
                {
                    Key = key,
                    Value = value,
                    Description = description,
                    UpdatedTime = DateTime.Now
                };

                return await _configRepository.SetOrUpdateAsync(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置配置值失败: {Key} = {Value}", key, value);
                return false;
            }
        }

        /// <summary>
        /// 设置布尔配置值
        /// </summary>
        public async Task<bool> SetBoolConfigAsync(string key, bool value, string? description = null)
        {
            return await SetConfigValueAsync(key, value.ToString().ToLower(), description);
        }

        /// <summary>
        /// 设置整数配置值
        /// </summary>
        public async Task<bool> SetIntConfigAsync(string key, int value, string? description = null)
        {
            return await SetConfigValueAsync(key, value.ToString(), description);
        }

        /// <summary>
        /// 删除配置
        /// </summary>
        public async Task<bool> DeleteConfigAsync(string key)
        {
            try
            {
                _logger.LogInformation("删除配置: {Key}", key);
                return await _configRepository.DeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除配置失败: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 检查配置是否存在
        /// </summary>
        public async Task<bool> ConfigExistsAsync(string key)
        {
            try
            {
                return await _configRepository.ExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查配置是否存在失败: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 批量更新配置
        /// </summary>
        public async Task<bool> BatchUpdateConfigsAsync(Dictionary<string, string> configs)
        {
            try
            {
                _logger.LogInformation("批量更新配置，数量: {Count}", configs.Count);
                
                foreach (var kvp in configs)
                {
                    await SetConfigValueAsync(kvp.Key, kvp.Value);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新配置失败");
                return false;
            }
        }

        /// <summary>
        /// 获取系统是否启用登录
        /// </summary>
        public async Task<bool> IsLoginEnabledAsync()
        {
            return await GetBoolConfigAsync(SystemConfigKeys.EnableLogin, true);
        }

        /// <summary>
        /// 设置系统是否启用登录
        /// </summary>
        public async Task<bool> SetLoginEnabledAsync(bool enabled)
        {
            return await SetBoolConfigAsync(SystemConfigKeys.EnableLogin, enabled, "是否启用用户登录验证");
        }

        /// <summary>
        /// 获取系统设置
        /// </summary>
        public async Task<SystemSettings> GetSystemSettings()
        {
            try
            {
                _logger.LogInformation("获取系统设置");
                
                var settings = new SystemSettings
                {
                    AutoSaveEnabled = await GetBoolConfigAsync(SystemConfigKeys.AutoSaveEnabled, true),
                    AutoSaveInterval = await GetIntConfigAsync(SystemConfigKeys.AutoSaveInterval, 5),
                    StartupMinimize = await GetBoolConfigAsync(SystemConfigKeys.StartupMinimize, false),
                    CheckForUpdates = await GetBoolConfigAsync(SystemConfigKeys.CheckForUpdates, true),
                    EnableLogging = await GetBoolConfigAsync(SystemConfigKeys.EnableLogging, true),
                    LogLevel = await GetConfigValueAsync(SystemConfigKeys.LogLevel, "Info"),
                    EnableNotifications = await GetBoolConfigAsync(SystemConfigKeys.EnableNotifications, true),
                    EnableLogin = await GetBoolConfigAsync(SystemConfigKeys.EnableLogin, true),
                    Language = await GetConfigValueAsync(SystemConfigKeys.Language, "简体中文"),
                    Theme = await GetConfigValueAsync(SystemConfigKeys.Theme, "深色主题"),
                    EnableAnimations = await GetBoolConfigAsync(SystemConfigKeys.EnableAnimations, true),
                    MaxRecentFiles = await GetIntConfigAsync(SystemConfigKeys.MaxRecentFiles, 10),
                    ConfirmBeforeClose = await GetBoolConfigAsync(SystemConfigKeys.ConfirmBeforeClose, true),
                    EnableBackup = await GetBoolConfigAsync(SystemConfigKeys.EnableBackup, true),
                    BackupRetentionDays = await GetIntConfigAsync(SystemConfigKeys.BackupRetentionDays, 30)
                };

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统设置失败");
                return new SystemSettings(); // 返回默认设置
            }
        }

        /// <summary>
        /// 保存系统设置
        /// </summary>
        public async Task<bool> SaveSystemSettings(SystemSettings settings)
        {
            try
            {
                _logger.LogInformation("保存系统设置");
                
                var configs = new Dictionary<string, string>
                {
                    { SystemConfigKeys.AutoSaveEnabled, settings.AutoSaveEnabled.ToString().ToLower() },
                    { SystemConfigKeys.AutoSaveInterval, settings.AutoSaveInterval.ToString() },
                    { SystemConfigKeys.StartupMinimize, settings.StartupMinimize.ToString().ToLower() },
                    { SystemConfigKeys.CheckForUpdates, settings.CheckForUpdates.ToString().ToLower() },
                    { SystemConfigKeys.EnableLogging, settings.EnableLogging.ToString().ToLower() },
                    { SystemConfigKeys.LogLevel, settings.LogLevel },
                    { SystemConfigKeys.EnableNotifications, settings.EnableNotifications.ToString().ToLower() },
                    { SystemConfigKeys.EnableLogin, settings.EnableLogin.ToString().ToLower() },
                    { SystemConfigKeys.Language, settings.Language },
                    { SystemConfigKeys.Theme, settings.Theme },
                    { SystemConfigKeys.EnableAnimations, settings.EnableAnimations.ToString().ToLower() },
                    { SystemConfigKeys.MaxRecentFiles, settings.MaxRecentFiles.ToString() },
                    { SystemConfigKeys.ConfirmBeforeClose, settings.ConfirmBeforeClose.ToString().ToLower() },
                    { SystemConfigKeys.EnableBackup, settings.EnableBackup.ToString().ToLower() },
                    { SystemConfigKeys.BackupRetentionDays, settings.BackupRetentionDays.ToString() }
                };

                return await BatchUpdateConfigsAsync(configs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存系统设置失败");
                return false;
            }
        }

        /// <summary>
        /// 重置到默认设置
        /// </summary>
        public async Task<bool> ResetToDefaults()
        {
            try
            {
                _logger.LogInformation("重置系统设置到默认值");
                
                var defaultSettings = new SystemSettings();
                return await SaveSystemSettings(defaultSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置系统设置失败");
                return false;
            }
        }
    }
} 