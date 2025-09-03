using ExcelProcessor.Core.Services;
using ExcelProcessor.Core.Interfaces;
using ExcelProcessor.Data.Repositories;
using ExcelProcessor.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using ExcelProcessor.Core.Repositories;
using ExcelProcessor.Models;

namespace ExcelProcessor.Data.DependencyInjection
{
    /// <summary>
    /// 数据服务集合扩展方法
    /// </summary>
    public static class DataServiceCollectionExtensions
    {
        /// <summary>
        /// 添加数据服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDataServices(this IServiceCollection services)
        {
            // 注册仓储
            services.AddScoped<UserRepository>();
            services.AddScoped<ExcelConfigRepository>();
            services.AddScoped<IJobRepository, JobRepository>();
            services.AddScoped<IJobStepRepository, JobStepRepository>();
            services.AddScoped<IJobExecutionRepository, JobExecutionRepository>();
            services.AddScoped<IJobStatisticsRepository, JobStatisticsRepository>();
            
            // 新增：核心仓储接口映射
            services.AddScoped<IRepository<User>, UserRepository>();
            services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();

            // 新增：Excel 相关仓储接口映射
            services.AddScoped<IRepository<ExcelConfig>, ExcelConfigRepository>();
            services.AddScoped<IRepository<ExcelFieldMapping>, ExcelFieldMappingRepository>();
            services.AddScoped<IExcelFieldMappingRepository, ExcelFieldMappingRepository>();
            services.AddScoped<IRepository<ExcelImportResult>, ExcelImportResultRepository>();

            // 注册业务服务
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IExcelConfigService, ExcelConfigService>();
            services.AddScoped<IExcelService, ExcelService>();
            services.AddScoped<IJobService, JobService>();
            services.AddScoped<IDataSourceService>(provider =>
            {
                var logger = provider.GetService<Microsoft.Extensions.Logging.ILogger<DataSourceService>>();
                var connectionString = provider.GetService<string>();
                return new DataSourceService(logger, connectionString);
            });

            // 新增：认证与系统配置服务
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ISystemConfigService, SystemConfigService>();

            // 注册作业执行引擎
            services.AddScoped<IJobExecutionEngine, JobExecutionEngine>();

            // 注册数据导入服务
            services.AddScoped<IDataImportService, DataImportService>();

            // 注册SQL相关服务
            services.AddScoped<ISqlService, SqlService>();
            services.AddScoped<IDatabaseTableService, DatabaseTableService>();
            services.AddScoped<ISqlOutputService, SqlOutputService>();

            // 注册作业配置包服务
            services.AddScoped<IJobPackageService, JobPackageService>();
            services.AddScoped<IImportExportHistoryService, ImportExportHistoryService>();

            // 注册作业调度器（单例模式）
            services.AddSingleton<JobScheduler>();

            // 配置循环依赖解决
            services.AddSingleton<JobSchedulerManager>();

            return services;
        }

        /// <summary>
        /// 配置作业调度器和作业服务的循环依赖
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection ConfigureJobSchedulerDependencies(this IServiceCollection services)
        {
            services.AddSingleton<JobSchedulerManager>();
            return services;
        }
    }
}