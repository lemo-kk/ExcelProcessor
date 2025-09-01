using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// 作业步骤数据访问层实现
    /// </summary>
    public class JobStepRepository : IJobStepRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<JobStepRepository> _logger;

        public JobStepRepository(IDbConnection connection, ILogger<JobStepRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<List<JobStep>> GetByJobIdAsync(string jobId)
        {
            try
            {
                const string sql = @"
                    SELECT Id, JobId, Name, Description, Type, OrderIndex, IsEnabled, 
                           ExcelConfigId, SqlConfigId, TimeoutSeconds, RetryCount, RetryIntervalSeconds, 
                           ContinueOnFailure, Dependencies, ConditionExpression,
                           CreatedAt, UpdatedAt
                    FROM JobSteps 
                    WHERE JobId = @JobId 
                    ORDER BY OrderIndex ASC";

                var steps = await _connection.QueryAsync<JobStep>(sql, new { JobId = jobId });
                
                // 处理依赖关系和条件表达式
                // 注意：Dependencies 现在是字符串类型，不需要额外处理

                return steps.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取作业步骤失败，JobId: {JobId}", jobId);
                throw;
            }
        }

        public async Task<JobStep?> GetByIdAsync(string id)
        {
            try
            {
                const string sql = @"
                    SELECT Id, JobId, Name, Description, Type, OrderIndex, IsEnabled, 
                           ExcelConfigId, SqlConfigId, TimeoutSeconds, RetryCount, RetryIntervalSeconds, 
                           ContinueOnFailure, Dependencies, ConditionExpression,
                           CreatedAt, UpdatedAt
                    FROM JobSteps 
                    WHERE Id = @Id";

                var step = await _connection.QueryFirstOrDefaultAsync<JobStep>(sql, new { Id = id });
                
                // 注意：Dependencies 现在是字符串类型，不需要额外处理

                return step;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取作业步骤失败，Id: {Id}", id);
                throw;
            }
        }

        public async Task<bool> CreateAsync(JobStep step)
        {
            try
            {
                const string sql = @"
                    INSERT INTO JobSteps (
                        Id, JobId, Name, Description, Type, OrderIndex, IsEnabled, 
                        ExcelConfigId, SqlConfigId, TimeoutSeconds, RetryCount, 
                        RetryIntervalSeconds, ContinueOnFailure, Dependencies, ConditionExpression,
                        CreatedAt, UpdatedAt
                    ) VALUES (
                        @Id, @JobId, @Name, @Description, @Type, @OrderIndex, @IsEnabled, 
                        @ExcelConfigId, @SqlConfigId, @TimeoutSeconds, @RetryCount, 
                        @RetryIntervalSeconds, @ContinueOnFailure, @Dependencies, @ConditionExpression,
                        @CreatedAt, @UpdatedAt
                    )";

                var parameters = new
                {
                    step.Id,
                    step.JobId,
                    step.Name,
                    step.Description,
                    Type = step.Type.ToString(),
                    step.OrderIndex,
                    IsEnabled = step.IsEnabled ? 1 : 0,
                    step.ExcelConfigId,
                    step.SqlConfigId,
                    step.TimeoutSeconds,
                    step.RetryCount,
                    step.RetryIntervalSeconds,
                    ContinueOnFailure = step.ContinueOnFailure ? 1 : 0,
                    Dependencies = step.Dependencies ?? string.Empty,
                    step.ConditionExpression,
                    step.CreatedAt,
                    step.UpdatedAt
                };

                var result = await _connection.ExecuteAsync(sql, parameters);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建作业步骤失败，Step: {@Step}", step);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(JobStep step)
        {
            try
            {
                const string sql = @"
                    UPDATE JobSteps SET 
                        Name = @Name, Description = @Description, Type = @Type, 
                        OrderIndex = @OrderIndex, IsEnabled = @IsEnabled, 
                        ExcelConfigId = @ExcelConfigId, SqlConfigId = @SqlConfigId,
                        TimeoutSeconds = @TimeoutSeconds, RetryCount = @RetryCount, 
                        RetryIntervalSeconds = @RetryIntervalSeconds, ContinueOnFailure = @ContinueOnFailure,
                        Dependencies = @Dependencies, ConditionExpression = @ConditionExpression,
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @Id";

                var parameters = new
                {
                    step.Id,
                    step.Name,
                    step.Description,
                    Type = step.Type.ToString(),
                    step.OrderIndex,
                    IsEnabled = step.IsEnabled ? 1 : 0,
                    step.ExcelConfigId,
                    step.SqlConfigId,
                    step.TimeoutSeconds,
                    step.RetryCount,
                    step.RetryIntervalSeconds,
                    ContinueOnFailure = step.ContinueOnFailure ? 1 : 0,
                    Dependencies = step.Dependencies != null ? JsonSerializer.Serialize(step.Dependencies) : null,
                    step.ConditionExpression,
                    UpdatedAt = DateTime.Now
                };

                var result = await _connection.ExecuteAsync(sql, parameters);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新作业步骤失败，Step: {@Step}", step);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                const string sql = "DELETE FROM JobSteps WHERE Id = @Id";
                var result = await _connection.ExecuteAsync(sql, new { Id = id });
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除作业步骤失败，Id: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteByJobIdAsync(string jobId)
        {
            try
            {
                const string sql = "DELETE FROM JobSteps WHERE JobId = @JobId";
                var result = await _connection.ExecuteAsync(sql, new { JobId = jobId });
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除作业所有步骤失败，JobId: {JobId}", jobId);
                throw;
            }
        }

        public async Task<bool> CreateBatchAsync(List<JobStep> steps)
        {
            try
            {
                using var transaction = _connection.BeginTransaction();
                
                foreach (var step in steps)
                {
                    const string sql = @"
                        INSERT INTO JobSteps (
                            Id, JobId, Name, Description, Type, OrderIndex, IsEnabled, 
                            ExcelConfigId, SqlConfigId, TimeoutSeconds, RetryCount, 
                            RetryIntervalSeconds, ContinueOnFailure, Dependencies, ConditionExpression,
                            CreatedAt, UpdatedAt
                        ) VALUES (
                            @Id, @JobId, @Name, @Description, @Type, @OrderIndex, @IsEnabled, 
                            @ExcelConfigId, @SqlConfigId, @TimeoutSeconds, @RetryCount, 
                            @RetryIntervalSeconds, @ContinueOnFailure, @Dependencies, @ConditionExpression,
                            @CreatedAt, @UpdatedAt
                        )";

                    var parameters = new
                    {
                        step.Id,
                        step.JobId,
                        step.Name,
                        step.Description,
                        Type = step.Type.ToString(),
                        step.OrderIndex,
                        IsEnabled = step.IsEnabled ? 1 : 0,
                        step.ExcelConfigId,
                        step.SqlConfigId,
                        step.TimeoutSeconds,
                        step.RetryCount,
                        step.RetryIntervalSeconds,
                        ContinueOnFailure = step.ContinueOnFailure ? 1 : 0,
                        Dependencies = step.Dependencies != null ? JsonSerializer.Serialize(step.Dependencies) : null,
                        step.ConditionExpression,
                        step.CreatedAt,
                        step.UpdatedAt
                    };

                    await _connection.ExecuteAsync(sql, parameters, transaction);
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量创建作业步骤失败，Steps: {@Steps}", steps);
                throw;
            }
        }

        public async Task<bool> UpdateOrderAsync(List<JobStep> steps)
        {
            try
            {
                using var transaction = _connection.BeginTransaction();
                
                foreach (var step in steps)
                {
                    const string sql = "UPDATE JobSteps SET OrderIndex = @OrderIndex, UpdatedAt = @UpdatedAt WHERE Id = @Id";
                    var parameters = new
                    {
                        step.Id,
                        step.OrderIndex,
                        UpdatedAt = DateTime.Now
                    };

                    await _connection.ExecuteAsync(sql, parameters, transaction);
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新作业步骤顺序失败，Steps: {@Steps}", steps);
                throw;
            }
        }
    }
} 