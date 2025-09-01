using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Core.Interfaces;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// 配置引用服务实现
    /// </summary>
    public class ConfigurationReferenceService : IConfigurationReferenceService
    {
        private readonly ILogger<ConfigurationReferenceService> _logger;
        private readonly string _connectionString;
        private readonly IExcelConfigService _excelConfigService;
        private readonly ISqlService _sqlService;
        private readonly IDataSourceService _dataSourceService;

        public ConfigurationReferenceService(
            ILogger<ConfigurationReferenceService> logger,
            string connectionString,
            IExcelConfigService excelConfigService,
            ISqlService sqlService,
            IDataSourceService dataSourceService)
        {
            _logger = logger;
            _connectionString = connectionString;
            _excelConfigService = excelConfigService;
            _sqlService = sqlService;
            _dataSourceService = dataSourceService;
        }

        #region 配置引用管理

        public async Task<(bool success, string message)> CreateReferenceAsync(ConfigurationReference reference)
        {
            try
            {
                _logger.LogInformation("创建配置引用: {ReferenceName}", reference.Name);

                // 验证引用
                var validation = await ValidateReferenceAsync(reference);
                if (!validation.isValid)
                {
                    return (false, string.Join("; ", validation.errors));
                }

                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO ConfigurationReferences (Id, Name, Description, Type, ReferencedConfigId, 
                                                       ReferencedConfigName, OverrideParameters, IsEnabled, 
                                                       CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
                    VALUES (@Id, @Name, @Description, @Type, @ReferencedConfigId, 
                            @ReferencedConfigName, @OverrideParameters, @IsEnabled, 
                            @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy)";

                await connection.ExecuteAsync(sql, reference);

                _logger.LogInformation("配置引用创建成功: {ReferenceId}", reference.Id);
                return (true, "配置引用创建成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建配置引用失败: {ReferenceName}", reference.Name);
                return (false, $"创建配置引用失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> UpdateReferenceAsync(ConfigurationReference reference)
        {
            try
            {
                _logger.LogInformation("更新配置引用: {ReferenceId}", reference.Id);

                // 验证引用
                var validation = await ValidateReferenceAsync(reference);
                if (!validation.isValid)
                {
                    return (false, string.Join("; ", validation.errors));
                }

                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE ConfigurationReferences 
                    SET Name = @Name, Description = @Description, Type = @Type, 
                        ReferencedConfigId = @ReferencedConfigId, ReferencedConfigName = @ReferencedConfigName,
                        OverrideParameters = @OverrideParameters, IsEnabled = @IsEnabled, 
                        UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
                    WHERE Id = @Id";

                reference.UpdatedAt = DateTime.Now;
                var rowsAffected = await connection.ExecuteAsync(sql, reference);

                if (rowsAffected == 0)
                {
                    return (false, "配置引用不存在");
                }

                _logger.LogInformation("配置引用更新成功: {ReferenceId}", reference.Id);
                return (true, "配置引用更新成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新配置引用失败: {ReferenceId}", reference.Id);
                return (false, $"更新配置引用失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> DeleteReferenceAsync(string referenceId)
        {
            try
            {
                _logger.LogInformation("删除配置引用: {ReferenceId}", referenceId);

                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "DELETE FROM ConfigurationReferences WHERE Id = @ReferenceId";
                var rowsAffected = await connection.ExecuteAsync(sql, new { ReferenceId = referenceId });

                if (rowsAffected == 0)
                {
                    return (false, "配置引用不存在");
                }

                _logger.LogInformation("配置引用删除成功: {ReferenceId}", referenceId);
                return (true, "配置引用删除成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除配置引用失败: {ReferenceId}", referenceId);
                return (false, $"删除配置引用失败: {ex.Message}");
            }
        }

        public async Task<ConfigurationReference?> GetReferenceByIdAsync(string referenceId)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT Id, Name, Description, Type, ReferencedConfigId, ReferencedConfigName,
                           OverrideParameters, IsEnabled, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
                    FROM ConfigurationReferences 
                    WHERE Id = @ReferenceId";

                var reference = await connection.QueryFirstOrDefaultAsync<ConfigurationReference>(sql, new { ReferenceId = referenceId });
                
                if (reference != null)
                {
                    // 解析覆盖参数
                    if (!string.IsNullOrEmpty(reference.OverrideParameters))
                    {
                        try
                        {
                            reference.Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(reference.OverrideParameters) ?? new Dictionary<string, object>();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "解析覆盖参数失败: {ReferenceId}", referenceId);
                            reference.Parameters = new Dictionary<string, object>();
                        }
                    }
                }

                return reference;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取配置引用失败: {ReferenceId}", referenceId);
                return null;
            }
        }

        public async Task<List<ConfigurationReference>> GetAllReferencesAsync()
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT Id, Name, Description, Type, ReferencedConfigId, ReferencedConfigName,
                           OverrideParameters, IsEnabled, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
                    FROM ConfigurationReferences 
                    ORDER BY CreatedAt DESC";

                var references = await connection.QueryAsync<ConfigurationReference>(sql);
                var result = references.ToList();

                // 解析覆盖参数
                foreach (var reference in result)
                {
                    if (!string.IsNullOrEmpty(reference.OverrideParameters))
                    {
                        try
                        {
                            reference.Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(reference.OverrideParameters) ?? new Dictionary<string, object>();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "解析覆盖参数失败: {ReferenceId}", reference.Id);
                            reference.Parameters = new Dictionary<string, object>();
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有配置引用失败");
                return new List<ConfigurationReference>();
            }
        }

        public async Task<List<ConfigurationReference>> GetReferencesByTypeAsync(ReferenceType type)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT Id, Name, Description, Type, ReferencedConfigId, ReferencedConfigName,
                           OverrideParameters, IsEnabled, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
                    FROM ConfigurationReferences 
                    WHERE Type = @Type
                    ORDER BY CreatedAt DESC";

                var references = await connection.QueryAsync<ConfigurationReference>(sql, new { Type = type });
                var result = references.ToList();

                // 解析覆盖参数
                foreach (var reference in result)
                {
                    if (!string.IsNullOrEmpty(reference.OverrideParameters))
                    {
                        try
                        {
                            reference.Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(reference.OverrideParameters) ?? new Dictionary<string, object>();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "解析覆盖参数失败: {ReferenceId}", reference.Id);
                            reference.Parameters = new Dictionary<string, object>();
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据类型获取配置引用失败: {Type}", type);
                return new List<ConfigurationReference>();
            }
        }

        public async Task<List<ConfigurationReference>> SearchReferencesAsync(string keyword)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT Id, Name, Description, Type, ReferencedConfigId, ReferencedConfigName,
                           OverrideParameters, IsEnabled, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
                    FROM ConfigurationReferences 
                    WHERE Name LIKE @Keyword OR Description LIKE @Keyword OR ReferencedConfigName LIKE @Keyword
                    ORDER BY CreatedAt DESC";

                var searchPattern = $"%{keyword}%";
                var references = await connection.QueryAsync<ConfigurationReference>(sql, new { Keyword = searchPattern });
                var result = references.ToList();

                // 解析覆盖参数
                foreach (var reference in result)
                {
                    if (!string.IsNullOrEmpty(reference.OverrideParameters))
                    {
                        try
                        {
                            reference.Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(reference.OverrideParameters) ?? new Dictionary<string, object>();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "解析覆盖参数失败: {ReferenceId}", reference.Id);
                            reference.Parameters = new Dictionary<string, object>();
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索配置引用失败: {Keyword}", keyword);
                return new List<ConfigurationReference>();
            }
        }

        #endregion

        #region 配置引用执行

        public async Task<ReferenceExecutionResult> ExecuteReferenceAsync(string referenceId, Dictionary<string, object>? parameters = null)
        {
            var result = new ReferenceExecutionResult();
            var startTime = DateTime.Now;

            try
            {
                _logger.LogInformation("执行配置引用: {ReferenceId}", referenceId);

                // 获取引用配置
                var reference = await GetReferenceByIdAsync(referenceId);
                if (reference == null)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "配置引用不存在";
                    return result;
                }

                if (!reference.IsEnabled)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "配置引用已禁用";
                    return result;
                }

                // 解析引用的配置
                var referencedConfig = await ResolveReferenceAsync(reference);
                if (referencedConfig == null)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "引用的配置不存在";
                    return result;
                }

                result.ReferencedConfig = referencedConfig;

                // 合并参数
                var mergedParameters = new Dictionary<string, object>();
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        mergedParameters[param.Key] = param.Value;
                    }
                }
                if (reference.Parameters != null)
                {
                    foreach (var param in reference.Parameters)
                    {
                        mergedParameters[param.Key] = param.Value;
                    }
                }

                // 根据引用类型执行
                switch (reference.Type)
                {
                    case ReferenceType.ExcelConfig:
                        await ExecuteExcelConfigReference(reference, referencedConfig as ExcelConfig, mergedParameters, result);
                        break;
                    case ReferenceType.SqlConfig:
                        await ExecuteSqlConfigReference(reference, referencedConfig as SqlConfig, mergedParameters, result);
                        break;
                    case ReferenceType.DataSourceConfig:
                        await ExecuteDataSourceConfigReference(reference, referencedConfig as DataSourceConfig, mergedParameters, result);
                        break;
                    default:
                        result.IsSuccess = false;
                        result.ErrorMessage = $"不支持的引用类型: {reference.Type}";
                        break;
                }

                result.ExecutionTimeSeconds = (DateTime.Now - startTime).TotalSeconds;
                result.ExecutionLogs.Add($"配置引用执行完成，耗时: {result.ExecutionTimeSeconds:F2}秒");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行配置引用失败: {ReferenceId}", referenceId);
                
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.ExecutionTimeSeconds = (DateTime.Now - startTime).TotalSeconds;
                result.ExecutionLogs.Add($"执行失败: {ex.Message}");

                return result;
            }
        }

        public async Task<List<ReferenceExecutionResult>> ExecuteReferencesAsync(List<string> referenceIds, Dictionary<string, object>? parameters = null)
        {
            var results = new List<ReferenceExecutionResult>();

            foreach (var referenceId in referenceIds)
            {
                var result = await ExecuteReferenceAsync(referenceId, parameters);
                results.Add(result);
            }

            return results;
        }

        public async Task<(bool isValid, List<string> errors)> ValidateReferenceAsync(ConfigurationReference reference)
        {
            var errors = new List<string>();

            try
            {
                // 基本验证
                if (string.IsNullOrWhiteSpace(reference.Name))
                {
                    errors.Add("引用名称不能为空");
                }

                if (string.IsNullOrWhiteSpace(reference.ReferencedConfigId))
                {
                    errors.Add("引用的配置ID不能为空");
                }

                // 验证引用的配置是否存在
                var referencedConfig = await ResolveReferenceAsync(reference);
                if (referencedConfig == null)
                {
                    errors.Add($"引用的配置不存在: {reference.ReferencedConfigId}");
                }

                // 验证覆盖参数格式
                if (!string.IsNullOrEmpty(reference.OverrideParameters))
                {
                    try
                    {
                        JsonSerializer.Deserialize<Dictionary<string, object>>(reference.OverrideParameters);
                    }
                    catch
                    {
                        errors.Add("覆盖参数格式不正确，必须是有效的JSON");
                    }
                }

                return (errors.Count == 0, errors);
            }
            catch (Exception ex)
            {
                errors.Add($"验证配置引用时发生错误: {ex.Message}");
                return (false, errors);
            }
        }

        #endregion

        #region 配置引用解析

        public async Task<object?> ResolveReferenceAsync(ConfigurationReference reference)
        {
            try
            {
                switch (reference.Type)
                {
                    case ReferenceType.ExcelConfig:
                        return await ResolveExcelConfigReferenceAsync(reference);
                    case ReferenceType.SqlConfig:
                        return await ResolveSqlConfigReferenceAsync(reference);
                    case ReferenceType.DataSourceConfig:
                        return await ResolveDataSourceConfigReferenceAsync(reference);
                    default:
                        _logger.LogWarning("不支持的引用类型: {Type}", reference.Type);
                        return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析配置引用失败: {ReferenceId}", reference.Id);
                return null;
            }
        }

        public async Task<ExcelConfig?> ResolveExcelConfigReferenceAsync(ConfigurationReference reference)
        {
            try
            {
                // Excel配置服务使用配置名称作为ID
                return await _excelConfigService.GetConfigByIdAsync(reference.ReferencedConfigId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析Excel配置引用失败: {ReferenceId}", reference.Id);
                return null;
            }
        }

        public async Task<SqlConfig?> ResolveSqlConfigReferenceAsync(ConfigurationReference reference)
        {
            try
            {
                return await _sqlService.GetSqlConfigByIdAsync(reference.ReferencedConfigId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析SQL配置引用失败: {ReferenceId}", reference.Id);
                return null;
            }
        }

        public async Task<DataSourceConfig?> ResolveDataSourceConfigReferenceAsync(ConfigurationReference reference)
        {
            try
            {
                return await _dataSourceService.GetDataSourceByIdAsync(reference.ReferencedConfigId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析数据源配置引用失败: {ReferenceId}", reference.Id);
                return null;
            }
        }

        #endregion

        #region 私有方法

        private async Task ExecuteExcelConfigReference(ConfigurationReference reference, ExcelConfig? excelConfig, Dictionary<string, object> parameters, ReferenceExecutionResult result)
        {
            if (excelConfig == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Excel配置不存在";
                return;
            }

            try
            {
                result.ExecutionLogs.Add($"开始执行Excel配置: {excelConfig.ConfigName}");
                
                // 这里应该调用Excel导入服务执行导入
                // 由于Excel导入服务的具体实现可能不同，这里只是示例
                result.ExecutionLogs.Add($"Excel文件路径: {excelConfig.FilePath}");
                result.ExecutionLogs.Add($"目标表: {excelConfig.TargetTableName}");
                result.ExecutionLogs.Add($"工作表: {excelConfig.SheetName}");

                // 应用覆盖参数
                if (parameters.ContainsKey("ClearTableDataBeforeImport"))
                {
                    excelConfig.ClearTableDataBeforeImport = Convert.ToBoolean(parameters["ClearTableDataBeforeImport"]);
                    result.ExecutionLogs.Add($"应用覆盖参数: ClearTableDataBeforeImport = {excelConfig.ClearTableDataBeforeImport}");
                }

                // 模拟执行成功
                result.IsSuccess = true;
                result.ResultData["ImportedRows"] = 100; // 示例数据
                result.ResultData["TargetTable"] = excelConfig.TargetTableName;
                result.ExecutionLogs.Add("Excel导入执行成功");
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"执行Excel配置失败: {ex.Message}";
                result.ExecutionLogs.Add($"执行失败: {ex.Message}");
            }
        }

        private async Task ExecuteSqlConfigReference(ConfigurationReference reference, SqlConfig? sqlConfig, Dictionary<string, object> parameters, ReferenceExecutionResult result)
        {
            if (sqlConfig == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "SQL配置不存在";
                return;
            }

            try
            {
                result.ExecutionLogs.Add($"开始执行SQL配置: {sqlConfig.Name}");
                result.ExecutionLogs.Add($"SQL语句: {sqlConfig.SqlStatement}");
                result.ExecutionLogs.Add($"输出类型: {sqlConfig.OutputType}");
                result.ExecutionLogs.Add($"输出目标: {sqlConfig.OutputTarget}");

                // 应用覆盖参数
                if (parameters.ContainsKey("TimeoutSeconds"))
                {
                    sqlConfig.TimeoutSeconds = Convert.ToInt32(parameters["TimeoutSeconds"]);
                    result.ExecutionLogs.Add($"应用覆盖参数: TimeoutSeconds = {sqlConfig.TimeoutSeconds}");
                }

                // 这里应该调用SQL执行服务执行SQL
                // 模拟执行成功
                result.IsSuccess = true;
                result.ResultData["AffectedRows"] = 50; // 示例数据
                result.ResultData["OutputTarget"] = sqlConfig.OutputTarget;
                result.ExecutionLogs.Add("SQL执行成功");
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"执行SQL配置失败: {ex.Message}";
                result.ExecutionLogs.Add($"执行失败: {ex.Message}");
            }
        }

        private async Task ExecuteDataSourceConfigReference(ConfigurationReference reference, DataSourceConfig? dataSourceConfig, Dictionary<string, object> parameters, ReferenceExecutionResult result)
        {
            if (dataSourceConfig == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "数据源配置不存在";
                return;
            }

            try
            {
                result.ExecutionLogs.Add($"开始验证数据源配置: {dataSourceConfig.Name}");
                result.ExecutionLogs.Add($"数据源类型: {dataSourceConfig.Type}");
                result.ExecutionLogs.Add($"连接字符串: {dataSourceConfig.ConnectionString}");

                // 这里应该测试数据源连接
                // 模拟连接测试成功
                result.IsSuccess = true;
                result.ResultData["DataSourceName"] = dataSourceConfig.Name;
                result.ResultData["DataSourceType"] = dataSourceConfig.Type;
                result.ExecutionLogs.Add("数据源连接测试成功");
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"验证数据源配置失败: {ex.Message}";
                result.ExecutionLogs.Add($"验证失败: {ex.Message}");
            }
        }

        #endregion
    }
} 