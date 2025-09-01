using ExcelProcessor.Models;
using ExcelProcessor.Data.Repositories;

namespace ExcelProcessor.Core.Repositories
{
    /// <summary>
    /// 用户仓储接口
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>用户对象</returns>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>
        /// 根据邮箱获取用户
        /// </summary>
        /// <param name="email">邮箱</param>
        /// <returns>用户对象</returns>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// 检查用户名是否存在
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>是否存在</returns>
        Task<bool> UsernameExistsAsync(string username);

        /// <summary>
        /// 检查邮箱是否存在
        /// </summary>
        /// <param name="email">邮箱</param>
        /// <returns>是否存在</returns>
        Task<bool> EmailExistsAsync(string email);
    }
} 