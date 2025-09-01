using ExcelProcessor.Data.Repositories;
using ExcelProcessor.Data.Services;
using ExcelProcessor.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ExcelProcessor.Data.DependencyInjection
{
    /// <summary>
    /// 服务集合扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册数据访问层服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDataServices(this IServiceCollection services)
        {
            // 注册仓储
            // TODO: 这些接口需要从 Core 项目定义
            // services.AddScoped<IUserRepository, UserRepository>();
            // services.AddScoped<IRoleRepository, RoleRepository>();
            // services.AddScoped<IPermissionRepository, PermissionRepository>();
            // services.AddScoped<IDataSourceRepository, DataSourceRepository>();
            // services.AddScoped<IExcelConfigRepository, ExcelConfigRepository>();
            // services.AddScoped<IExcelFieldMappingRepository, ExcelFieldMappingRepository>();
            // services.AddScoped<ISqlConfigRepository, SqlConfigRepository>();
            services.AddScoped<IJobRepository, JobRepository>();
            services.AddScoped<IJobStepRepository, JobStepRepository>();
            // services.AddScoped<IJobExecutionRepository, JobExecutionRepository>();
            // services.AddScoped<IJobStepExecutionRepository, JobStepExecutionRepository>();
            // services.AddScoped<IConfigurationReferenceRepository, ConfigurationReferenceRepository>();

            // 注册服务
            // TODO: 这些接口需要从 Core 项目定义
            // services.AddScoped<IUserService, UserService>();
            // services.AddScoped<IRoleService, RoleService>();
            // services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IDataSourceService, DataSourceService>();
            // services.AddScoped<IExcelConfigService, ExcelConfigService>();
            // services.AddScoped<IExcelService, ExcelService>();
            // services.AddScoped<ISqlService, SqlService>();
            // TODO: 这些接口需要从 Core 项目定义
            // services.AddScoped<IJobService, JobService>();
            // services.AddScoped<IJobScheduler, JobScheduler>();
            // services.AddScoped<IJobExecutionEngine, JobExecutionEngine>();
            services.AddScoped<IDataImportService, DataImportService>();
            // services.AddScoped<IConfigurationReferenceService, ConfigurationReferenceService>();

            return services;
        }
    }
} 