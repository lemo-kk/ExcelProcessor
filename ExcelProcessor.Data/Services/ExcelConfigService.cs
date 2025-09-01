using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Data.Database;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Services
{
    public class ExcelConfigService : IExcelConfigService
    {
        private readonly IDbContext _dbContext;
        private readonly ILogger<ExcelConfigService> _logger;

        public ExcelConfigService(IDbContext dbContext, ILogger<ExcelConfigService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> SaveConfigAsync(ExcelConfig config)
        {
            try
            {
                _logger.LogInformation($"开始保存配置: {config.ConfigName}");
                _logger.LogInformation($"配置详情: FilePath={config.FilePath}, TargetDataSource={config.TargetDataSource}, SheetName={config.SheetName}, HeaderRow={config.HeaderRow}");

                // 生成GUID格式的ID
                config.Id = Guid.NewGuid().ToString();

                var sql = @"
                    INSERT INTO ExcelConfigs (Id, ConfigName, Description, FilePath, TargetDataSourceId, TargetDataSourceName, TargetTableName, SheetName, HeaderRow, DataStartRow, MaxRows, SkipEmptyRows, SplitEachRow, ClearTableDataBeforeImport, EnableValidation, EnableTransaction, ErrorHandlingStrategy, Status, CreatedAt, UpdatedAt)
                    VALUES (@Id, @ConfigName, @Description, @FilePath, @TargetDataSourceId, @TargetDataSourceName, @TargetTableName, @SheetName, @HeaderRow, @DataStartRow, @MaxRows, @SkipEmptyRows, @SplitEachRow, @ClearTableDataBeforeImport, @EnableValidation, @EnableTransaction, @ErrorHandlingStrategy, @Status, @CreatedAt, @UpdatedAt)";

                var parameters = new
                {
                    config.Id,
                    config.ConfigName,
                    Description = config.Description ?? "",
                    config.FilePath,
                    TargetDataSourceId = !string.IsNullOrWhiteSpace(config.TargetDataSourceId) ? config.TargetDataSourceId : "default", // 使用string类型
                    TargetDataSourceName = config.TargetDataSourceName ?? config.TargetDataSource ?? "默认数据源",
                    TargetTableName = config.TargetTableName ?? "ImportedData",
                    config.SheetName,
                    config.HeaderRow,
                    DataStartRow = config.DataStartRow > 0 ? config.DataStartRow : 2,
                    MaxRows = config.MaxRows > 0 ? config.MaxRows : 0,
                    SkipEmptyRows = config.SkipEmptyRows ? 1 : 0,
                    SplitEachRow = config.SplitEachRow ? 1 : 0,
                    ClearTableDataBeforeImport = config.ClearTableDataBeforeImport ? 1 : 0,
                    EnableValidation = config.EnableValidation ? 1 : 0,
                    EnableTransaction = config.EnableTransaction ? 1 : 0,
                    ErrorHandlingStrategy = config.ErrorHandlingStrategy ?? "Log",
                    Status = config.Status ?? "Active",
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                _logger.LogInformation($"执行SQL: {sql}");
                _logger.LogInformation($"参数: ConfigName={parameters.ConfigName}, FilePath={parameters.FilePath}, TargetDataSourceName={parameters.TargetDataSourceName}");

                using var connection = _dbContext.GetConnection();
                var result = await connection.ExecuteAsync(sql, parameters);
                
                _logger.LogInformation($"配置 '{config.ConfigName}' 保存成功，影响行数: {result}");
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"保存配置 '{config.ConfigName}' 时出错");
                return false;
            }
        }

        public async Task<List<ExcelConfig>> GetAllConfigsAsync()
        {
            try
            {
                var sql = "SELECT Id, ConfigName, Description, FilePath, TargetDataSourceId, TargetDataSourceName, TargetTableName, SheetName, HeaderRow, DataStartRow, MaxRows, SkipEmptyRows, SplitEachRow, ClearTableDataBeforeImport, EnableValidation, EnableTransaction, ErrorHandlingStrategy, Status, CreatedAt, UpdatedAt, Remarks FROM ExcelConfigs ORDER BY CreatedAt DESC";
                
                using var connection = _dbContext.GetConnection();
                var configs = await connection.QueryAsync<ExcelConfig>(sql);
                
                return configs.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有配置时出错");
                return new List<ExcelConfig>();
            }
        }

        public async Task<ExcelConfig> GetConfigByIdAsync(string id)
        {
            try
            {
                var sql = "SELECT Id, ConfigName, Description, FilePath, TargetDataSourceId, TargetDataSourceName, TargetTableName, SheetName, HeaderRow, DataStartRow, MaxRows, SkipEmptyRows, SplitEachRow, ClearTableDataBeforeImport, EnableValidation, EnableTransaction, ErrorHandlingStrategy, Status, CreatedAt, UpdatedAt, Remarks FROM ExcelConfigs WHERE Id = @Id";
                
                using var connection = _dbContext.GetConnection();
                var config = await connection.QueryFirstOrDefaultAsync<ExcelConfig>(sql, new { Id = id });
                
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取配置 ID '{id}' 时出错");
                return null;
            }
        }

        public async Task<ExcelConfig> GetConfigByNameAsync(string configName)
        {
            try
            {
                var sql = "SELECT Id, ConfigName, Description, FilePath, TargetDataSourceId, TargetDataSourceName, TargetTableName, SheetName, HeaderRow, DataStartRow, MaxRows, SkipEmptyRows, SplitEachRow, ClearTableDataBeforeImport, EnableValidation, EnableTransaction, ErrorHandlingStrategy, Status, CreatedAt, UpdatedAt, Remarks FROM ExcelConfigs WHERE ConfigName = @ConfigName";
                
                using var connection = _dbContext.GetConnection();
                var config = await connection.QueryFirstOrDefaultAsync<ExcelConfig>(sql, new { ConfigName = configName });
                
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取配置名称 '{configName}' 时出错");
                return null;
            }
        }

        public async Task<bool> DeleteConfigAsync(string configName)
        {
            try
            {
                _logger.LogInformation($"开始删除配置: {configName}");
                
                // 首先检查配置是否存在
                var existingConfig = await GetConfigByIdAsync(configName);
                if (existingConfig == null)
                {
                    _logger.LogWarning($"配置 '{configName}' 不存在，无法删除");
                    return false;
                }
                
                _logger.LogInformation($"找到配置，ID: {existingConfig.Id}");
                
                var sql = "DELETE FROM ExcelConfigs WHERE ConfigName = @ConfigName";
                
                using var connection = _dbContext.GetConnection();
                _logger.LogInformation("数据库连接已建立，准备执行删除操作");
                
                var result = await connection.ExecuteAsync(sql, new { ConfigName = configName });
                
                _logger.LogInformation($"配置 '{configName}' 删除操作完成，影响行数: {result}");
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除配置 '{configName}' 时出错: {ex.Message}");
                _logger.LogError($"异常堆栈: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> UpdateConfigAsync(ExcelConfig config)
        {
            try
            {
                var sql = @"
                    UPDATE ExcelConfigs 
                    SET FilePath = @FilePath, TargetDataSourceName = @TargetDataSourceName, SheetName = @SheetName, 
                        HeaderRow = @HeaderRow, SkipEmptyRows = @SkipEmptyRows, SplitEachRow = @SplitEachRow, 
                        ClearTableDataBeforeImport = @ClearTableDataBeforeImport, Status = @Status, UpdatedAt = @UpdatedAt
                    WHERE ConfigName = @ConfigName";

                var parameters = new
                {
                    config.ConfigName,
                    config.FilePath,
                    TargetDataSourceName = config.TargetDataSourceName,
                    config.SheetName,
                    config.HeaderRow,
                    SkipEmptyRows = config.SkipEmptyRows ? 1 : 0,
                    SplitEachRow = config.SplitEachRow ? 1 : 0,
                    ClearTableDataBeforeImport = config.ClearTableDataBeforeImport ? 1 : 0,
                    Status = "Active",
                    UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                using var connection = _dbContext.GetConnection();
                var result = await connection.ExecuteAsync(sql, parameters);
                
                _logger.LogInformation($"配置 '{config.ConfigName}' 更新成功");
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新配置 '{config.ConfigName}' 时出错");
                return false;
            }
        }
    }
} 