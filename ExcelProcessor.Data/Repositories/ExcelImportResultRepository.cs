using ExcelProcessor.Data.Database;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// Excel导入结果仓储实现
    /// </summary>
    public class ExcelImportResultRepository : BaseRepository<ExcelImportResult>
    {
        public ExcelImportResultRepository(IDbContext dbContext, ILogger<ExcelImportResultRepository> logger) : base(dbContext, logger)
        {
        }

        protected override string GetTableName()
        {
            return "ExcelImportResults";
        }
    }
} 