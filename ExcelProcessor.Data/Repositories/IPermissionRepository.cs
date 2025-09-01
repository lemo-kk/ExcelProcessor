using ExcelProcessor.Models;
using ExcelProcessor.Data.Repositories;

namespace ExcelProcessor.Core.Repositories
{
    /// <summary>
    /// 权限仓储接口
    /// </summary>
    public interface IPermissionRepository : IRepository<Permission>
    {
        /// <summary>
        /// 根据权限代码获取权限
        /// </summary>
        /// <param name="permissionCode">权限代码</param>
        /// <returns>权限对象</returns>
        Task<Permission?> GetByCodeAsync(string permissionCode);

        /// <summary>
        /// 检查权限代码是否存在
        /// </summary>
        /// <param name="permissionCode">权限代码</param>
        /// <returns>是否存在</returns>
        Task<bool> CodeExistsAsync(string permissionCode);

        /// <summary>
        /// 获取用户的所有权限
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>权限列表</returns>
        Task<IEnumerable<Permission>> GetUserPermissionsAsync(int userId);

        /// <summary>
        /// 获取角色的所有权限
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <returns>权限列表</returns>
        Task<IEnumerable<Permission>> GetRolePermissionsAsync(int roleId);

        /// <summary>
        /// 获取权限树形结构
        /// </summary>
        /// <returns>权限树形结构</returns>
        Task<IEnumerable<Permission>> GetPermissionTreeAsync();
    }
} 