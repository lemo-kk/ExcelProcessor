using ExcelProcessor.Data.Database;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// Excel字段映射仓储实现
    /// </summary>
    public class ExcelFieldMappingRepository : BaseRepository<ExcelFieldMapping>
    {
        public ExcelFieldMappingRepository(IDbContext dbContext, ILogger<ExcelFieldMappingRepository> logger) : base(dbContext, logger)
        {
        }

        protected override string GetTableName()
        {
            return "ExcelFieldMappings";
        }
    }
} 