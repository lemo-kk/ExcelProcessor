using ExcelProcessor.Core.Services;
using ExcelProcessor.Data.Repositories;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// 认证服务实现
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IRepository<User> _userRepository;
        private readonly ISystemConfigService _systemConfigService;
        private readonly ILogger<AuthService> _logger;
        private User? _currentUser;

        public AuthService(IRepository<User> userRepository, ISystemConfigService systemConfigService, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _systemConfigService = systemConfigService;
            _logger = logger;
        }

        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            try
            {
                _logger.LogInformation("用户登录: {Username}", username);

                // 检查系统是否启用登录
                var loginEnabled = await _systemConfigService.IsLoginEnabledAsync();
                if (!loginEnabled)
                {
                    _logger.LogInformation("系统未启用登录验证，自动登录成功");
                    
                    // 创建虚拟超级管理员用户
                    var virtualUser = new User
                    {
                        Id = -1, // 虚拟用户ID
                        Username = "system_admin",
                        DisplayName = "系统管理员",
                        Email = "admin@system.local",
                        IsEnabled = true,
                        Role = UserRole.SuperAdmin,
                        Status = UserStatus.Active,
                        CreatedTime = DateTime.Now,
                        UpdatedTime = DateTime.Now,
                        LastLoginTime = DateTime.Now
                    };

                    _currentUser = virtualUser;

                    return new AuthResult
                    {
                        Success = true,
                        Message = "系统未启用登录验证，自动登录成功",
                        User = virtualUser
                    };
                }

                // 查找用户
                var users = await _userRepository.FindAsync(u => u.Username == username);
                var user = users.FirstOrDefault();

                if (user == null)
                {
                    _logger.LogWarning("用户不存在: {Username}", username);
                    return new AuthResult
                    {
                        Success = false,
                        Message = "用户名或密码错误"
                    };
                }

                // 检查用户是否启用
                if (!user.IsEnabled)
                {
                    _logger.LogWarning("用户已禁用: {Username}", username);
                    return new AuthResult
                    {
                        Success = false,
                        Message = "用户账户已禁用"
                    };
                }

                // 验证密码
                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    _logger.LogWarning("密码验证失败: {Username}", username);
                    return new AuthResult
                    {
                        Success = false,
                        Message = "用户名或密码错误"
                    };
                }

                // 更新最后登录时间
                user.LastLoginTime = DateTime.Now;
                user.UpdatedTime = DateTime.Now;
                await _userRepository.UpdateAsync(user);

                // 设置当前用户
                _currentUser = user;

                _logger.LogInformation("用户登录成功: {Username}", username);

                return new AuthResult
                {
                    Success = true,
                    Message = "登录成功",
                    User = user
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户登录失败: {Username}", username);
                return new AuthResult
                {
                    Success = false,
                    Message = "登录过程中发生错误"
                };
            }
        }

        public async Task<bool> LogoutAsync(int userId)
        {
            try
            {
                _logger.LogInformation("用户登出: {UserId}", userId);
                
                // 检查系统是否启用登录
                var loginEnabled = await _systemConfigService.IsLoginEnabledAsync();
                if (!loginEnabled)
                {
                    _logger.LogInformation("系统未启用登录验证，无需登出");
                    return true;
                }

                // TODO: 实现用户登出逻辑
                _currentUser = null;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户登出失败: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ValidateSessionAsync(int userId)
        {
            try
            {
                // 检查系统是否启用登录
                var loginEnabled = await _systemConfigService.IsLoginEnabledAsync();
                if (!loginEnabled)
                {
                    _logger.LogInformation("系统未启用登录验证，会话始终有效");
                    return true;
                }

                if (_currentUser == null || _currentUser.Id != userId)
                {
                    return false;
                }

                // TODO: 检查会话是否过期
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户会话失败: {UserId}", userId);
                return false;
            }
        }

        public async Task<User?> GetCurrentUserAsync(int userId)
        {
            try
            {
                // 检查系统是否启用登录
                var loginEnabled = await _systemConfigService.IsLoginEnabledAsync();
                if (!loginEnabled)
                {
                    // 返回虚拟超级管理员用户
                    return new User
                    {
                        Id = -1,
                        Username = "system_admin",
                        DisplayName = "系统管理员",
                        Email = "admin@system.local",
                        IsEnabled = true,
                        Role = UserRole.SuperAdmin,
                        Status = UserStatus.Active,
                        CreatedTime = DateTime.Now,
                        UpdatedTime = DateTime.Now,
                        LastLoginTime = DateTime.Now
                    };
                }

                if (_currentUser != null && _currentUser.Id == userId)
                {
                    return _currentUser;
                }

                var users = await _userRepository.FindAsync(u => u.Id == userId);
                return users.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取当前用户失败: {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
        {
            try
            {
                // 检查系统是否启用登录
                var loginEnabled = await _systemConfigService.IsLoginEnabledAsync();
                if (!loginEnabled)
                {
                    _logger.LogInformation("系统未启用登录验证，自动授予所有权限");
                    return true; // 未启用登录时，自动给予最高权限
                }

                // TODO: 实现权限检查逻辑
                _logger.LogInformation("检查用户权限: {UserId}, {PermissionCode}", userId, permissionCode);
                return true; // 临时返回true，后续实现具体权限检查
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查用户权限失败: {UserId}, {PermissionCode}", userId, permissionCode);
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetUserPermissionCodesAsync(int userId)
        {
            try
            {
                // 检查系统是否启用登录
                var loginEnabled = await _systemConfigService.IsLoginEnabledAsync();
                if (!loginEnabled)
                {
                    _logger.LogInformation("系统未启用登录验证，返回所有权限");
                    return new List<string> { "*" }; // 返回通配符表示所有权限
                }

                // TODO: 实现获取用户权限代码列表
                _logger.LogInformation("获取用户权限代码: {UserId}", userId);
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户权限代码失败: {UserId}", userId);
                return new List<string>();
            }
        }

        public async Task RefreshUserPermissionsAsync(int userId)
        {
            try
            {
                // 检查系统是否启用登录
                var loginEnabled = await _systemConfigService.IsLoginEnabledAsync();
                if (!loginEnabled)
                {
                    _logger.LogInformation("系统未启用登录验证，无需刷新权限缓存");
                    return;
                }

                // TODO: 实现刷新用户权限缓存
                _logger.LogInformation("刷新用户权限缓存: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新用户权限缓存失败: {UserId}", userId);
            }
        }

        public async Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword)
        {
            try
            {
                // 检查系统是否启用登录
                var loginEnabled = await _systemConfigService.IsLoginEnabledAsync();
                if (!loginEnabled)
                {
                    _logger.LogInformation("系统未启用登录验证，无需修改密码");
                    return true;
                }

                _logger.LogInformation("修改密码: {Username}", username);

                var users = await _userRepository.FindAsync(u => u.Username == username);
                var user = users.FirstOrDefault();

                if (user == null)
                {
                    _logger.LogWarning("用户不存在: {Username}", username);
                    return false;
                }

                // 验证旧密码
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    _logger.LogWarning("旧密码验证失败: {Username}", username);
                    return false;
                }

                // 加密新密码
                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.PasswordHash = newPasswordHash;
                user.UpdatedTime = DateTime.Now;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("密码修改成功: {Username}", username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改密码失败: {Username}", username);
                return false;
            }
        }
    }
} 