using ExcelProcessor.Models;
using ExcelProcessor.Data.Repositories;

namespace ExcelProcessor.Core.Repositories
{
    /// <summary>
    /// 角色仓储接口
    /// </summary>
    public interface IRoleRepository : IRepository<Role>
    {
        /// <summary>
        /// 根据角色名称获取角色
        /// </summary>
        /// <param name="roleName">角色名称</param>
        /// <returns>角色对象</returns>
        Task<Role?> GetByNameAsync(string roleName);

        /// <summary>
        /// 检查角色名称是否存在
        /// </summary>
        /// <param name="roleName">角色名称</param>
        /// <returns>是否存在</returns>
        Task<bool> NameExistsAsync(string roleName);

        /// <summary>
        /// 获取用户的所有角色
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>角色列表</returns>
        Task<IEnumerable<Role>> GetUserRolesAsync(int userId);

        /// <summary>
        /// 获取角色的所有权限
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <returns>权限列表</returns>
        Task<IEnumerable<Permission>> GetRolePermissionsAsync(int roleId);
    }
} 