using System;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Database
{
    /// <summary>
    /// 数据库迁移工具
    /// </summary>
    public class DatabaseMigration
    {
        private readonly ILogger<DatabaseMigration> _logger;
        private readonly string _connectionString;

        public DatabaseMigration(string connectionString, ILogger<DatabaseMigration> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        /// <summary>
        /// 执行数据库迁移
        /// </summary>
        public void Migrate()
        {
            try
            {
                _logger.LogInformation("开始执行数据库迁移...");

                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();

                // 迁移数据源配置表
                MigrateDataSourceConfigTable(connection);

                // 迁移Excel配置表
                MigrateExcelConfigTable(connection);

                _logger.LogInformation("数据库迁移完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库迁移失败");
                throw;
            }
        }

        /// <summary>
        /// 迁移数据源配置表
        /// </summary>
        private void MigrateDataSourceConfigTable(SQLiteConnection connection)
        {
            try
            {
                // 检查IsDefault列是否存在
                var checkColumnSql = @"
                    SELECT COUNT(*) 
                    FROM pragma_table_info('DataSourceConfig') 
                    WHERE name = 'IsDefault'";

                using var checkCommand = new SQLiteCommand(checkColumnSql, connection);
                var columnExists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;

                if (!columnExists)
                {
                    _logger.LogInformation("为DataSourceConfig表添加IsDefault列");

                    // 添加IsDefault列
                    var addColumnSql = @"
                        ALTER TABLE DataSourceConfig 
                        ADD COLUMN IsDefault INTEGER NOT NULL DEFAULT 0";

                    using var addCommand = new SQLiteCommand(addColumnSql, connection);
                    addCommand.ExecuteNonQuery();

                    _logger.LogInformation("IsDefault列添加成功");
                }
                else
                {
                    _logger.LogInformation("IsDefault列已存在，跳过迁移");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "迁移数据源配置表失败");
                throw;
            }
        }

        /// <summary>
        /// 迁移Excel配置表
        /// </summary>
        private void MigrateExcelConfigTable(SQLiteConnection connection)
        {
            try
            {
                // 检查SplitEachRow列是否存在
                var checkSplitColumnSql = @"
                    SELECT COUNT(*) 
                    FROM pragma_table_info('ExcelConfigs') 
                    WHERE name = 'SplitEachRow'";

                using var checkSplitCommand = new SQLiteCommand(checkSplitColumnSql, connection);
                var splitColumnExists = Convert.ToInt32(checkSplitCommand.ExecuteScalar()) > 0;

                if (!splitColumnExists)
                {
                    _logger.LogInformation("为ExcelConfigs表添加SplitEachRow列");

                    // 添加SplitEachRow列
                    var addSplitColumnSql = @"
                        ALTER TABLE ExcelConfigs 
                        ADD COLUMN SplitEachRow INTEGER NOT NULL DEFAULT 0";

                    using var addSplitCommand = new SQLiteCommand(addSplitColumnSql, connection);
                    addSplitCommand.ExecuteNonQuery();

                    _logger.LogInformation("SplitEachRow列添加成功");
                }
                else
                {
                    _logger.LogInformation("SplitEachRow列已存在，跳过迁移");
                }

                // 检查ClearTableDataBeforeImport列是否存在
                var checkClearColumnSql = @"
                    SELECT COUNT(*) 
                    FROM pragma_table_info('ExcelConfigs') 
                    WHERE name = 'ClearTableDataBeforeImport'";

                using var checkClearCommand = new SQLiteCommand(checkClearColumnSql, connection);
                var clearColumnExists = Convert.ToInt32(checkClearCommand.ExecuteScalar()) > 0;

                if (!clearColumnExists)
                {
                    _logger.LogInformation("为ExcelConfigs表添加ClearTableDataBeforeImport列");

                    // 添加ClearTableDataBeforeImport列
                    var addClearColumnSql = @"
                        ALTER TABLE ExcelConfigs 
                        ADD COLUMN ClearTableDataBeforeImport INTEGER NOT NULL DEFAULT 0";

                    using var addClearCommand = new SQLiteCommand(addClearColumnSql, connection);
                    addClearCommand.ExecuteNonQuery();

                    _logger.LogInformation("ClearTableDataBeforeImport列添加成功");
                }
                else
                {
                    _logger.LogInformation("ClearTableDataBeforeImport列已存在，跳过迁移");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "迁移Excel配置表失败");
                throw;
            }
        }

        /// <summary>
        /// 获取默认数据源
        /// </summary>
        public string GetDefaultDataSourceId()
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();

                var sql = @"
                    SELECT Id 
                    FROM DataSourceConfig 
                    WHERE IsDefault = 1 
                    LIMIT 1";

                using var command = new SQLiteCommand(sql, connection);
                var result = command.ExecuteScalar();
                
                return result?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取默认数据源失败");
                return null;
            }
        }

        /// <summary>
        /// 设置默认数据源
        /// </summary>
        public bool SetDefaultDataSource(string dataSourceId)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();

                // 开始事务
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 先取消所有数据源的默认状态
                    var clearDefaultSql = @"
                        UPDATE DataSourceConfig 
                        SET IsDefault = 0, UpdatedTime = @UpdatedTime";

                    using var clearCommand = new SQLiteCommand(clearDefaultSql, connection, transaction);
                    clearCommand.Parameters.AddWithValue("@UpdatedTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    clearCommand.ExecuteNonQuery();

                    // 设置指定数据源为默认
                    var setDefaultSql = @"
                        UPDATE DataSourceConfig 
                        SET IsDefault = 1, UpdatedTime = @UpdatedTime 
                        WHERE Id = @Id";

                    using var setCommand = new SQLiteCommand(setDefaultSql, connection, transaction);
                    setCommand.Parameters.AddWithValue("@Id", dataSourceId);
                    setCommand.Parameters.AddWithValue("@UpdatedTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    var result = setCommand.ExecuteNonQuery();

                    // 提交事务
                    transaction.Commit();

                    return result > 0;
                }
                catch
                {
                    // 回滚事务
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置默认数据源失败: {DataSourceId}", dataSourceId);
                return false;
            }
        }

        /// <summary>
        /// 取消默认数据源
        /// </summary>
        public bool RemoveDefaultDataSource(string dataSourceId)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();

                var sql = @"
                    UPDATE DataSourceConfig 
                    SET IsDefault = 0, UpdatedTime = @UpdatedTime 
                    WHERE Id = @Id";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", dataSourceId);
                command.Parameters.AddWithValue("@UpdatedTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                
                var result = command.ExecuteNonQuery();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消默认数据源失败: {DataSourceId}", dataSourceId);
                return false;
            }
        }
    }
} 