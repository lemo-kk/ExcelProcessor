using ExcelProcessor.Models;
using ExcelProcessor.Data.Database;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// SQL配置仓储实现
    /// </summary>
    public class SqlConfigRepository : BaseRepository<SqlConfig>
    {
        public SqlConfigRepository(IDbContext dbContext, ILogger<SqlConfigRepository> logger) 
            : base(dbContext, logger)
        {
        }

        protected override string GetTableName()
        {
            return "SqlConfigs";
        }

        /// <summary>
        /// 重写获取属性方法，只包含数据库表中实际存在的列
        /// </summary>
        /// <returns>属性列表</returns>
        protected override PropertyInfo[] GetProperties()
        {
            return typeof(SqlConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && 
                           p.Name != "DataSource" && p.Name != "CreatedByUser" && p.Name != "LastModifiedByUser") // 排除导航属性
                .ToArray();
        }
    }
} 
 
 