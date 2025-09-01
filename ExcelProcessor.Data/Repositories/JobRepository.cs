using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using ExcelProcessor.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// 作业配置数据访问层实现
    /// </summary>
    public class JobRepository : IJobRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<JobRepository> _logger;
        private readonly IJobStepRepository _jobStepRepository;

        public JobRepository(IDbConnection connection, ILogger<JobRepository> logger, IJobStepRepository jobStepRepository)
        {
            _connection = connection;
            _logger = logger;
            _jobStepRepository = jobStepRepository;
        }

        public async Task<List<JobConfig>> GetAllAsync()
        {
            try
            {
                const string sql = @"
                    SELECT Id, Name, Description, Type, Category, Status, Priority, 
                           IsEnabled, ExecutionMode, CronExpression, TimeoutSeconds, 
                           MaxRetryCount, RetryIntervalSeconds, AllowParallelExecution,
                           InputParameters, OutputConfig, NotificationConfig,
                           CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, Remarks
                    FROM JobConfigs 
                    ORDER BY CreatedAt DESC";

                var jobs = await _connection.QueryAsync<JobConfig>(sql);
                return jobs.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有作业配置失败");
                throw;
            }
        }

        public async Task<JobConfig?> GetByIdAsync(string id)
        {
            try
            {
                const string sql = @"
                    SELECT Id, Name, Description, Type, Category, Status, Priority, 
                           IsEnabled, ExecutionMode, CronExpression, TimeoutSeconds, 
                           MaxRetryCount, RetryIntervalSeconds, AllowParallelExecution,
                           InputParameters, OutputConfig, NotificationConfig,
                           CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, Remarks
                    FROM JobConfigs 
                    WHERE Id = @Id";

                var job = await _connection.QueryFirstOrDefaultAsync<JobConfig>(sql, new { Id = id });
                
                if (job != null)
                {
                    // 从数据库表获取作业步骤
                    job.Steps = await _jobStepRepository.GetByJobIdAsync(id);
                    
                    // 处理输入参数（如果存在）
                    if (!string.IsNullOrEmpty(job.InputParameters))
                    {
                        try
                        {
                            job.Parameters = JsonSerializer.Deserialize<List<JobParameter>>(job.InputParameters) ?? new List<JobParameter>();
                        }
                        catch
                        {
                            job.Parameters = new List<JobParameter>();
                        }
                    }
                }

                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取作业配置失败，Id: {Id}", id);
                throw;
            }
        }

        public async Task<JobConfig?> GetByNameAsync(string name)
        {
            try
            {
                const string sql = @"
                    SELECT Id, Name, Description, Type, Category, Status, Priority, 
                           IsEnabled, ExecutionMode, CronExpression, TimeoutSeconds, 
                           MaxRetryCount, RetryIntervalSeconds, AllowParallelExecution,
                           InputParameters, OutputConfig, NotificationConfig,
                           CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, Remarks
                    FROM JobConfigs 
                    WHERE Name = @Name";

                var parameters = new { Name = name };
                return await _connection.QueryFirstOrDefaultAsync<JobConfig>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据名称获取作业配置失败: {JobName}", name);
                throw;
            }
        }

        public async Task<List<JobConfig>> GetByTypeAsync(string type)
        {
            try
            {
                const string sql = @"
                    SELECT Id, Name, Description, Type, Category, Status, Priority, 
                           IsEnabled, ExecutionMode, CronExpression, TimeoutSeconds, 
                           MaxRetryCount, RetryIntervalSeconds, AllowParallelExecution,
                           InputParameters, OutputConfig, NotificationConfig,
                           CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, Remarks
                    FROM JobConfigs 
                    WHERE Type = @Type
                    ORDER BY CreatedAt DESC";

                var parameters = new { Type = type };
                var jobs = await _connection.QueryAsync<JobConfig>(sql, parameters);
                return jobs.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据类型获取作业配置失败: {JobType}", type);
                throw;
            }
        }

        public async Task<List<JobConfig>> GetByCategoryAsync(string category)
        {
            try
            {
                const string sql = @"
                    SELECT Id, Name, Description, Type, Category, Status, Priority, 
                           IsEnabled, ExecutionMode, CronExpression, TimeoutSeconds, 
                           MaxRetryCount, RetryIntervalSeconds, AllowParallelExecution,
                           InputParameters, OutputConfig, NotificationConfig,
                           CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, Remarks
                    FROM JobConfigs 
                    WHERE Category = @Category
                    ORDER BY CreatedAt DESC";

                var parameters = new { Category = category };
                var jobs = await _connection.QueryAsync<JobConfig>(sql, parameters);
                return jobs.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据分类获取作业配置失败: {Category}", category);
                throw;
            }
        }

        public async Task<List<JobConfig>> GetByStatusAsync(JobStatus status)
        {
            try
            {
                const string sql = @"
                    SELECT Id, Name, Description, Type, Category, Status, Priority, 
                           IsEnabled, ExecutionMode, CronExpression, TimeoutSeconds, 
                           MaxRetryCount, RetryIntervalSeconds, AllowParallelExecution,
                           InputParameters, OutputConfig, NotificationConfig,
                           CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, Remarks
                    FROM JobConfigs 
                    WHERE Status = @Status
                    ORDER BY CreatedAt DESC";

                var parameters = new { Status = status };
                var jobs = await _connection.QueryAsync<JobConfig>(sql, parameters);
                return jobs.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据状态获取作业配置失败: {Status}", status);
                throw;
            }
        }

        public async Task<List<JobConfig>> SearchAsync(string keyword)
        {
            try
            {
                const string sql = @"
                    SELECT Id, Name, Description, Type, Category, Status, Priority, 
                           IsEnabled, ExecutionMode, CronExpression, TimeoutSeconds, 
                           MaxRetryCount, RetryIntervalSeconds, AllowParallelExecution,
                           InputParameters, OutputConfig, NotificationConfig,
                           CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, Remarks
                    FROM JobConfigs 
                    WHERE Name LIKE @Keyword OR Description LIKE @Keyword OR Type LIKE @Keyword OR Category LIKE @Keyword
                    ORDER BY CreatedAt DESC";

                var parameters = new { Keyword = $"%{keyword}%" };
                var jobs = await _connection.QueryAsync<JobConfig>(sql, parameters);
                return jobs.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索作业配置失败: {Keyword}", keyword);
                throw;
            }
        }

        public async Task<bool> CreateAsync(JobConfig jobConfig)
        {
            try
            {
                const string sql = @"
                    INSERT INTO JobConfigs (
                        Id, Name, Description, Type, Category, Status, Priority, 
                        IsEnabled, ExecutionMode, CronExpression, TimeoutSeconds, 
                        MaxRetryCount, RetryIntervalSeconds, AllowParallelExecution,
                        InputParameters, OutputConfig, NotificationConfig,
                        CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, Remarks
                    ) VALUES (
                        @Id, @Name, @Description, @Type, @Category, @Status, @Priority, 
                        @IsEnabled, @ExecutionMode, @CronExpression, @TimeoutSeconds, 
                        @MaxRetryCount, @RetryIntervalSeconds, @AllowParallelExecution,
                        @InputParameters, @OutputConfig, @NotificationConfig,
                        @CreatedAt, @UpdatedAt, @CreatedBy, @UpdatedBy, @Remarks
                    )";

                var affectedRows = await _connection.ExecuteAsync(sql, jobConfig);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建作业配置失败: {JobName}", jobConfig.Name);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(JobConfig jobConfig)
        {
            try
            {
                const string sql = @"
                    UPDATE JobConfigs SET 
                        Name = @Name, Description = @Description, Type = @Type, 
                        Category = @Category, Status = @Status, Priority = @Priority, 
                        IsEnabled = @IsEnabled, ExecutionMode = @ExecutionMode, 
                        CronExpression = @CronExpression, TimeoutSeconds = @TimeoutSeconds, 
                        MaxRetryCount = @MaxRetryCount, RetryIntervalSeconds = @RetryIntervalSeconds, 
                        AllowParallelExecution = @AllowParallelExecution, 
                        InputParameters = @InputParameters, OutputConfig = @OutputConfig, 
                        NotificationConfig = @NotificationConfig, UpdatedAt = @UpdatedAt, 
                        UpdatedBy = @UpdatedBy, Remarks = @Remarks
                    WHERE Id = @Id";

                var affectedRows = await _connection.ExecuteAsync(sql, jobConfig);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新作业配置失败: {JobId}", jobConfig.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                const string sql = "DELETE FROM JobConfigs WHERE Id = @Id";
                var parameters = new { Id = id };
                var affectedRows = await _connection.ExecuteAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除作业配置失败: {JobId}", id);
                throw;
            }
        }

        public async Task<bool> BatchDeleteAsync(List<string> ids)
        {
            try
            {
                const string sql = "DELETE FROM JobConfigs WHERE Id IN @Ids";
                var parameters = new { Ids = ids };
                var affectedRows = await _connection.ExecuteAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除作业配置失败: {JobIds}", string.Join(", ", ids));
                throw;
            }
        }

        public async Task<bool> ExistsByNameAsync(string name, string? excludeId = null)
        {
            try
            {
                string sql;
                object parameters;

                if (string.IsNullOrEmpty(excludeId))
                {
                    sql = "SELECT COUNT(1) FROM JobConfigs WHERE Name = @Name";
                    parameters = new { Name = name };
                }
                else
                {
                    sql = "SELECT COUNT(1) FROM JobConfigs WHERE Name = @Name AND Id != @ExcludeId";
                    parameters = new { Name = name, ExcludeId = excludeId };
                }

                var count = await _connection.ExecuteScalarAsync<int>(sql, parameters);
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查作业名称是否存在失败: {JobName}", name);
                throw;
            }
        }

        public async Task<int> GetCountAsync()
        {
            try
            {
                const string sql = "SELECT COUNT(1) FROM JobConfigs";
                return await _connection.ExecuteScalarAsync<int>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取作业数量失败");
                throw;
            }
        }

        public async Task<int> GetEnabledCountAsync()
        {
            try
            {
                const string sql = "SELECT COUNT(1) FROM JobConfigs WHERE IsEnabled = 1";
                return await _connection.ExecuteScalarAsync<int>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取启用的作业数量失败");
                throw;
            }
        }
    }
} 