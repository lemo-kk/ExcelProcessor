using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// 作业统计信息数据访问层实现
    /// </summary>
    public class JobStatisticsRepository : IJobStatisticsRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<JobStatisticsRepository> _logger;

        public JobStatisticsRepository(IDbConnection connection, ILogger<JobStatisticsRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<List<JobStatistics>> GetAllAsync()
        {
            try
            {
                const string sql = @"
                    SELECT JobId, JobName, TotalExecutions, SuccessfulExecutions, FailedExecutions, 
                           CancelledExecutions, SuccessRate, AverageDuration, TotalDuration,
                           LastExecutionTime, FirstExecutionTime, CreatedAt, UpdatedAt
                    FROM JobStatistics 
                    ORDER BY LastExecutionTime DESC";

                var statistics = await _connection.QueryAsync<JobStatistics>(sql);
                return statistics.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有作业统计信息失败");
                throw;
            }
        }

        public async Task<JobStatistics?> GetByJobIdAsync(string jobId)
        {
            try
            {
                const string sql = @"
                    SELECT JobId, JobName, TotalExecutions, SuccessfulExecutions, FailedExecutions, 
                           CancelledExecutions, SuccessRate, 
                           CAST(AverageDuration AS TEXT) as AverageDuration, 
                           CAST(TotalDuration AS TEXT) as TotalDuration,
                           LastExecutionTime, FirstExecutionTime, CreatedAt, UpdatedAt
                    FROM JobStatistics 
                    WHERE JobId = @JobId";

                var parameters = new { JobId = jobId };
                var result = await _connection.QueryFirstOrDefaultAsync<dynamic>(sql, parameters);
                
                if (result == null)
                    return null;

                return new JobStatistics
                {
                    JobId = result.JobId,
                    JobName = result.JobName,
                    TotalExecutions = Convert.ToInt32(result.TotalExecutions),
                    SuccessfulExecutions = Convert.ToInt32(result.SuccessfulExecutions),
                    FailedExecutions = Convert.ToInt32(result.FailedExecutions),
                    CancelledExecutions = Convert.ToInt32(result.CancelledExecutions),
                    SuccessRate = Convert.ToDouble(result.SuccessRate),
                    AverageDuration = ParseTimeSpan(result.AverageDuration),
                    TotalDuration = ParseTimeSpan(result.TotalDuration),
                    LastExecutionTime = result.LastExecutionTime != null ? DateTime.Parse(result.LastExecutionTime.ToString()) : null,
                    FirstExecutionTime = result.FirstExecutionTime != null ? DateTime.Parse(result.FirstExecutionTime.ToString()) : null,
                    CreatedAt = result.CreatedAt != null ? DateTime.Parse(result.CreatedAt.ToString()) : null,
                    UpdatedAt = result.UpdatedAt != null ? DateTime.Parse(result.UpdatedAt.ToString()) : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据作业ID获取统计信息失败: {JobId}", jobId);
                throw;
            }
        }

        private static TimeSpan ParseTimeSpan(object? value)
        {
            if (value == null || value == DBNull.Value)
                return TimeSpan.Zero;

            if (value is TimeSpan timeSpan)
                return timeSpan;

            if (value is string strValue)
            {
                if (TimeSpan.TryParse(strValue, out var parsed))
                    return parsed;
                
                // 尝试解析格式为 "00:00:00" 的字符串
                if (strValue.Contains(":"))
                {
                    var parts = strValue.Split(':');
                    if (parts.Length >= 2)
                    {
                        if (int.TryParse(parts[0], out var hours) && 
                            int.TryParse(parts[1], out var minutes))
                        {
                            var seconds = parts.Length > 2 && int.TryParse(parts[2], out var sec) ? sec : 0;
                            return new TimeSpan(hours, minutes, seconds);
                        }
                    }
                }
            }

            return TimeSpan.Zero;
        }

        public async Task<bool> CreateAsync(JobStatistics statistics)
        {
            try
            {
                const string sql = @"
                    INSERT INTO JobStatistics (
                        JobId, JobName, TotalExecutions, SuccessfulExecutions, FailedExecutions, 
                        CancelledExecutions, SuccessRate, AverageDuration, TotalDuration,
                        LastExecutionTime, FirstExecutionTime, CreatedAt, UpdatedAt
                    ) VALUES (
                        @JobId, @JobName, @TotalExecutions, @SuccessfulExecutions, @FailedExecutions, 
                        @CancelledExecutions, @SuccessRate, @AverageDuration, @TotalDuration,
                        @LastExecutionTime, @FirstExecutionTime, @CreatedAt, @UpdatedAt
                    )";

                var affectedRows = await _connection.ExecuteAsync(sql, statistics);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建作业统计信息失败: {JobId}", statistics.JobId);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(JobStatistics statistics)
        {
            try
            {
                const string sql = @"
                    UPDATE JobStatistics SET 
                        JobName = @JobName, TotalExecutions = @TotalExecutions, 
                        SuccessfulExecutions = @SuccessfulExecutions, FailedExecutions = @FailedExecutions, 
                        CancelledExecutions = @CancelledExecutions, SuccessRate = @SuccessRate, 
                        AverageDuration = @AverageDuration, TotalDuration = @TotalDuration,
                        LastExecutionTime = @LastExecutionTime, FirstExecutionTime = @FirstExecutionTime, 
                        UpdatedAt = @UpdatedAt
                    WHERE JobId = @JobId";

                var affectedRows = await _connection.ExecuteAsync(sql, statistics);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新作业统计信息失败: {JobId}", statistics.JobId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string jobId)
        {
            try
            {
                const string sql = "DELETE FROM JobStatistics WHERE JobId = @JobId";
                var parameters = new { JobId = jobId };
                var affectedRows = await _connection.ExecuteAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除作业统计信息失败: {JobId}", jobId);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string jobId)
        {
            try
            {
                const string sql = "SELECT COUNT(1) FROM JobStatistics WHERE JobId = @JobId";
                var parameters = new { JobId = jobId };
                var count = await _connection.ExecuteScalarAsync<int>(sql, parameters);
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查作业统计信息是否存在失败: {JobId}", jobId);
                throw;
            }
        }

        public async Task<int> GetCountAsync()
        {
            try
            {
                const string sql = "SELECT COUNT(1) FROM JobStatistics";
                return await _connection.ExecuteScalarAsync<int>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取作业统计信息数量失败");
                throw;
            }
        }

        public async Task<bool> BatchDeleteAsync(List<string> jobIds)
        {
            try
            {
                const string sql = "DELETE FROM JobStatistics WHERE JobId IN @JobIds";
                var parameters = new { JobIds = jobIds };
                var affectedRows = await _connection.ExecuteAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除作业统计信息失败: {JobIds}", string.Join(", ", jobIds));
                throw;
            }
        }

        public async Task<int> CleanupExpiredAsync(int daysToKeep = 90)
        {
            try
            {
                const string sql = "DELETE FROM JobStatistics WHERE LastExecutionTime < @CutoffDate";
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var parameters = new { CutoffDate = cutoffDate };
                var affectedRows = await _connection.ExecuteAsync(sql, parameters);
                return affectedRows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期作业统计信息失败");
                throw;
            }
        }
    }
} 