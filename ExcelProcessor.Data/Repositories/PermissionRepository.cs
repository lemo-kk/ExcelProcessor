using ExcelProcessor.Models;
using ExcelProcessor.Data.Database;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// 权限仓储实现
    /// </summary>
    public class PermissionRepository : BaseRepository<Permission>
    {
        public PermissionRepository(IDbContext dbContext, ILogger<PermissionRepository> logger) 
            : base(dbContext, logger)
        {
        }

        protected override string GetTableName()
        {
            return "Permissions";
        }

        /// <summary>
        /// 重写获取属性方法，只包含数据库表中实际存在的列
        /// </summary>
        /// <returns>属性列表</returns>
        protected override PropertyInfo[] GetProperties()
        {
            return typeof(Permission).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && 
                           p.Name != "CreatedAt" && p.Name != "UpdatedAt" && 
                           p.Name != "Roles" && p.Name != "Users") // 排除导航属性
                .ToArray();
        }
    }
} 