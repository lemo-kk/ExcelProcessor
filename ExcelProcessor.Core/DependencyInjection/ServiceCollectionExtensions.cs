using ExcelProcessor.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Core.DependencyInjection
{
    /// <summary>
    /// 服务集合扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加核心服务
        /// </summary>
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            // 注意：服务实现注册在Data项目中
            // 这里只注册接口，具体实现由Data项目提供

            return services;
        }



        /// <summary>
        /// 添加仓储服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // 这里可以注册具体的仓储实现
            // 例如：services.AddScoped<IUserRepository, UserRepository>();
            
            return services;
        }

        /// <summary>
        /// 添加业务服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // 这里可以注册业务服务
            // 例如：services.AddScoped<IUserService, UserService>();
            
            return services;
        }

        /// <summary>
        /// 添加日志服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddLoggingServices(this IServiceCollection services)
        {
            services.AddLogging();

            return services;
        }


    }
} 