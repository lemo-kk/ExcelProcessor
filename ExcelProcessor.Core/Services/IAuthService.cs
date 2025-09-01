using ExcelProcessor.Models;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 认证服务接口
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// 用户登录
        /// </summary>
        Task<AuthResult> LoginAsync(string username, string password);

        /// <summary>
        /// 用户登出
        /// </summary>
        Task<bool> LogoutAsync(int userId);

        /// <summary>
        /// 验证用户会话
        /// </summary>
        Task<bool> ValidateSessionAsync(int userId);

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        Task<User?> GetCurrentUserAsync(int userId);

        /// <summary>
        /// 检查用户是否有指定权限
        /// </summary>
        Task<bool> HasPermissionAsync(int userId, string permissionCode);

        /// <summary>
        /// 获取用户所有权限
        /// </summary>
        Task<IEnumerable<string>> GetUserPermissionCodesAsync(int userId);

        /// <summary>
        /// 刷新用户权限缓存
        /// </summary>
        Task RefreshUserPermissionsAsync(int userId);

        /// <summary>
        /// 修改用户密码
        /// </summary>
        Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword);
    }

    /// <summary>
    /// 认证结果
    /// </summary>
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public User? User { get; set; }
        public string? Token { get; set; }
    }
} 