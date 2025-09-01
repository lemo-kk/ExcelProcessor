using ExcelProcessor.Data.Database;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// Excel配置仓储实现
    /// </summary>
    public class ExcelConfigRepository : BaseRepository<ExcelConfig>
    {
        public ExcelConfigRepository(IDbContext dbContext, ILogger<ExcelConfigRepository> logger) : base(dbContext, logger)
        {
        }

        protected override string GetTableName()
        {
            return "ExcelConfigs";
        }
    }
} 