using ExcelProcessor.Core.Services;
using ExcelProcessor.Data.Repositories;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// 角色服务实现
    /// </summary>
    public class RoleService : IRoleService
    {
        private readonly IRepository<Role> _roleRepository;
        private readonly ILogger<RoleService> _logger;

        public RoleService(IRepository<Role> roleRepository, ILogger<RoleService> logger)
        {
            _roleRepository = roleRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            try
            {
                _logger.LogInformation("获取所有角色");
                return await _roleRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有角色失败");
                throw;
            }
        }

        public async Task<Role?> GetRoleByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("根据ID获取角色: {RoleId}", id);
                return await _roleRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取角色失败: {RoleId}", id);
                throw;
            }
        }

        public async Task<Role?> GetRoleByCodeAsync(string code)
        {
            try
            {
                _logger.LogInformation("根据代码获取角色: {RoleCode}", code);
                var roles = await _roleRepository.FindAsync(r => r.Code == code);
                return roles.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据代码获取角色失败: {RoleCode}", code);
                throw;
            }
        }

        public async Task<Role> CreateRoleAsync(Role role)
        {
            try
            {
                _logger.LogInformation("创建角色: {RoleCode}", role.Code);

                // 检查角色代码是否已存在
                var existingRole = await GetRoleByCodeAsync(role.Code);
                if (existingRole != null)
                {
                    throw new InvalidOperationException($"角色代码 '{role.Code}' 已存在");
                }

                role.CreatedTime = DateTime.Now;
                role.UpdatedTime = DateTime.Now;

                var result = await _roleRepository.AddAsync(role);
                _logger.LogInformation("角色创建成功: {RoleCode}", role.Code);

                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建角色失败: {RoleCode}", role.Code);
                throw;
            }
        }

        public async Task<bool> UpdateRoleAsync(Role role)
        {
            try
            {
                _logger.LogInformation("更新角色: {RoleId}", role.Id);

                // 检查角色是否存在
                var existingRole = await GetRoleByIdAsync(role.Id);
                if (existingRole == null)
                {
                    throw new InvalidOperationException($"角色不存在: {role.Id}");
                }

                // 检查角色代码是否被其他角色使用
                var rolesWithSameCode = await _roleRepository.FindAsync(r => r.Code == role.Code && r.Id != role.Id);
                if (rolesWithSameCode.Any())
                {
                    throw new InvalidOperationException($"角色代码 '{role.Code}' 已被其他角色使用");
                }

                role.UpdatedTime = DateTime.Now;

                var result = await _roleRepository.UpdateAsync(role);
                _logger.LogInformation("角色更新成功: {RoleId}", role.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新角色失败: {RoleId}", role.Id);
                throw;
            }
        }

        public async Task<bool> DeleteRoleAsync(int id)
        {
            try
            {
                _logger.LogInformation("删除角色: {RoleId}", id);

                // 检查角色是否存在
                var existingRole = await GetRoleByIdAsync(id);
                if (existingRole == null)
                {
                    throw new InvalidOperationException($"角色不存在: {id}");
                }

                // 删除角色
                var result = await _roleRepository.DeleteByIdAsync(id);
                _logger.LogInformation("角色删除成功: {RoleId}", id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除角色失败: {RoleId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Role>> SearchRolesAsync(string keyword)
        {
            try
            {
                _logger.LogInformation("搜索角色: {Keyword}", keyword);
                var roles = await _roleRepository.FindAsync(r => 
                    r.Name.Contains(keyword) || r.Code.Contains(keyword) || 
                    (r.Description != null && r.Description.Contains(keyword)));
                return roles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索角色失败: {Keyword}", keyword);
                throw;
            }
        }

        // 简化实现，暂时返回空集合
        public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(int roleId)
        {
            try
            {
                _logger.LogInformation("获取角色权限: {RoleId}", roleId);
                // TODO: 实现角色权限查询
                return new List<Permission>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色权限失败: {RoleId}", roleId);
                throw;
            }
        }

        // 简化实现，暂时返回true
        public async Task<bool> AssignRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds)
        {
            try
            {
                _logger.LogInformation("分配角色权限: {RoleId}, 权限数量: {PermissionCount}", roleId, permissionIds.Count());
                // TODO: 实现角色权限分配
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分配角色权限失败: {RoleId}", roleId);
                throw;
            }
        }

        // 简化实现，暂时返回空集合
        public async Task<IEnumerable<User>> GetRoleUsersAsync(int roleId)
        {
            try
            {
                _logger.LogInformation("获取角色用户: {RoleId}", roleId);
                // TODO: 实现角色用户查询
                return new List<User>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色用户失败: {RoleId}", roleId);
                throw;
            }
        }
    }
} 