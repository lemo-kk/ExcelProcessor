using ExcelProcessor.Models;
using ExcelProcessor.Data.Database;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// 数据源配置仓储
    /// </summary>
    public class DataSourceConfigRepository : BaseRepository<DataSourceConfig>
    {
        public DataSourceConfigRepository(IDbContext dbContext, ILogger<DataSourceConfigRepository> logger) 
            : base(dbContext, logger)
        {
        }

        protected override string GetTableName()
        {
            return "DataSourceConfig";
        }
    }
} 