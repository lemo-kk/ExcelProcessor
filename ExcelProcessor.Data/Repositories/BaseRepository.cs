using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;
using Microsoft.Extensions.Logging;
using ExcelProcessor.Data.Database;

namespace ExcelProcessor.Data.Repositories
{
    /// <summary>
    /// 基础仓储实现
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly IDbContext _dbContext;
        protected readonly ILogger<BaseRepository<T>> _logger;
        protected readonly string _tableName;

        protected BaseRepository(IDbContext dbContext, ILogger<BaseRepository<T>> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
            _tableName = GetTableName();
        }

        /// <summary>
        /// 根据ID获取实体
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <returns>实体对象</returns>
        public virtual async Task<T?> GetByIdAsync(object id)
        {
            try
            {
                using var connection = _dbContext.GetConnection();
                var sql = $"SELECT * FROM {_tableName} WHERE [Id] = @Id";
                return await connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取实体失败: {TableName}, Id: {Id}", _tableName, id);
                throw;
            }
        }

        /// <summary>
        /// 获取所有实体
        /// </summary>
        /// <returns>实体列表</returns>
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                using var connection = _dbContext.GetConnection();
                var sql = $"SELECT * FROM {_tableName}";
                return await connection.QueryAsync<T>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有实体失败: {TableName}", _tableName);
                throw;
            }
        }

        /// <summary>
        /// 根据条件查询实体
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <returns>实体列表</returns>
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                // 这里简化实现，实际项目中可以使用更复杂的表达式解析
                using var connection = _dbContext.GetConnection();
                var sql = $"SELECT * FROM {_tableName}";
                var entities = await connection.QueryAsync<T>(sql);
                return entities.AsQueryable().Where(predicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询实体失败: {TableName}", _tableName);
                throw;
            }
        }

        /// <summary>
        /// 添加实体
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <returns>是否成功</returns>
        public virtual async Task<bool> AddAsync(T entity)
        {
            try
            {
                using var connection = _dbContext.GetConnection();
                return await AddAsync(entity, connection, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加实体失败: {TableName}", _tableName);
                throw;
            }
        }

        /// <summary>
        /// 添加实体（使用指定连接和事务）
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="connection">数据库连接</param>
        /// <param name="transaction">数据库事务</param>
        /// <returns>是否成功</returns>
        public virtual async Task<bool> AddAsync(T entity, IDbConnection connection, IDbTransaction transaction = null)
        {
            try
            {
                var sql = GenerateInsertSql();
                var parameters = GetParameters(entity);
                var result = await connection.ExecuteAsync(sql, parameters, transaction);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加实体失败: {TableName}", _tableName);
                throw;
            }
        }

        /// <summary>
        /// 更新实体
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <returns>是否成功</returns>
        public virtual async Task<bool> UpdateAsync(T entity)
        {
            try
            {
                using var connection = _dbContext.GetConnection();
                return await UpdateAsync(entity, connection, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新实体失败: {TableName}", _tableName);
                throw;
            }
        }

        /// <summary>
        /// 更新实体（使用指定连接和事务）
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="connection">数据库连接</param>
        /// <param name="transaction">数据库事务</param>
        /// <returns>是否成功</returns>
        public virtual async Task<bool> UpdateAsync(T entity, IDbConnection connection, IDbTransaction transaction = null)
        {
            try
            {
                var sql = GenerateUpdateSql();
                var parameters = GetParameters(entity);
                var result = await connection.ExecuteAsync(sql, parameters, transaction);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新实体失败: {TableName}", _tableName);
                throw;
            }
        }

        /// <summary>
        /// 删除实体
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <returns>是否成功</returns>
        public virtual async Task<bool> DeleteAsync(T entity)
        {
            try
            {
                var id = GetEntityId(entity);
                return await DeleteByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除实体失败: {TableName}", _tableName);
                throw;
            }
        }

        /// <summary>
        /// 根据ID删除实体
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <returns>是否成功</returns>
        public virtual async Task<bool> DeleteByIdAsync(object id)
        {
            try
            {
                using var connection = _dbContext.GetConnection();
                var sql = $"DELETE FROM {_tableName} WHERE Id = @Id";
                var result = await connection.ExecuteAsync(sql, new { Id = id });
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID删除实体失败: {TableName}, Id: {Id}", _tableName, id);
                throw;
            }
        }

        /// <summary>
        /// 检查实体是否存在
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <returns>是否存在</returns>
        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                var entities = await FindAsync(predicate);
                return entities.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查实体是否存在失败: {TableName}", _tableName);
                throw;
            }
        }

        /// <summary>
        /// 获取实体数量
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <returns>实体数量</returns>
        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            try
            {
                if (predicate == null)
                {
                    using var connection = _dbContext.GetConnection();
                    var sql = $"SELECT COUNT(*) FROM {_tableName}";
                    return await connection.ExecuteScalarAsync<int>(sql);
                }
                else
                {
                    var entities = await FindAsync(predicate);
                    return entities.Count();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取实体数量失败: {TableName}", _tableName);
                throw;
            }
        }

        /// <summary>
        /// 获取表名
        /// </summary>
        /// <returns>表名</returns>
        protected virtual string GetTableName()
        {
            var type = typeof(T);
            return type.Name;
        }

        /// <summary>
        /// 生成插入SQL
        /// </summary>
        /// <returns>插入SQL</returns>
        protected virtual string GenerateInsertSql()
        {
            var properties = GetProperties().Where(p => p.Name.ToLower() != "id");
            var columns = string.Join(", ", properties.Select(p => $"[{p.Name}]"));
            var parameters = string.Join(", ", properties.Select(p => "@" + p.Name));
            return $"INSERT INTO {_tableName} ({columns}) VALUES ({parameters})";
        }

        /// <summary>
        /// 生成更新SQL
        /// </summary>
        /// <returns>更新SQL</returns>
        protected virtual string GenerateUpdateSql()
        {
            var properties = GetProperties().Where(p => p.Name.ToLower() != "id");
            var setClause = string.Join(", ", properties.Select(p => $"[{p.Name}] = @{p.Name}"));
            return $"UPDATE {_tableName} SET {setClause} WHERE [Id] = @Id";
        }

        /// <summary>
        /// 获取实体属性
        /// </summary>
        /// <returns>属性列表</returns>
        protected virtual PropertyInfo[] GetProperties()
        {
            return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => !IsNavigationProperty(p))
                .ToArray();
        }

        /// <summary>
        /// 判断是否为导航属性
        /// </summary>
        /// <param name="property">属性信息</param>
        /// <returns>是否为导航属性</returns>
        protected virtual bool IsNavigationProperty(PropertyInfo property)
        {
            // 排除常见的导航属性
            var navigationPropertyNames = new[] { "Permissions", "Users", "Roles", "UserRoles", "RolePermissions", "UserPermissions" };
            return navigationPropertyNames.Contains(property.Name);
        }

        /// <summary>
        /// 获取实体参数
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <returns>参数字典</returns>
        protected virtual object GetParameters(T entity)
        {
            var parameters = new Dictionary<string, object>();
            var properties = GetProperties();

            foreach (var property in properties)
            {
                var value = property.GetValue(entity);
                
                // 处理null值 - 对于Dapper，直接传递null而不是DBNull.Value
                if (value == null)
                {
                    // 对于所有null值，直接传递null，让Dapper自动处理
                    parameters[property.Name] = null;
                }
                else
                {
                    parameters[property.Name] = value;
                }
            }

            return parameters;
        }

        /// <summary>
        /// 获取实体ID
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <returns>实体ID</returns>
        protected virtual object GetEntityId(T entity)
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null)
            {
                throw new InvalidOperationException($"实体 {typeof(T).Name} 没有找到 Id 属性");
            }
            return idProperty.GetValue(entity) ?? throw new InvalidOperationException("实体ID不能为空");
        }
    }
} 