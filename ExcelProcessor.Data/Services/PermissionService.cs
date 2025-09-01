using ExcelProcessor.Core.Services;
using ExcelProcessor.Data.Repositories;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// 权限服务实现
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly IRepository<Permission> _permissionRepository;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(IRepository<Permission> permissionRepository, ILogger<PermissionService> logger)
        {
            _permissionRepository = permissionRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<Permission>> GetAllPermissionsAsync()
        {
            try
            {
                _logger.LogInformation("获取所有权限");
                return await _permissionRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有权限失败");
                throw;
            }
        }

        public async Task<Permission?> GetPermissionByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("根据ID获取权限: {PermissionId}", id);
                return await _permissionRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取权限失败: {PermissionId}", id);
                throw;
            }
        }

        public async Task<Permission?> GetPermissionByCodeAsync(string code)
        {
            try
            {
                _logger.LogInformation("根据代码获取权限: {PermissionCode}", code);
                var permissions = await _permissionRepository.FindAsync(p => p.Code == code);
                return permissions.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据代码获取权限失败: {PermissionCode}", code);
                throw;
            }
        }

        public async Task<Permission> CreatePermissionAsync(Permission permission)
        {
            try
            {
                _logger.LogInformation("创建权限: {PermissionCode}", permission.Code);

                // 检查权限代码是否已存在
                var existingPermission = await GetPermissionByCodeAsync(permission.Code);
                if (existingPermission != null)
                {
                    throw new InvalidOperationException($"权限代码 '{permission.Code}' 已存在");
                }

                permission.CreatedTime = DateTime.Now;
                permission.UpdatedTime = DateTime.Now;

                var result = await _permissionRepository.AddAsync(permission);
                _logger.LogInformation("权限创建成功: {PermissionCode}", permission.Code);

                return permission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建权限失败: {PermissionCode}", permission.Code);
                throw;
            }
        }

        public async Task<bool> UpdatePermissionAsync(Permission permission)
        {
            try
            {
                _logger.LogInformation("更新权限: {PermissionId}", permission.Id);

                // 检查权限是否存在
                var existingPermission = await GetPermissionByIdAsync(permission.Id);
                if (existingPermission == null)
                {
                    throw new InvalidOperationException($"权限不存在: {permission.Id}");
                }

                // 检查权限代码是否被其他权限使用
                var permissionsWithSameCode = await _permissionRepository.FindAsync(p => p.Code == permission.Code && p.Id != permission.Id);
                if (permissionsWithSameCode.Any())
                {
                    throw new InvalidOperationException($"权限代码 '{permission.Code}' 已被其他权限使用");
                }

                permission.UpdatedTime = DateTime.Now;

                var result = await _permissionRepository.UpdateAsync(permission);
                _logger.LogInformation("权限更新成功: {PermissionId}", permission.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新权限失败: {PermissionId}", permission.Id);
                throw;
            }
        }

        public async Task<bool> DeletePermissionAsync(int id)
        {
            try
            {
                _logger.LogInformation("删除权限: {PermissionId}", id);

                // 检查权限是否存在
                var existingPermission = await GetPermissionByIdAsync(id);
                if (existingPermission == null)
                {
                    throw new InvalidOperationException($"权限不存在: {id}");
                }

                // 删除权限
                var result = await _permissionRepository.DeleteByIdAsync(id);
                _logger.LogInformation("权限删除成功: {PermissionId}", id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除权限失败: {PermissionId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Permission>> GetPermissionsByCategoryAsync(string category)
        {
            try
            {
                _logger.LogInformation("根据分类获取权限: {Category}", category);
                var permissions = await _permissionRepository.FindAsync(p => p.Category == category);
                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据分类获取权限失败: {Category}", category);
                throw;
            }
        }

        public async Task<IEnumerable<Permission>> SearchPermissionsAsync(string keyword)
        {
            try
            {
                _logger.LogInformation("搜索权限: {Keyword}", keyword);
                var permissions = await _permissionRepository.FindAsync(p => 
                    p.Name.Contains(keyword) || p.Code.Contains(keyword) || 
                    p.Category.Contains(keyword) || 
                    (p.Description != null && p.Description.Contains(keyword)));
                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索权限失败: {Keyword}", keyword);
                throw;
            }
        }

        // 简化实现，暂时返回空集合
        public async Task<IEnumerable<Role>> GetPermissionRolesAsync(int permissionId)
        {
            try
            {
                _logger.LogInformation("获取权限角色: {PermissionId}", permissionId);
                // TODO: 实现权限角色查询
                return new List<Role>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取权限角色失败: {PermissionId}", permissionId);
                throw;
            }
        }

        // 简化实现，暂时返回空集合
        public async Task<IEnumerable<User>> GetPermissionUsersAsync(int permissionId)
        {
            try
            {
                _logger.LogInformation("获取权限用户: {PermissionId}", permissionId);
                // TODO: 实现权限用户查询
                return new List<User>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取权限用户失败: {PermissionId}", permissionId);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetAllPermissionCategoriesAsync()
        {
            try
            {
                _logger.LogInformation("获取所有权限分类");
                var permissions = await _permissionRepository.GetAllAsync();
                var categories = permissions.Select(p => p.Category).Distinct().ToList();
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有权限分类失败");
                throw;
            }
        }
    }
} 