using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ExcelProcessor.Core.Repositories;
using ExcelProcessor.Data.Database;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// 系统配置仓储实现
    /// </summary>
    public class SystemConfigRepository : ISystemConfigRepository
    {
        private readonly IDbContext _dbContext;
        private readonly ILogger<SystemConfigRepository> _logger;

        public SystemConfigRepository(IDbContext dbContext, ILogger<SystemConfigRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有配置
        /// </summary>
        public async Task<IEnumerable<SystemConfig>> GetAllAsync()
        {
            try
            {
                using var connection = _dbContext.GetConnection();
                var sql = "SELECT Key, Value, Description, UpdatedTime FROM SystemConfig";
                return await connection.QueryAsync<SystemConfig>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有系统配置失败");
                return new List<SystemConfig>();
            }
        }

        /// <summary>
        /// 根据键获取配置
        /// </summary>
        public async Task<SystemConfig?> GetByKeyAsync(string key)
        {
            try
            {
                using var connection = _dbContext.GetConnection();
                var sql = "SELECT Key, Value, Description, UpdatedTime FROM SystemConfig WHERE Key = @Key";
                return await connection.QueryFirstOrDefaultAsync<SystemConfig>(sql, new { Key = key });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据键获取系统配置失败: {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// 添加配置
        /// </summary>
        public async Task<bool> AddAsync(SystemConfig config)
        {
            try
            {
                using var connection = _dbContext.GetConnection();
                var sql = @"
                    INSERT INTO SystemConfig (Key, Value, Description, UpdatedTime)
                    VALUES (@Key, @Value, @Description, @UpdatedTime)";
                
                var result = await connection.ExecuteAsync(sql, new
                {
                    config.Key,
                    config.Value,
                    config.Description,
                    UpdatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
                
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加系统配置失败: {Key}", config.Key);
                return false;
            }
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        public async Task<bool> UpdateAsync(SystemConfig config)
        {
            try
            {
                using var connection = _dbContext.GetConnection();
                var sql = @"
                    UPDATE SystemConfig 
                    SET Value = @Value, Description = @Description, UpdatedTime = @UpdatedTime
                    WHERE Key = @Key";
                
                var result = await connection.ExecuteAsync(sql, new
                {
                    config.Key,
                    config.Value,
                    config.Description,
                    UpdatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
                
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新系统配置失败: {Key}", config.Key);
                return false;
            }
        }

        /// <summary>
        /// 删除配置
        /// </summary>
        public async Task<bool> DeleteAsync(string key)
        {
            try
            {
                using var connection = _dbContext.GetConnection();
                var sql = "DELETE FROM SystemConfig WHERE Key = @Key";
                var result = await connection.ExecuteAsync(sql, new { Key = key });
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除系统配置失败: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 检查配置是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                using var connection = _dbContext.GetConnection();
                var sql = "SELECT COUNT(1) FROM SystemConfig WHERE Key = @Key";
                var count = await connection.ExecuteScalarAsync<int>(sql, new { Key = key });
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查系统配置是否存在失败: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 设置或更新配置
        /// </summary>
        public async Task<bool> SetOrUpdateAsync(SystemConfig config)
        {
            try
            {
                var exists = await ExistsAsync(config.Key);
                if (exists)
                {
                    return await UpdateAsync(config);
                }
                else
                {
                    return await AddAsync(config);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置或更新系统配置失败: {Key}", config.Key);
                return false;
            }
        }
    }
} 