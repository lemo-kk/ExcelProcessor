using System;
using System.Linq;
using System.Threading.Tasks;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Data.Repositories;
using ExcelProcessor.Data.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ExcelProcessor.Data.Tests
{
    public class JobSchedulerTests
    {
        private readonly Mock<IJobRepository> _mockJobRepository;
        private readonly Mock<ILogger<JobScheduler>> _mockLogger;
        private readonly JobScheduler _scheduler;

        public JobSchedulerTests()
        {
            _mockJobRepository = new Mock<IJobRepository>();
            _mockLogger = new Mock<ILogger<JobScheduler>>();
            _scheduler = new JobScheduler(_mockJobRepository.Object, _mockLogger.Object);
            
            // 设置默认的Mock行为
            _mockJobRepository.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string jobId) => new Models.JobConfig 
                { 
                    Id = jobId, 
                    Name = $"Test Job {jobId}", 
                    Type = "TestType",
                    IsEnabled = true,
                    Status = Models.JobStatus.Pending,
                    Priority = Models.JobPriority.Normal,
                    ExecutionMode = Models.ExecutionMode.Scheduled
                });
        }

        [Fact]
        public async Task StartAsync_ShouldStartScheduler()
        {
            // Act
            var result = await _scheduler.StartAsync();

            // Assert
            Assert.True(result);
            var status = _scheduler.GetStatus();
            Assert.True(status.isRunning);
            Assert.False(status.isPaused);
        }

        [Fact]
        public async Task StopAsync_ShouldStopScheduler()
        {
            // Arrange
            await _scheduler.StartAsync();

            // Act
            var result = await _scheduler.StopAsync();

            // Assert
            Assert.True(result);
            var status = _scheduler.GetStatus();
            Assert.False(status.isRunning);
            Assert.False(status.isPaused);
        }

        [Fact]
        public async Task PauseAsync_ShouldPauseScheduler()
        {
            // Arrange
            await _scheduler.StartAsync();

            // Act
            var result = await _scheduler.PauseAsync();

            // Assert
            Assert.True(result);
            var status = _scheduler.GetStatus();
            Assert.True(status.isRunning);
            Assert.True(status.isPaused);
        }

        [Fact]
        public async Task ResumeAsync_ShouldResumeScheduler()
        {
            // Arrange
            await _scheduler.StartAsync();
            await _scheduler.PauseAsync();

            // Act
            var result = await _scheduler.ResumeAsync();

            // Assert
            Assert.True(result);
            var status = _scheduler.GetStatus();
            Assert.True(status.isRunning);
            Assert.False(status.isPaused);
        }

        [Fact]
        public async Task AddScheduledJobAsync_WithValidCron_ShouldSucceed()
        {
            // Arrange
            var jobId = "test-job-1";
            var cronExpression = "0 0 * * *"; // 每天午夜执行

            // 验证Mock设置
            var mockJob = await _mockJobRepository.Object.GetByIdAsync(jobId);
            Console.WriteLine($"Mock job: {mockJob?.Id}, {mockJob?.Name}, {mockJob?.IsEnabled}");

            // 验证Cron表达式
            var cronParseResult = Cronos.CronExpression.TryParse(cronExpression, out var cron);
            Console.WriteLine($"Cron parse result: {cronParseResult}");

            // Act
            var result = await _scheduler.AddScheduledJobAsync(jobId, cronExpression);

            // Assert
            Console.WriteLine($"AddScheduledJobAsync result: {result}");
            
            Assert.True(result);
            var scheduledJobs = _scheduler.GetScheduledJobs();
            Console.WriteLine($"Scheduled jobs count: {scheduledJobs.Count}");
            Assert.True(scheduledJobs.Any(job => job.jobId == jobId));
        }

        [Fact]
        public async Task AddScheduledJobAsync_WithInvalidCron_ShouldFail()
        {
            // Arrange
            var jobId = "test-job-1";
            var invalidCronExpression = "invalid-cron";

            // Act
            var result = await _scheduler.AddScheduledJobAsync(jobId, invalidCronExpression);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RemoveScheduledJobAsync_ShouldRemoveJob()
        {
            // Arrange
            var jobId = "test-job-1";
            var cronExpression = "0 0 * * *";
            await _scheduler.AddScheduledJobAsync(jobId, cronExpression);

            // Act
            var result = await _scheduler.RemoveScheduledJobAsync(jobId);

            // Assert
            Assert.True(result);
            var scheduledJobs = _scheduler.GetScheduledJobs();
            Assert.False(scheduledJobs.Any(job => job.jobId == jobId));
        }

        [Fact]
        public async Task GetScheduledJobs_ShouldReturnAllScheduledJobs()
        {
            // Arrange
            await _scheduler.AddScheduledJobAsync("job-1", "0 0 * * *");
            await _scheduler.AddScheduledJobAsync("job-2", "0 12 * * *");

            // Act
            var scheduledJobs = _scheduler.GetScheduledJobs();

            // Assert
            Assert.Equal(2, scheduledJobs.Count);
            Assert.True(scheduledJobs.Any(job => job.jobId == "job-1"));
            Assert.True(scheduledJobs.Any(job => job.jobId == "job-2"));
        }

        [Fact]
        public async Task UpdateJobEnabledStatus_ShouldUpdateJobStatus()
        {
            // Arrange
            var jobId = "test-job-1";
            var cronExpression = "0 0 * * *";
            await _scheduler.AddScheduledJobAsync(jobId, cronExpression);

            // Act
            _scheduler.UpdateJobEnabledStatus(jobId, false);

            // Assert
            var scheduledJobs = _scheduler.GetScheduledJobs();
            var job = scheduledJobs.FirstOrDefault(j => j.jobId == jobId);
            Assert.NotNull(job);
            Assert.False(job.isEnabled);
        }

        [Fact]
        public async Task UpdateJobCronExpressionAsync_WithValidCron_ShouldSucceed()
        {
            // Arrange
            var jobId = "test-job-1";
            var originalCron = "0 0 * * *";
            var newCron = "0 12 * * *";
            await _scheduler.AddScheduledJobAsync(jobId, originalCron);

            // Act
            var result = await _scheduler.UpdateJobCronExpressionAsync(jobId, newCron);

            // Assert
            Assert.True(result);
            var scheduledJobs = _scheduler.GetScheduledJobs();
            var job = scheduledJobs.FirstOrDefault(j => j.jobId == jobId);
            Assert.NotNull(job);
            Assert.Equal(newCron, job.cronExpression);
        }

        [Fact]
        public async Task UpdateJobCronExpressionAsync_WithInvalidCron_ShouldFail()
        {
            // Arrange
            var jobId = "test-job-1";
            var originalCron = "0 0 * * *";
            var invalidCron = "invalid-cron";
            await _scheduler.AddScheduledJobAsync(jobId, originalCron);

            // Act
            var result = await _scheduler.UpdateJobCronExpressionAsync(jobId, invalidCron);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateJobCronExpressionAsync_WithNonExistentJob_ShouldFail()
        {
            // Arrange
            var jobId = "non-existent-job";
            var newCron = "0 12 * * *";

            // Act
            var result = await _scheduler.UpdateJobCronExpressionAsync(jobId, newCron);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SimpleTest_ShouldWork()
        {
            // Arrange
            var jobId = "simple-test-job";
            var cronExpression = "0 0 * * *";

            // Act
            var result = await _scheduler.AddScheduledJobAsync(jobId, cronExpression);

            // Assert
            Console.WriteLine($"Simple test result: {result}");
            Assert.True(result);
        }

        public void Dispose()
        {
            _scheduler?.Dispose();
        }
    }
}