using ExcelProcessor.Models;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 权限管理服务接口
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// 获取所有权限
        /// </summary>
        Task<IEnumerable<Permission>> GetAllPermissionsAsync();

        /// <summary>
        /// 根据ID获取权限
        /// </summary>
        Task<Permission?> GetPermissionByIdAsync(int id);

        /// <summary>
        /// 根据代码获取权限
        /// </summary>
        Task<Permission?> GetPermissionByCodeAsync(string code);

        /// <summary>
        /// 创建权限
        /// </summary>
        Task<Permission> CreatePermissionAsync(Permission permission);

        /// <summary>
        /// 更新权限
        /// </summary>
        Task<bool> UpdatePermissionAsync(Permission permission);

        /// <summary>
        /// 删除权限
        /// </summary>
        Task<bool> DeletePermissionAsync(int id);

        /// <summary>
        /// 获取权限角色
        /// </summary>
        Task<IEnumerable<Role>> GetPermissionRolesAsync(int permissionId);

        /// <summary>
        /// 获取权限用户
        /// </summary>
        Task<IEnumerable<User>> GetPermissionUsersAsync(int permissionId);

        /// <summary>
        /// 按分类获取权限
        /// </summary>
        Task<IEnumerable<Permission>> GetPermissionsByCategoryAsync(string category);

        /// <summary>
        /// 搜索权限
        /// </summary>
        Task<IEnumerable<Permission>> SearchPermissionsAsync(string keyword);

        /// <summary>
        /// 获取所有权限分类
        /// </summary>
        Task<IEnumerable<string>> GetAllPermissionCategoriesAsync();
    }
} 