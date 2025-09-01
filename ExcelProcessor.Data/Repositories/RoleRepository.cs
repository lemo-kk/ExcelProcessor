using ExcelProcessor.Data.Database;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// 角色仓储实现
    /// </summary>
    public class RoleRepository : BaseRepository<Role>, IRepository<Role>
    {
        public RoleRepository(IDbContext context, ILogger<RoleRepository> logger) : base(context, logger)
        {
        }

        protected override string GetTableName()
        {
            return "Roles";
        }
    }
} 