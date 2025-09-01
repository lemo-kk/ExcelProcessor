using ExcelProcessor.Models;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 用户管理服务接口
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 获取所有用户
        /// </summary>
        Task<IEnumerable<User>> GetAllUsersAsync();

        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        Task<User?> GetUserByIdAsync(int id);

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        Task<User?> GetUserByUsernameAsync(string username);

        /// <summary>
        /// 创建用户
        /// </summary>
        Task<User> CreateUserAsync(User user, string password);

        /// <summary>
        /// 更新用户
        /// </summary>
        Task<bool> UpdateUserAsync(User user);

        /// <summary>
        /// 删除用户
        /// </summary>
        Task<bool> DeleteUserAsync(int id);

        /// <summary>
        /// 验证用户密码
        /// </summary>
        Task<bool> ValidatePasswordAsync(string username, string password);

        /// <summary>
        /// 修改用户密码
        /// </summary>
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);

        /// <summary>
        /// 重置用户密码
        /// </summary>
        Task<bool> ResetPasswordAsync(int userId, string newPassword);

        /// <summary>
        /// 获取用户角色
        /// </summary>
        Task<IEnumerable<Role>> GetUserRolesAsync(int userId);

        /// <summary>
        /// 分配用户角色
        /// </summary>
        Task<bool> AssignUserRolesAsync(int userId, IEnumerable<int> roleIds);

        /// <summary>
        /// 获取用户权限
        /// </summary>
        Task<IEnumerable<Permission>> GetUserPermissionsAsync(int userId);

        /// <summary>
        /// 检查用户是否有指定权限
        /// </summary>
        Task<bool> HasPermissionAsync(int userId, string permissionCode);

        /// <summary>
        /// 搜索用户
        /// </summary>
        Task<IEnumerable<User>> SearchUsersAsync(string keyword);
    }
} 