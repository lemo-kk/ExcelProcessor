using ExcelProcessor.Data.Database;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;
using System.Data.SQLite;
using ExcelProcessor.Core.Repositories;
using Dapper;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// Excel字段映射仓储实现
    /// </summary>
    public class ExcelFieldMappingRepository : BaseRepository<ExcelFieldMapping>, IExcelFieldMappingRepository
    {
        public ExcelFieldMappingRepository(IDbContext dbContext, ILogger<ExcelFieldMappingRepository> logger) : base(dbContext, logger)
        {
        }

        protected override string GetTableName()
        {
            return "ExcelFieldMappings";
        }

        /// <summary>
        /// 根据Excel配置ID获取字段映射
        /// </summary>
        /// <param name="excelConfigId">Excel配置ID</param>
        /// <returns>字段映射列表</returns>
        public async Task<IEnumerable<ExcelFieldMapping>> GetByExcelConfigIdAsync(int excelConfigId)
        {
            try
            {
                using var connection = _dbContext.GetConnection();
                var sql = $"SELECT * FROM {GetTableName()} WHERE ExcelConfigId = @ExcelConfigId AND IsEnabled = 1 ORDER BY SortOrder";
                var parameters = new { ExcelConfigId = excelConfigId };

                return await connection.QueryAsync<ExcelFieldMapping>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据Excel配置ID获取字段映射失败: ExcelConfigId={ExcelConfigId}", excelConfigId);
                throw;
            }
        }

        /// <summary>
        /// 根据Excel配置ID获取字段映射（字符串ID版本）
        /// </summary>
        /// <param name="excelConfigId">Excel配置ID</param>
        /// <returns>字段映射列表</returns>
        public async Task<IEnumerable<ExcelFieldMapping>> GetByExcelConfigIdAsync(string excelConfigId)
        {
            try
            {
                using var connection = _dbContext.GetConnection();
                var sql = $"SELECT * FROM {GetTableName()} WHERE ExcelConfigId = @ExcelConfigId AND IsEnabled = 1 ORDER BY SortOrder";
                var parameters = new { ExcelConfigId = excelConfigId };

                return await connection.QueryAsync<ExcelFieldMapping>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据Excel配置ID获取字段映射失败: ExcelConfigId={ExcelConfigId}", excelConfigId);
                throw;
            }
        }

        /// <summary>
        /// 删除Excel配置的所有字段映射
        /// </summary>
        /// <param name="excelConfigId">Excel配置ID</param>
        /// <returns>是否成功</returns>
        public async Task<bool> DeleteByExcelConfigIdAsync(int excelConfigId)
        {
            try
            {
                using var connection = _dbContext.GetConnection();
                var sql = $"DELETE FROM {GetTableName()} WHERE ExcelConfigId = @ExcelConfigId";
                var parameters = new { ExcelConfigId = excelConfigId };

                var result = await connection.ExecuteAsync(sql, parameters);
                _logger.LogInformation("删除Excel配置的字段映射成功: ExcelConfigId={ExcelConfigId}", excelConfigId);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除Excel配置的字段映射失败: ExcelConfigId={ExcelConfigId}", excelConfigId);
                throw;
            }
        }

        /// <summary>
        /// 删除Excel配置的所有字段映射（字符串ID版本）
        /// </summary>
        /// <param name="excelConfigId">Excel配置ID</param>
        /// <returns>是否成功</returns>
        public async Task<bool> DeleteByExcelConfigIdAsync(string excelConfigId)
        {
            try
            {
                using var connection = _dbContext.GetConnection();
                var sql = $"DELETE FROM {GetTableName()} WHERE ExcelConfigId = @ExcelConfigId";
                var parameters = new { ExcelConfigId = excelConfigId };

                var result = await connection.ExecuteAsync(sql, parameters);
                _logger.LogInformation("删除Excel配置的字段映射成功: ExcelConfigId={ExcelConfigId}", excelConfigId);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除Excel配置的字段映射失败: ExcelConfigId={ExcelConfigId}", excelConfigId);
                throw;
            }
        }

        /// <summary>
        /// 批量保存字段映射
        /// </summary>
        /// <param name="mappings">字段映射列表</param>
        /// <returns>是否成功</returns>
        public async Task<bool> SaveMappingsAsync(IEnumerable<ExcelFieldMapping> mappings)
        {
            try
            {
                using var connection = _dbContext.GetConnection();
                using var transaction = connection.BeginTransaction();

                try
                {
                    foreach (var mapping in mappings)
                    {
                        if (mapping.Id == 0)
                        {
                            // 新增 - 使用统一连接和事务
                            await AddAsync(mapping, connection, transaction);
                        }
                        else
                        {
                            // 更新 - 使用统一连接和事务
                            await UpdateAsync(mapping, connection, transaction);
                        }
                    }

                    transaction.Commit();
                    _logger.LogInformation("批量保存字段映射成功: 数量={Count}", mappings.Count());
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量保存字段映射失败");
                throw;
            }
        }
    }
} 