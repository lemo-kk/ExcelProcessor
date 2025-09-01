using System.Data;
using System.Data.SQLite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Database
{
    /// <summary>
    /// SQLite数据库上下文
    /// </summary>
    public class SQLiteDbContext : IDbContext
    {
        private readonly string _connectionString;
        private readonly ILogger<SQLiteDbContext> _logger;
        private readonly DatabaseInitializer _initializer;

        public SQLiteDbContext(IConfiguration configuration, ILogger<SQLiteDbContext> logger)
        {
            _logger = logger;
            
            // 从配置文件获取连接字符串，如果没有则使用默认值
            var baseConnectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Data Source=./data/ExcelProcessor.db;";
            
            // 确保连接字符串包含UTF-8编码设置
            if (!baseConnectionString.Contains("Encoding="))
            {
                _connectionString = baseConnectionString.TrimEnd(';') + ";Encoding=UTF8;";
            }
            else
            {
                _connectionString = baseConnectionString;
            }

            // 创建数据库初始化器的日志记录器
            var loggerFactory = LoggerFactory.Create(builder => 
                builder.AddConsole().AddDebug());
            var initializerLogger = loggerFactory.CreateLogger<DatabaseInitializer>();
            _initializer = new DatabaseInitializer(_connectionString, initializerLogger);
        }

        /// <summary>
        /// 获取数据库连接
        /// </summary>
        /// <returns>数据库连接</returns>
        public IDbConnection GetConnection()
        {
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            
            // 启用外键约束
            using var command = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection);
            command.ExecuteNonQuery();
            
            // 设置UTF-8编码
            using var encodingCommand = new SQLiteCommand("PRAGMA encoding = 'UTF-8';", connection);
            encodingCommand.ExecuteNonQuery();
            
            return connection;
        }

        /// <summary>
        /// 获取连接字符串
        /// </summary>
        /// <returns>连接字符串</returns>
        public string GetConnectionString()
        {
            return _connectionString;
        }

        /// <summary>
        /// 开始事务
        /// </summary>
        /// <returns>数据库事务</returns>
        public IDbTransaction BeginTransaction()
        {
            var connection = GetConnection();
            return connection.BeginTransaction();
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        public void Initialize()
        {
            try
            {
                _logger.LogInformation("开始初始化SQLite数据库...");
                _initializer.Initialize();
                _logger.LogInformation("SQLite数据库初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQLite数据库初始化失败");
                throw;
            }
        }
    }
} 