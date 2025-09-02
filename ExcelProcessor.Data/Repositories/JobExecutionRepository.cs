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
    /// 作业执行记录数据访问层实现
    /// </summary>
    public class JobExecutionRepository : IJobExecutionRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<JobExecutionRepository> _logger;

        public JobExecutionRepository(IDbConnection connection, ILogger<JobExecutionRepository> logger)
        {
            _connection = connection;
            _logger = logger;

            // 注册 TimeSpan 类型处理器（SQLite 将其存为 TEXT）
            if (!_timeSpanHandlerRegistered)
            {
                SqlMapper.AddTypeHandler(typeof(TimeSpan), new SqliteTimeSpanTypeHandler());
                SqlMapper.AddTypeHandler(typeof(TimeSpan?), new SqliteNullableTimeSpanTypeHandler());
                _timeSpanHandlerRegistered = true;
            }
        }

        private static bool _timeSpanHandlerRegistered = false;

        private sealed class SqliteTimeSpanTypeHandler : SqlMapper.TypeHandler<TimeSpan>
        {
            public override void SetValue(IDbDataParameter parameter, TimeSpan value)
            {
                parameter.Value = value.ToString();
            }

            public override TimeSpan Parse(object value)
            {
                if (value is TimeSpan ts)
                {
                    return ts;
                }
                if (value is string s && TimeSpan.TryParse(s, out var parsed))
                {
                    return parsed;
                }
                if (value is long ticks)
                {
                    return TimeSpan.FromTicks(ticks);
                }
                if (value is double seconds)
                {
                    return TimeSpan.FromSeconds(seconds);
                }
                if (value is decimal decSeconds)
                {
                    return TimeSpan.FromSeconds((double)decSeconds);
                }
                throw new InvalidCastException($"Cannot convert {value?.GetType().FullName ?? "null"} to TimeSpan");
            }
        }

        private sealed class SqliteNullableTimeSpanTypeHandler : SqlMapper.TypeHandler<TimeSpan?>
        {
            public override void SetValue(IDbDataParameter parameter, TimeSpan? value)
            {
                parameter.Value = value?.ToString();
            }

            public override TimeSpan? Parse(object value)
            {
                if (value is null || value is DBNull)
                {
                    return null;
                }
                if (value is TimeSpan ts)
                {
                    return ts;
                }
                if (value is string s && TimeSpan.TryParse(s, out var parsed))
                {
                    return parsed;
                }
                if (value is long ticks)
                {
                    return TimeSpan.FromTicks(ticks);
                }
                if (value is double seconds)
                {
                    return TimeSpan.FromSeconds(seconds);
                }
                if (value is decimal decSeconds)
                {
                    return TimeSpan.FromSeconds((double)decSeconds);
                }
                throw new InvalidCastException($"Cannot convert {value.GetType().FullName} to TimeSpan?");
            }
        }

        public async Task<List<JobExecution>> GetAllAsync()
        {
            try
            {
                const string sql = @"
                    SELECT Id, JobId, JobName, Status, StartTime, EndTime, Duration, 
                           ExecutedBy, Parameters, Results, ErrorMessage, ErrorDetails,
                           CreatedAt, UpdatedAt
                    FROM JobExecutions 
                    ORDER BY StartTime DESC";

                var executions = await _connection.QueryAsync<dynamic>(sql);
                return executions.Select(MapToJobExecution).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有执行记录失败");
                throw;
            }
        }

        public async Task<JobExecution?> GetByIdAsync(string id)
        {
            try
            {
                const string sql = @"
                    SELECT Id, JobId, JobName, Status, StartTime, EndTime, Duration, 
                           ExecutedBy, Parameters, Results, ErrorMessage, ErrorDetails,
                           CreatedAt, UpdatedAt
                    FROM JobExecutions 
                    WHERE Id = @Id";

                var parameters = new { Id = id };
                var result = await _connection.QueryFirstOrDefaultAsync<dynamic>(sql, parameters);
                return result != null ? MapToJobExecution(result) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取执行记录失败: {ExecutionId}", id);
                throw;
            }
        }

        private JobExecution MapToJobExecution(dynamic row)
        {
            return new JobExecution
            {
                Id = row.Id,
                JobId = row.JobId,
                JobName = row.JobName,
                Status = Enum.Parse<JobStatus>(row.Status),
                StartTime = DateTime.Parse(row.StartTime),
                EndTime = row.EndTime != null ? DateTime.Parse(row.EndTime) : null,
                Duration = row.Duration != null ? TimeSpan.FromSeconds((double)row.Duration) : null,
                ExecutedBy = row.ExecutedBy,
                Parameters = !string.IsNullOrEmpty(row.Parameters) ? JsonSerializer.Deserialize<Dictionary<string, object>>(row.Parameters) ?? new Dictionary<string, object>() : new Dictionary<string, object>(),
                Results = !string.IsNullOrEmpty(row.Results) ? JsonSerializer.Deserialize<Dictionary<string, object>>(row.Results) ?? new Dictionary<string, object>() : new Dictionary<string, object>(),
                ErrorMessage = row.ErrorMessage,
                ErrorDetails = row.ErrorDetails,
                CreatedAt = DateTime.Parse(row.CreatedAt),
                UpdatedAt = DateTime.Parse(row.UpdatedAt)
            };
        }

        public async Task<(List<JobExecution> executions, int totalCount)> GetByJobIdAsync(string jobId, int page = 1, int pageSize = 20)
        {
            try
            {
                var offset = (page - 1) * pageSize;

                const string countSql = "SELECT COUNT(1) FROM JobExecutions WHERE JobId = @JobId";
                const string dataSql = @"
                    SELECT Id, JobId, JobName, Status, StartTime, EndTime, Duration, 
                           ExecutedBy, Parameters, Results, ErrorMessage, ErrorDetails,
                           CreatedAt, UpdatedAt
                    FROM JobExecutions 
                    WHERE JobId = @JobId
                    ORDER BY StartTime DESC
                    LIMIT @PageSize OFFSET @Offset";

                var parameters = new { JobId = jobId, PageSize = pageSize, Offset = offset };

                var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, new { JobId = jobId });
                var executions = await _connection.QueryAsync<dynamic>(dataSql, parameters);

                return (executions.Select(MapToJobExecution).ToList(), totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据作业ID获取执行记录失败: {JobId}", jobId);
                throw;
            }
        }

        public async Task<List<JobExecution>> GetAllByJobIdAsync(string jobId)
        {
            try
            {
                const string sql = @"
                    SELECT Id, JobId, JobName, Status, StartTime, EndTime, Duration, 
                           ExecutedBy, Parameters, Results, ErrorMessage, ErrorDetails,
                           CreatedAt, UpdatedAt
                    FROM JobExecutions 
                    WHERE JobId = @JobId
                    ORDER BY StartTime DESC";

                var parameters = new { JobId = jobId };
                var executions = await _connection.QueryAsync<dynamic>(sql, parameters);
                return executions.Select(MapToJobExecution).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据作业ID获取所有执行记录失败: {JobId}", jobId);
                throw;
            }
        }

        public async Task<JobExecution?> GetLatestByJobIdAsync(string jobId)
        {
            try
            {
                const string sql = @"
                    SELECT Id, JobId, JobName, Status, StartTime, EndTime, Duration, 
                           ExecutedBy, Parameters, Results, ErrorMessage, ErrorDetails,
                           CreatedAt, UpdatedAt
                    FROM JobExecutions 
                    WHERE JobId = @JobId
                    ORDER BY StartTime DESC
                    LIMIT 1";

                var parameters = new { JobId = jobId };
                var execution = await _connection.QueryFirstOrDefaultAsync<dynamic>(sql, parameters);
                return execution != null ? MapToJobExecution(execution) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据作业ID获取最新执行记录失败: {JobId}", jobId);
                throw;
            }
        }

        public async Task<List<JobExecution>> GetRunningAsync()
        {
            try
            {
                const string sql = @"
                    SELECT Id, JobId, JobName, Status, StartTime, EndTime, Duration, 
                           ExecutedBy, Parameters, Results, ErrorMessage, ErrorDetails,
                           CreatedAt, UpdatedAt
                    FROM JobExecutions 
                    WHERE Status = @Status
                    ORDER BY StartTime DESC";

                var parameters = new { Status = JobStatus.Running.ToString() };
                var executions = await _connection.QueryAsync<dynamic>(sql, parameters);
                return executions.Select(MapToJobExecution).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取正在运行的执行记录失败");
                throw;
            }
        }

        public async Task<List<JobExecution>> GetByStatusAsync(JobStatus status)
        {
            try
            {
                const string sql = @"
                    SELECT Id, JobId, JobName, Status, StartTime, EndTime, Duration, 
                           ExecutedBy, Parameters, Results, ErrorMessage, ErrorDetails,
                           CreatedAt, UpdatedAt
                    FROM JobExecutions 
                    WHERE Status = @Status
                    ORDER BY StartTime DESC";

                var parameters = new { Status = status.ToString() };
                var executions = await _connection.QueryAsync<dynamic>(sql, parameters);
                return executions.Select(MapToJobExecution).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据状态获取执行记录失败: {Status}", status);
                throw;
            }
        }

        public async Task<List<JobExecution>> GetByTimeRangeAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                const string sql = @"
                    SELECT Id, JobId, JobName, Status, StartTime, EndTime, Duration, 
                           ExecutedBy, Parameters, Results, ErrorMessage, ErrorDetails,
                           CreatedAt, UpdatedAt
                    FROM JobExecutions 
                    WHERE StartTime >= @StartTime AND StartTime <= @EndTime
                    ORDER BY StartTime DESC";

                var parameters = new { StartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss"), EndTime = endTime.ToString("yyyy-MM-dd HH:mm:ss") };
                var executions = await _connection.QueryAsync<dynamic>(sql, parameters);
                return executions.Select(MapToJobExecution).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据时间范围获取执行记录失败");
                throw;
            }
        }

        public async Task<List<JobExecution>> GetByExecutedByAsync(string executedBy)
        {
            try
            {
                const string sql = @"
                    SELECT Id, JobId, JobName, Status, StartTime, EndTime, Duration, 
                           ExecutedBy, Parameters, Results, ErrorMessage, ErrorDetails,
                           CreatedAt, UpdatedAt
                    FROM JobExecutions 
                    WHERE ExecutedBy = @ExecutedBy
                    ORDER BY StartTime DESC";

                var parameters = new { ExecutedBy = executedBy };
                var executions = await _connection.QueryAsync<dynamic>(sql, parameters);
                return executions.Select(MapToJobExecution).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据执行人获取执行记录失败: {ExecutedBy}", executedBy);
                throw;
            }
        }

        public async Task<bool> CreateAsync(JobExecution execution)
        {
            try
            {
                const string sql = @"
                    INSERT INTO JobExecutions (
                        Id, JobId, JobName, Status, StartTime, EndTime, Duration, 
                        ExecutedBy, Parameters, Results, ErrorMessage, ErrorDetails,
                        CreatedAt, UpdatedAt
                    ) VALUES (
                        @Id, @JobId, @JobName, @Status, @StartTime, @EndTime, @Duration, 
                        @ExecutedBy, @Parameters, @Results, @ErrorMessage, @ErrorDetails,
                        @CreatedAt, @UpdatedAt
                    )";

                var parameters = new
                {
                    execution.Id,
                    execution.JobId,
                    execution.JobName,
                    Status = execution.Status.ToString(),
                    StartTime = execution.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    EndTime = execution.EndTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                    Duration = execution.Duration?.TotalSeconds,
                    execution.ExecutedBy,
                    Parameters = JsonSerializer.Serialize(execution.Parameters),
                    Results = JsonSerializer.Serialize(execution.Results),
                    execution.ErrorMessage,
                    execution.ErrorDetails,
                    CreatedAt = execution.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = execution.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                };

                var affectedRows = await _connection.ExecuteAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建执行记录失败: {ExecutionId}", execution.Id);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(JobExecution execution)
        {
            try
            {
                const string sql = @"
                    UPDATE JobExecutions SET 
                        JobId = @JobId, JobName = @JobName, Status = @Status, 
                        StartTime = @StartTime, EndTime = @EndTime, Duration = @Duration, 
                        ExecutedBy = @ExecutedBy, Parameters = @Parameters, Results = @Results, 
                        ErrorMessage = @ErrorMessage, ErrorDetails = @ErrorDetails, 
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @Id";

                var parameters = new
                {
                    execution.Id,
                    execution.JobId,
                    execution.JobName,
                    Status = execution.Status.ToString(),
                    StartTime = execution.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    EndTime = execution.EndTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                    Duration = execution.Duration?.TotalSeconds,
                    execution.ExecutedBy,
                    Parameters = JsonSerializer.Serialize(execution.Parameters),
                    Results = JsonSerializer.Serialize(execution.Results),
                    execution.ErrorMessage,
                    execution.ErrorDetails,
                    UpdatedAt = execution.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                };

                var affectedRows = await _connection.ExecuteAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新执行记录失败: {ExecutionId}", execution.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                const string sql = "DELETE FROM JobExecutions WHERE Id = @Id";
                var parameters = new { Id = id };
                var affectedRows = await _connection.ExecuteAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除执行记录失败: {ExecutionId}", id);
                throw;
            }
        }

        public async Task<bool> BatchDeleteAsync(List<string> ids)
        {
            try
            {
                const string sql = "DELETE FROM JobExecutions WHERE Id IN @Ids";
                var parameters = new { Ids = ids };
                var affectedRows = await _connection.ExecuteAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除执行记录失败: {ExecutionIds}", string.Join(", ", ids));
                throw;
            }
        }

        public async Task<int> DeleteExpiredAsync(DateTime cutoffDate)
        {
            try
            {
                const string sql = "DELETE FROM JobExecutions WHERE StartTime < @CutoffDate";
                var parameters = new { CutoffDate = cutoffDate };
                var affectedRows = await _connection.ExecuteAsync(sql, parameters);
                return affectedRows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除过期执行记录失败");
                throw;
            }
        }

        public async Task<int> GetCountAsync()
        {
            try
            {
                const string sql = "SELECT COUNT(1) FROM JobExecutions";
                return await _connection.ExecuteScalarAsync<int>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取执行记录数量失败");
                throw;
            }
        }

        public async Task<int> GetCountByJobIdAsync(string jobId)
        {
            try
            {
                const string sql = "SELECT COUNT(1) FROM JobExecutions WHERE JobId = @JobId";
                var parameters = new { JobId = jobId };
                return await _connection.ExecuteScalarAsync<int>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据作业ID获取执行记录数量失败: {JobId}", jobId);
                throw;
            }
        }

        public async Task<int> GetCountByStatusAsync(JobStatus status)
        {
            try
            {
                const string sql = "SELECT COUNT(1) FROM JobExecutions WHERE Status = @Status";
                var parameters = new { Status = status };
                return await _connection.ExecuteScalarAsync<int>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据状态获取执行记录数量失败: {Status}", status);
                throw;
            }
        }

        #region 步骤执行记录

        public async Task<List<JobStepExecution>> GetStepExecutionsAsync(string executionId)
        {
            try
            {
                const string sql = @"
                    SELECT 
                        Id, 
                        JobExecutionId AS ExecutionId,
                        StepId, 
                        StepName, 
                        StepType, 
                        Status, 
                        StartTime, 
                        EndTime, 
                        Duration, 
                        ErrorMessage, 
                        CreatedAt, 
                        UpdatedAt
                    FROM JobStepExecutions 
                    WHERE JobExecutionId = @ExecutionId
                    ORDER BY StartTime ASC";

                var parameters = new { ExecutionId = executionId };
                var stepExecutions = await _connection.QueryAsync<JobStepExecution>(sql, parameters);
                return stepExecutions.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取步骤执行记录失败: {ExecutionId}", executionId);
                throw;
            }
        }

        public async Task<JobStepExecution?> GetStepExecutionByIdAsync(string id)
        {
            try
            {
                const string sql = @"
                    SELECT 
                        Id, 
                        JobExecutionId AS ExecutionId,
                        StepId, 
                        StepName, 
                        StepType, 
                        Status, 
                        StartTime, 
                        EndTime, 
                        Duration, 
                        ErrorMessage, 
                        CreatedAt, 
                        UpdatedAt
                    FROM JobStepExecutions 
                    WHERE Id = @Id";

                var parameters = new { Id = id };
                return await _connection.QueryFirstOrDefaultAsync<JobStepExecution>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取步骤执行记录失败: {StepExecutionId}", id);
                throw;
            }
        }

        public async Task<bool> CreateStepExecutionAsync(JobStepExecution stepExecution)
        {
            try
            {
                const string sql = @"
                    INSERT INTO JobStepExecutions (
                        Id, JobExecutionId, StepId, StepName, StepType, Status, StartTime, EndTime,
                        Duration, ErrorMessage, CreatedAt, UpdatedAt
                    ) VALUES (
                        @Id, @ExecutionId, @StepId, @StepName, @StepType, @Status, @StartTime, @EndTime,
                        @Duration, @ErrorMessage, @CreatedAt, @UpdatedAt
                    )";

                var affectedRows = await _connection.ExecuteAsync(sql, stepExecution);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建步骤执行记录失败: {StepExecutionId}", stepExecution.Id);
                throw;
            }
        }

        public async Task<bool> UpdateStepExecutionAsync(JobStepExecution stepExecution)
        {
            try
            {
                const string sql = @"
                    UPDATE JobStepExecutions SET 
                        JobExecutionId = @ExecutionId, 
                        StepId = @StepId, 
                        StepName = @StepName, 
                        StepType = @StepType,
                        Status = @Status, 
                        StartTime = @StartTime, 
                        EndTime = @EndTime, 
                        Duration = @Duration, 
                        ErrorMessage = @ErrorMessage, 
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @Id";

                var affectedRows = await _connection.ExecuteAsync(sql, stepExecution);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新步骤执行记录失败: {StepExecutionId}", stepExecution.Id);
                throw;
            }
        }

        public async Task<bool> DeleteStepExecutionAsync(string id)
        {
            try
            {
                const string sql = "DELETE FROM JobStepExecutions WHERE Id = @Id";
                var parameters = new { Id = id };
                var affectedRows = await _connection.ExecuteAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除步骤执行记录失败: {StepExecutionId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteStepExecutionsByExecutionIdAsync(string executionId)
        {
            try
            {
                const string sql = "DELETE FROM JobStepExecutions WHERE JobExecutionId = @ExecutionId";
                var parameters = new { ExecutionId = executionId };
                var affectedRows = await _connection.ExecuteAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据执行ID删除步骤执行记录失败: {ExecutionId}", executionId);
                throw;
            }
        }

        #endregion
    }
} 