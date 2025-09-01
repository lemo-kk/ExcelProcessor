using System.Data;

namespace ExcelProcessor.Data.Database
{
    /// <summary>
    /// 数据库上下文接口
    /// </summary>
    public interface IDbContext
    {
        /// <summary>
        /// 获取数据库连接
        /// </summary>
        /// <returns>数据库连接</returns>
        IDbConnection GetConnection();

        /// <summary>
        /// 获取连接字符串
        /// </summary>
        /// <returns>连接字符串</returns>
        string GetConnectionString();

        /// <summary>
        /// 开始事务
        /// </summary>
        /// <returns>数据库事务</returns>
        IDbTransaction BeginTransaction();

        /// <summary>
        /// 初始化数据库
        /// </summary>
        void Initialize();
    }
} 