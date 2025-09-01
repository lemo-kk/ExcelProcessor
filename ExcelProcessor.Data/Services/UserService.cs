using ExcelProcessor.Core.Services;
using ExcelProcessor.Data.Repositories;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// 用户管理服务实现
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IRepository<User> _userRepository;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IRepository<User> userRepository,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                _logger.LogInformation("获取所有用户");
                return await _userRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有用户失败");
                throw;
            }
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("根据ID获取用户: {UserId}", id);
                return await _userRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取用户失败: {UserId}", id);
                throw;
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                _logger.LogInformation("根据用户名获取用户: {Username}", username);
                var users = await _userRepository.FindAsync(u => u.Username == username);
                return users.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户名获取用户失败: {Username}", username);
                throw;
            }
        }

        public async Task<User> CreateUserAsync(User user, string password)
        {
            try
            {
                _logger.LogInformation("创建用户: {Username}", user.Username);

                // 检查用户名是否已存在
                var existingUser = await GetUserByUsernameAsync(user.Username);
                if (existingUser != null)
                {
                    throw new InvalidOperationException($"用户名 '{user.Username}' 已存在");
                }

                // 加密密码
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                user.CreatedTime = DateTime.Now;
                user.UpdatedTime = DateTime.Now;

                var result = await _userRepository.AddAsync(user);
                _logger.LogInformation("用户创建成功: {Username}", user.Username);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败: {Username}", user.Username);
                throw;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                _logger.LogInformation("更新用户: {UserId}", user.Id);

                // 检查用户是否存在
                var existingUser = await GetUserByIdAsync(user.Id);
                if (existingUser == null)
                {
                    throw new InvalidOperationException($"用户不存在: {user.Id}");
                }

                // 检查用户名是否被其他用户使用
                var usersWithSameUsername = await _userRepository.FindAsync(u => u.Username == user.Username && u.Id != user.Id);
                if (usersWithSameUsername.Any())
                {
                    throw new InvalidOperationException($"用户名 '{user.Username}' 已被其他用户使用");
                }

                user.UpdatedTime = DateTime.Now;
                user.PasswordHash = existingUser.PasswordHash; // 保持原密码

                var result = await _userRepository.UpdateAsync(user);
                _logger.LogInformation("用户更新成功: {UserId}", user.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户失败: {UserId}", user.Id);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                _logger.LogInformation("删除用户: {UserId}", id);

                // 检查用户是否存在
                var existingUser = await GetUserByIdAsync(id);
                if (existingUser == null)
                {
                    throw new InvalidOperationException($"用户不存在: {id}");
                }

                // 删除用户
                var result = await _userRepository.DeleteByIdAsync(id);
                _logger.LogInformation("用户删除成功: {UserId}", id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败: {UserId}", id);
                throw;
            }
        }

        public async Task<bool> ValidatePasswordAsync(string username, string password)
        {
            try
            {
                _logger.LogInformation("验证用户密码: {Username}", username);

                var user = await GetUserByUsernameAsync(username);
                if (user == null)
                {
                    return false;
                }

                var isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                _logger.LogInformation("密码验证结果: {Username} - {IsValid}", username, isValid);

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户密码失败: {Username}", username);
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            try
            {
                _logger.LogInformation("修改用户密码: {UserId}", userId);

                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException($"用户不存在: {userId}");
                }

                // 验证旧密码
                if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
                {
                    throw new InvalidOperationException("旧密码不正确");
                }

                // 加密新密码
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.Now;

                var result = await _userRepository.UpdateAsync(user);
                _logger.LogInformation("密码修改成功: {UserId}", userId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改用户密码失败: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            try
            {
                _logger.LogInformation("重置用户密码: {UserId}", userId);

                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException($"用户不存在: {userId}");
                }

                // 加密新密码
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.Now;

                var result = await _userRepository.UpdateAsync(user);
                _logger.LogInformation("密码重置成功: {UserId}", userId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置用户密码失败: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Role>> GetUserRolesAsync(int userId)
        {
            // 简化实现，暂时返回空集合
            return Enumerable.Empty<Role>();
        }

        public async Task<bool> AssignUserRolesAsync(int userId, IEnumerable<int> roleIds)
        {
            // 简化实现，暂时返回true
            return true;
        }

        public async Task<IEnumerable<Permission>> GetUserPermissionsAsync(int userId)
        {
            // 简化实现，暂时返回空集合
            return Enumerable.Empty<Permission>();
        }

        public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
        {
            // 简化实现，暂时返回true
            return true;
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string keyword)
        {
            try
            {
                _logger.LogInformation("搜索用户: {Keyword}", keyword);

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return await GetAllUsersAsync();
                }

                var users = await _userRepository.FindAsync(u => 
                    u.Username.Contains(keyword) || 
                    (u.Email != null && u.Email.Contains(keyword)) || 
                    u.DisplayName.Contains(keyword));

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索用户失败: {Keyword}", keyword);
                throw;
            }
        }
    }
} 