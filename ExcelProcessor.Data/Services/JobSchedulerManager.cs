using ExcelProcessor.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// 作业调度器管理器，用于解决循环依赖问题
    /// </summary>
    public class JobSchedulerManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobSchedulerManager> _logger;
        private bool _isConfigured = false;

        public JobSchedulerManager(IServiceProvider serviceProvider, ILogger<JobSchedulerManager> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// 配置作业调度器和作业服务的依赖关系
        /// </summary>
        public void ConfigureDependencies()
        {
            if (_isConfigured)
            {
                return;
            }

            try
            {
                // 获取JobScheduler实例
                var jobScheduler = _serviceProvider.GetRequiredService<JobScheduler>();
                
                // 获取JobService实例
                var jobService = _serviceProvider.GetRequiredService<IJobService>();
                
                // 设置JobService的JobScheduler引用
                if (jobService is JobService concreteJobService)
                {
                    concreteJobService.SetJobScheduler(jobScheduler);
                    _logger.LogInformation("作业调度器和作业服务依赖关系配置完成");
                }
                else
                {
                    _logger.LogError("JobService不是预期的类型，无法配置依赖关系");
                }

                _isConfigured = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "配置作业调度器依赖关系时发生异常");
                throw;
            }
        }
    }
} 
 
 