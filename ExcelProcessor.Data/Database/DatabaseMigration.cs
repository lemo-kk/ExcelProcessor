using System;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using Dapper;

namespace ExcelProcessor.Data.Database
{
    /// <summary>
    /// 数据库迁移管理器
    /// </summary>
    public class DatabaseMigration
    {
        private readonly ILogger _logger;

        public DatabaseMigration(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 执行数据库迁移
        /// </summary>
        public void Migrate(SQLiteConnection connection)
        {
            try
            {
                _logger.LogInformation("开始执行数据库迁移...");
                
                // 迁移ExcelConfigs表
                MigrateExcelConfigTable(connection);
                
                // 迁移SqlConfigs表
                MigrateSqlConfigTable(connection);
                
                _logger.LogInformation("数据库迁移完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库迁移失败");
                throw;
            }
        }

        /// <summary>
        /// 迁移ExcelConfigs表
        /// </summary>
        private void MigrateExcelConfigTable(SQLiteConnection connection)
        {
            try
            {
                // 检查TargetDataSourceId列的类型
                var checkColumnTypeSql = @"
                    SELECT type 
                    FROM pragma_table_info('ExcelConfigs') 
                    WHERE name = 'TargetDataSourceId'";

                var columnType = connection.ExecuteScalar<string>(checkColumnTypeSql);
                
                if (columnType == "INTEGER")
                {
                    _logger.LogInformation("检测到ExcelConfigs表的TargetDataSourceId字段为INTEGER类型，开始迁移为TEXT类型...");
                    
                    // 创建临时表
                    var createTempTableSql = @"
                        CREATE TABLE ExcelConfigs_Temp (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ConfigName TEXT NOT NULL,
                            Description TEXT,
                            FilePath TEXT NOT NULL,
                            TargetDataSourceId TEXT NOT NULL,
                            TargetDataSourceName TEXT,
                            TargetTableName TEXT NOT NULL,
                            SheetName TEXT NOT NULL,
                            HeaderRow INTEGER NOT NULL DEFAULT 1,
                            DataStartRow INTEGER NOT NULL DEFAULT 2,
                            MaxRows INTEGER NOT NULL DEFAULT 0,
                            SkipEmptyRows INTEGER NOT NULL DEFAULT 1,
                            SplitEachRow INTEGER NOT NULL DEFAULT 0,
                            ClearTableDataBeforeImport INTEGER NOT NULL DEFAULT 0,
                            EnableValidation INTEGER NOT NULL DEFAULT 1,
                            EnableTransaction INTEGER NOT NULL DEFAULT 1,
                            ErrorHandlingStrategy TEXT NOT NULL DEFAULT 'Log',
                            Status TEXT NOT NULL DEFAULT 'Active',
                            CreatedByUserId INTEGER,
                            CreatedAt TEXT NOT NULL,
                            UpdatedAt TEXT,
                            Remarks TEXT
                        )";

                    connection.Execute(createTempTableSql);
                    _logger.LogInformation("临时表创建完成");

                    // 复制数据，将INTEGER类型的TargetDataSourceId转换为TEXT
                    var copyDataSql = @"
                        INSERT INTO ExcelConfigs_Temp 
                        SELECT 
                            Id, ConfigName, Description, FilePath, 
                            CASE 
                                WHEN TargetDataSourceId = 0 OR TargetDataSourceId IS NULL THEN 'default'
                                ELSE CAST(TargetDataSourceId AS TEXT)
                            END as TargetDataSourceId,
                            TargetDataSourceName, TargetTableName, SheetName, HeaderRow, 
                            DataStartRow, MaxRows, SkipEmptyRows, SplitEachRow, 
                            ClearTableDataBeforeImport, EnableValidation, EnableTransaction, 
                            ErrorHandlingStrategy, Status, CreatedByUserId, CreatedAt, UpdatedAt, Remarks
                        FROM ExcelConfigs";

                    var affectedRows = connection.Execute(copyDataSql);
                    _logger.LogInformation("数据复制完成，影响行数: {AffectedRows}", affectedRows);

                    // 删除原表
                    connection.Execute("DROP TABLE ExcelConfigs");
                    _logger.LogInformation("原表删除完成");

                    // 重命名临时表
                    connection.Execute("ALTER TABLE ExcelConfigs_Temp RENAME TO ExcelConfigs");
                    _logger.LogInformation("临时表重命名完成");

                    _logger.LogInformation("ExcelConfigs表迁移完成，TargetDataSourceId字段已从INTEGER改为TEXT");
                }
                else
                {
                    _logger.LogInformation("ExcelConfigs表的TargetDataSourceId字段已经是TEXT类型，无需迁移");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "迁移ExcelConfigs表失败");
                throw;
            }
        }

        /// <summary>
        /// 迁移SqlConfigs表
        /// </summary>
        private void MigrateSqlConfigTable(SQLiteConnection connection)
        {
            try
            {
                // 检查OutputDataSourceId列是否存在
                var checkOutputDataSourceIdColumnSql = @"
                    SELECT COUNT(*) 
                    FROM pragma_table_info('SqlConfigs') 
                    WHERE name = 'OutputDataSourceId'";

                using var checkOutputDataSourceIdCommand = new SQLiteCommand(checkOutputDataSourceIdColumnSql, connection);
                var outputDataSourceIdColumnExists = Convert.ToInt32(checkOutputDataSourceIdCommand.ExecuteScalar()) > 0;

                if (!outputDataSourceIdColumnExists)
                {
                    _logger.LogInformation("为SqlConfigs表添加OutputDataSourceId列");

                                         // 添加OutputDataSourceId列
                     var addOutputDataSourceIdColumnSql = @"
                         ALTER TABLE SqlConfigs 
                         ADD COLUMN OutputDataSourceId TEXT";

                    using var addOutputDataSourceIdCommand = new SQLiteCommand(addOutputDataSourceIdColumnSql, connection);
                    addOutputDataSourceIdCommand.ExecuteNonQuery();

                    _logger.LogInformation("OutputDataSourceId列添加成功");
                }
                else
                {
                    _logger.LogInformation("OutputDataSourceId列已存在，跳过迁移");
                }

                // 检查ClearSheetBeforeOutput列是否存在
                var checkClearSheetBeforeOutputColumnSql = @"
                    SELECT COUNT(*) 
                    FROM pragma_table_info('SqlConfigs') 
                    WHERE name = 'ClearSheetBeforeOutput'";

                using var checkClearSheetBeforeOutputCommand = new SQLiteCommand(checkClearSheetBeforeOutputColumnSql, connection);
                var clearSheetBeforeOutputColumnExists = Convert.ToInt32(checkClearSheetBeforeOutputCommand.ExecuteScalar()) > 0;

                if (!clearSheetBeforeOutputColumnExists)
                {
                    _logger.LogInformation("为SqlConfigs表添加ClearSheetBeforeOutput列");

                    // 添加ClearSheetBeforeOutput列
                    var addClearSheetBeforeOutputColumnSql = @"
                        ALTER TABLE SqlConfigs 
                        ADD COLUMN ClearSheetBeforeOutput INTEGER NOT NULL DEFAULT 0";

                    using var addClearSheetBeforeOutputCommand = new SQLiteCommand(addClearSheetBeforeOutputColumnSql, connection);
                    addClearSheetBeforeOutputCommand.ExecuteNonQuery();

                    _logger.LogInformation("ClearSheetBeforeOutput列添加成功");
                }
                else
                {
                    _logger.LogInformation("ClearSheetBeforeOutput列已存在，跳过迁移");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "迁移SqlConfigs表时出错");
                throw;
            }
        }
    }
} 