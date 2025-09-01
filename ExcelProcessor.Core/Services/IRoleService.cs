using ExcelProcessor.Models;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 角色管理服务接口
    /// </summary>
    public interface IRoleService
    {
        /// <summary>
        /// 获取所有角色
        /// </summary>
        Task<IEnumerable<Role>> GetAllRolesAsync();

        /// <summary>
        /// 根据ID获取角色
        /// </summary>
        Task<Role?> GetRoleByIdAsync(int id);

        /// <summary>
        /// 根据代码获取角色
        /// </summary>
        Task<Role?> GetRoleByCodeAsync(string code);

        /// <summary>
        /// 创建角色
        /// </summary>
        Task<Role> CreateRoleAsync(Role role);

        /// <summary>
        /// 更新角色
        /// </summary>
        Task<bool> UpdateRoleAsync(Role role);

        /// <summary>
        /// 删除角色
        /// </summary>
        Task<bool> DeleteRoleAsync(int id);

        /// <summary>
        /// 获取角色权限
        /// </summary>
        Task<IEnumerable<Permission>> GetRolePermissionsAsync(int roleId);

        /// <summary>
        /// 分配角色权限
        /// </summary>
        Task<bool> AssignRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds);

        /// <summary>
        /// 获取角色用户
        /// </summary>
        Task<IEnumerable<User>> GetRoleUsersAsync(int roleId);

        /// <summary>
        /// 搜索角色
        /// </summary>
        Task<IEnumerable<Role>> SearchRolesAsync(string keyword);
    }
} 