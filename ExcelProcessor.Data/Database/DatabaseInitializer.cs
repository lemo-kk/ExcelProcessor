using System;
using System.Data.SQLite;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Database
{
    /// <summary>
    /// 数据库初始化器
    /// </summary>
    public class DatabaseInitializer
    {
        private readonly ILogger<DatabaseInitializer> _logger;
        private readonly string _connectionString;

        public DatabaseInitializer(string connectionString, ILogger<DatabaseInitializer> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        public void Initialize()
        {
            try
            {
                _logger.LogInformation("开始初始化数据库...");

                // 确保数据库文件存在
                EnsureDatabaseExists();

                // 创建表结构
                CreateTables();

                // 初始化基础数据
                InitializeBaseData();

                _logger.LogInformation("数据库初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库初始化失败");
                throw;
            }
        }

        /// <summary>
        /// 确保数据库文件存在
        /// </summary>
        private void EnsureDatabaseExists()
        {
            var connectionStringBuilder = new SQLiteConnectionStringBuilder(_connectionString);
            var databasePath = connectionStringBuilder.DataSource;

            if (!string.IsNullOrEmpty(databasePath) && !File.Exists(databasePath))
            {
                _logger.LogInformation("创建数据库文件: {DatabasePath}", databasePath);
                var directory = Path.GetDirectoryName(databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                SQLiteConnection.CreateFile(databasePath);
            }
        }

        /// <summary>
        /// 创建表结构
        /// </summary>
        private void CreateTables()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // 启用外键约束
            using var pragmaCommand = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection);
            pragmaCommand.ExecuteNonQuery();

            // 创建用户表
            CreateUsersTable(connection);

            // 创建角色表
            CreateRolesTable(connection);

            // 创建权限表
            CreatePermissionsTable(connection);

            // 创建用户角色关联表
            CreateUserRolesTable(connection);

            // 创建角色权限关联表
            CreateRolePermissionsTable(connection);

            // 创建用户权限关联表
            CreateUserPermissionsTable(connection);

            // 创建数据源配置表
            CreateDataSourceConfigTable(connection);

            // 创建Excel配置表
            CreateExcelConfigTable(connection);

            // 创建Excel字段映射表
            CreateExcelFieldMappingTable(connection);

            // 创建Excel导入结果表
            CreateExcelImportResultTable(connection);

            // 创建SQL配置表
            CreateSqlConfigTable(connection);

            // 创建作业配置表
            CreateJobConfigTable(connection);

            // 创建作业步骤表
            CreateJobStepsTable(connection);

            // 创建作业执行记录表
            CreateJobExecutionsTable(connection);

            // 创建作业步骤执行记录表
            CreateJobStepExecutionsTable(connection);

            // 创建配置引用表
            CreateConfigurationReferencesTable(connection);

            // 创建作业统计表
            CreateJobStatisticsTable(connection);

            // 创建执行日志表
            CreateExecutionLogTable(connection);

            // 创建系统配置表
            CreateSystemConfigTable(connection);

            // 创建SQL执行历史表
            CreateSqlExecutionHistoryTable(connection);

            // 执行数据库迁移
            var migration = new DatabaseMigration(_logger);
            migration.Migrate(connection);
        }

        /// <summary>
        /// 创建用户表
        /// </summary>
        private void CreateUsersTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    DisplayName TEXT NOT NULL,
                    Email TEXT,
                    Role INTEGER NOT NULL DEFAULT 2,
                    Status INTEGER NOT NULL DEFAULT 0,
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    LastLoginTime TEXT,
                    CreatedTime TEXT NOT NULL,
                    UpdatedTime TEXT NOT NULL,
                    Remarks TEXT
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("用户表创建完成");
        }

        /// <summary>
        /// 创建角色表
        /// </summary>
        private void CreateRolesTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS Roles (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Code TEXT NOT NULL UNIQUE,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    Type INTEGER NOT NULL DEFAULT 1,
                    IsSystem INTEGER NOT NULL DEFAULT 0,
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    SortOrder INTEGER NOT NULL DEFAULT 0,
                    CreatedTime TEXT NOT NULL,
                    UpdatedTime TEXT NOT NULL,
                    Remarks TEXT
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("角色表创建完成");
        }

        /// <summary>
        /// 创建权限表
        /// </summary>
        private void CreatePermissionsTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS Permissions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Code TEXT NOT NULL UNIQUE,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    Type INTEGER NOT NULL DEFAULT 2,
                    [Group] TEXT NOT NULL,
                    Category TEXT NOT NULL DEFAULT '',
                    SortOrder INTEGER NOT NULL DEFAULT 0,
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    CreatedTime TEXT NOT NULL,
                    UpdatedTime TEXT NOT NULL
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("权限表创建完成");
        }

        /// <summary>
        /// 创建用户角色关联表
        /// </summary>
        private void CreateUserRolesTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS UserRoles (
                    UserId INTEGER NOT NULL,
                    RoleId INTEGER NOT NULL,
                    GrantedTime TEXT NOT NULL,
                    GrantedByUserId INTEGER,
                    Remarks TEXT,
                    PRIMARY KEY (UserId, RoleId),
                    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("用户角色关联表创建完成");
        }

        /// <summary>
        /// 创建角色权限关联表
        /// </summary>
        private void CreateRolePermissionsTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS RolePermissions (
                    RoleId INTEGER NOT NULL,
                    PermissionId INTEGER NOT NULL,
                    IsGranted INTEGER NOT NULL DEFAULT 1,
                    GrantedTime TEXT NOT NULL,
                    GrantedByUserId INTEGER,
                    Remarks TEXT,
                    PRIMARY KEY (RoleId, PermissionId),
                    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE,
                    FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("角色权限关联表创建完成");
        }

        /// <summary>
        /// 创建用户权限关联表
        /// </summary>
        private void CreateUserPermissionsTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS UserPermissions (
                    UserId INTEGER NOT NULL,
                    PermissionId INTEGER NOT NULL,
                    IsGranted INTEGER NOT NULL DEFAULT 1,
                    GrantedTime TEXT NOT NULL,
                    GrantedByUserId INTEGER,
                    Remarks TEXT,
                    PRIMARY KEY (UserId, PermissionId),
                    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                    FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("用户权限关联表创建完成");
        }

        /// <summary>
        /// 创建数据源配置表
        /// </summary>
        private void CreateDataSourceConfigTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS DataSourceConfig (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Description TEXT,
                    ConnectionString TEXT NOT NULL,
                    IsConnected INTEGER NOT NULL DEFAULT 0,
                    Status TEXT NOT NULL DEFAULT '未连接',
                    LastTestTime TEXT,
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    IsDefault INTEGER NOT NULL DEFAULT 0,
                    CreatedTime TEXT NOT NULL,
                    UpdatedTime TEXT NOT NULL
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("数据源配置表创建完成");
        }

        /// <summary>
        /// 创建Excel配置表
        /// </summary>
        private void CreateExcelConfigTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ExcelConfigs (
                    Id TEXT PRIMARY KEY,
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

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("Excel配置表创建完成");
        }

        /// <summary>
        /// 创建Excel字段映射表
        /// </summary>
        private void CreateExcelFieldMappingTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ExcelFieldMappings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ExcelConfigId TEXT NOT NULL,
                    ExcelColumnName TEXT NOT NULL,
                    ExcelColumnIndex INTEGER NOT NULL,
                    TargetFieldName TEXT NOT NULL,
                    TargetFieldType TEXT NOT NULL,
                    IsRequired INTEGER NOT NULL DEFAULT 0,
                    DefaultValue TEXT,
                    TransformRule TEXT,
                    ValidationRule TEXT,
                    SortOrder INTEGER NOT NULL DEFAULT 0,
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT,
                    Remarks TEXT,
                    FOREIGN KEY (ExcelConfigId) REFERENCES ExcelConfigs(Id) ON DELETE CASCADE
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("Excel字段映射表创建完成");
        }

        /// <summary>
        /// 创建Excel导入结果表
        /// </summary>
        private void CreateExcelImportResultTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ExcelImportResults (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ExcelConfigId TEXT NOT NULL,
                    BatchNumber TEXT NOT NULL,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT,
                    TotalRows INTEGER NOT NULL DEFAULT 0,
                    SuccessRows INTEGER NOT NULL DEFAULT 0,
                    FailedRows INTEGER NOT NULL DEFAULT 0,
                    SkippedRows INTEGER NOT NULL DEFAULT 0,
                    Status TEXT NOT NULL,
                    ErrorMessage TEXT,
                    ExecutedByUserId INTEGER,
                    ExecutedByUserName TEXT,
                    Progress INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT,
                    Remarks TEXT,
                    FOREIGN KEY (ExcelConfigId) REFERENCES ExcelConfigs(Id) ON DELETE CASCADE
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("Excel导入结果表创建完成");
        }

        /// <summary>
        /// 创建SQL配置表
        /// </summary>
        private void CreateSqlConfigTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS SqlConfigs (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Category TEXT NOT NULL,
                    OutputType TEXT NOT NULL,
                    OutputTarget TEXT NOT NULL,
                    Description TEXT,
                    SqlStatement TEXT NOT NULL,
                    DataSourceId TEXT,
                    OutputDataSourceId TEXT,
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    CreatedDate TEXT NOT NULL,
                    LastModified TEXT NOT NULL,
                    CreatedBy TEXT,
                    LastModifiedBy TEXT,
                    Parameters TEXT,
                    TimeoutSeconds INTEGER NOT NULL DEFAULT 300,
                    MaxRows INTEGER NOT NULL DEFAULT 10000,
                    AllowDeleteTarget INTEGER NOT NULL DEFAULT 0,
                    ClearTargetBeforeImport INTEGER NOT NULL DEFAULT 0,
                    ClearSheetBeforeOutput INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (DataSourceId) REFERENCES DataSourceConfigs(Id) ON DELETE SET NULL,
                    FOREIGN KEY (OutputDataSourceId) REFERENCES DataSourceConfigs(Id) ON DELETE SET NULL,
                    FOREIGN KEY (CreatedBy) REFERENCES Users(Id) ON DELETE SET NULL,
                    FOREIGN KEY (LastModifiedBy) REFERENCES Users(Id) ON DELETE SET NULL
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("SQL配置表创建完成");
        }

        /// <summary>
        /// 创建作业配置表
        /// </summary>
        private void CreateJobConfigTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS JobConfigs (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    Type TEXT,
                    Category TEXT,
                    Status TEXT,
                    Priority INTEGER,
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    ExecutionMode TEXT,
                    CronExpression TEXT,
                    TimeoutSeconds INTEGER,
                    MaxRetryCount INTEGER,
                    RetryIntervalSeconds INTEGER,
                    AllowParallelExecution INTEGER,
                    StepsConfig TEXT,
                    InputParameters TEXT,
                    OutputConfig TEXT,
                    NotificationConfig TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    CreatedBy TEXT,
                    UpdatedBy TEXT,
                    Remarks TEXT
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("作业配置表创建完成");
        }

        /// <summary>
        /// 创建作业步骤表
        /// </summary>
        private void CreateJobStepsTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS JobSteps (
                    Id TEXT PRIMARY KEY,
                    JobId TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    Type TEXT NOT NULL,
                    OrderIndex INTEGER NOT NULL,
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    Config TEXT,
                    ExcelConfigId TEXT,
                    SqlConfigId TEXT,
                    TimeoutSeconds INTEGER NOT NULL DEFAULT 300,
                    RetryCount INTEGER NOT NULL DEFAULT 0,
                    RetryIntervalSeconds INTEGER NOT NULL DEFAULT 60,
                    ContinueOnFailure INTEGER NOT NULL DEFAULT 0,
                    Dependencies TEXT,
                    ConditionExpression TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    FOREIGN KEY (JobId) REFERENCES JobConfigs(Id) ON DELETE CASCADE
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("作业步骤表创建完成");
        }

        /// <summary>
        /// 创建作业执行记录表
        /// </summary>
        private void CreateJobExecutionsTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS JobExecutions (
                    Id TEXT PRIMARY KEY,
                    JobId TEXT NOT NULL,
                    JobName TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT,
                    Duration INTEGER,
                    ExecutedBy TEXT,
                    Parameters TEXT,
                    Results TEXT,
                    ErrorMessage TEXT,
                    ErrorDetails TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    FOREIGN KEY (JobId) REFERENCES JobConfigs(Id) ON DELETE CASCADE
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("作业执行记录表创建完成");
        }

        /// <summary>
        /// 创建作业步骤执行记录表
        /// </summary>
        private void CreateJobStepExecutionsTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS JobStepExecutions (
                    Id TEXT PRIMARY KEY,
                    JobExecutionId TEXT NOT NULL,
                    StepId TEXT NOT NULL,
                    StepName TEXT NOT NULL,
                    StepType TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT,
                    Duration INTEGER,
                    Config TEXT,
                    Input TEXT,
                    Output TEXT,
                    ErrorMessage TEXT,
                    ErrorDetails TEXT,
                    RetryCount INTEGER DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    FOREIGN KEY (JobExecutionId) REFERENCES JobExecutions(Id) ON DELETE CASCADE
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("作业步骤执行记录表创建完成");
        }

        /// <summary>
        /// 创建配置引用表
        /// </summary>
        private void CreateConfigurationReferencesTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ConfigurationReferences (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    Type TEXT NOT NULL,
                    ReferencedConfigId TEXT NOT NULL,
                    ReferencedConfigName TEXT NOT NULL,
                    OverrideParameters TEXT,
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    CreatedBy TEXT,
                    UpdatedBy TEXT
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("配置引用表创建完成");
        }

        /// <summary>
        /// 创建作业统计表
        /// </summary>
        private void CreateJobStatisticsTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS JobStatistics (
                    JobId TEXT PRIMARY KEY,
                    JobName TEXT NOT NULL,
                    TotalExecutions INTEGER DEFAULT 0,
                    SuccessfulExecutions INTEGER DEFAULT 0,
                    FailedExecutions INTEGER DEFAULT 0,
                    CancelledExecutions INTEGER DEFAULT 0,
                    SuccessRate REAL DEFAULT 0,
                    AverageDuration INTEGER DEFAULT 0,
                    TotalDuration INTEGER DEFAULT 0,
                    LastExecutionTime TEXT,
                    FirstExecutionTime TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    FOREIGN KEY (JobId) REFERENCES JobConfigs(Id) ON DELETE CASCADE
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("作业统计表创建完成");
        }

        /// <summary>
        /// 创建执行日志表
        /// </summary>
        private void CreateExecutionLogTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ExecutionLog (
                    Id TEXT PRIMARY KEY,
                    JobId TEXT NOT NULL,
                    JobName TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT,
                    ErrorMessage TEXT,
                    Progress INTEGER NOT NULL DEFAULT 0,
                    LogDetails TEXT,
                    FOREIGN KEY (JobId) REFERENCES JobConfig(Id) ON DELETE CASCADE
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("执行日志表创建完成");
        }

        /// <summary>
        /// 创建系统配置表
        /// </summary>
        private void CreateSystemConfigTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS SystemConfig (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL,
                    Description TEXT,
                    UpdatedTime TEXT NOT NULL
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("系统配置表创建完成");
        }

        /// <summary>
        /// 初始化基础数据
        /// </summary>
        private void InitializeBaseData()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // 初始化系统角色
            InitializeSystemRoles(connection);

            // 初始化系统权限
            InitializeSystemPermissions(connection);

            // 初始化超级管理员用户
            InitializeSuperAdmin(connection);

            // 初始化系统配置
            InitializeSystemConfig(connection);

            // 初始化默认数据源
            InitializeDefaultDataSource(connection);
        }

        /// <summary>
        /// 初始化系统角色
        /// </summary>
        private void InitializeSystemRoles(SQLiteConnection connection)
        {
            var roles = new[]
            {
                new { Code = "SuperAdmin", Name = "超级管理员", Description = "拥有系统所有权限的最高管理员", Type = 0, IsSystem = 1, SortOrder = 1 },
                new { Code = "Admin", Name = "系统管理员", Description = "负责系统管理和用户管理", Type = 0, IsSystem = 1, SortOrder = 2 },
                new { Code = "User", Name = "普通用户", Description = "普通业务用户", Type = 0, IsSystem = 1, SortOrder = 3 },
                new { Code = "ReadOnly", Name = "只读用户", Description = "只能查看数据的用户", Type = 0, IsSystem = 1, SortOrder = 4 },
                new { Code = "FileProcessor", Name = "文件处理员", Description = "负责Excel文件的处理和分析", Type = 1, IsSystem = 0, SortOrder = 5 },
                new { Code = "DataManager", Name = "数据管理员", Description = "负责数据管理和维护", Type = 1, IsSystem = 0, SortOrder = 6 },
                new { Code = "Auditor", Name = "审计员", Description = "负责系统审计和监控", Type = 1, IsSystem = 0, SortOrder = 7 }
            };

            foreach (var role in roles)
            {
                var sql = @"
                    INSERT OR IGNORE INTO Roles (Code, Name, Description, Type, IsSystem, IsEnabled, SortOrder, CreatedTime, UpdatedTime)
                    VALUES (@Code, @Name, @Description, @Type, @IsSystem, 1, @SortOrder, @CreatedTime, @UpdatedTime)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@Code", role.Code);
                command.Parameters.AddWithValue("@Name", role.Name);
                command.Parameters.AddWithValue("@Description", role.Description);
                command.Parameters.AddWithValue("@Type", role.Type);
                command.Parameters.AddWithValue("@IsSystem", role.IsSystem);
                command.Parameters.AddWithValue("@SortOrder", role.SortOrder);
                command.Parameters.AddWithValue("@CreatedTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@UpdatedTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.ExecuteNonQuery();
            }

            _logger.LogInformation("系统角色初始化完成");
        }

        /// <summary>
        /// 初始化系统权限
        /// </summary>
        private void InitializeSystemPermissions(SQLiteConnection connection)
        {
            var permissions = new[]
            {
                // 系统管理权限
                new { Code = "SystemManagement", Name = "系统管理", Description = "系统管理相关权限", Type = 2, Group = "SystemManagement", SortOrder = 1 },
                new { Code = "UserManagement", Name = "用户管理", Description = "管理用户账户", Type = 2, Group = "UserManagement", SortOrder = 2 },
                new { Code = "RoleManagement", Name = "角色管理", Description = "管理用户角色", Type = 2, Group = "PermissionManagement", SortOrder = 3 },
                new { Code = "PermissionManagement", Name = "权限管理", Description = "管理系统权限", Type = 2, Group = "PermissionManagement", SortOrder = 4 },
                
                // 文件处理权限
                new { Code = "FileUpload", Name = "文件上传", Description = "上传Excel文件", Type = 2, Group = "FileProcessing", SortOrder = 5 },
                new { Code = "FileProcess", Name = "文件处理", Description = "处理Excel文件", Type = 2, Group = "FileProcessing", SortOrder = 6 },
                new { Code = "FileExport", Name = "文件导出", Description = "导出处理结果", Type = 2, Group = "FileProcessing", SortOrder = 7 },
                
                // 数据管理权限
                new { Code = "DataView", Name = "数据查看", Description = "查看处理结果", Type = 2, Group = "DataManagement", SortOrder = 8 },
                new { Code = "DataExport", Name = "数据导出", Description = "导出处理结果", Type = 2, Group = "DataManagement", SortOrder = 9 },
                new { Code = "DataBackup", Name = "数据备份", Description = "备份数据", Type = 2, Group = "DataManagement", SortOrder = 10 },
                
                // 系统设置权限
                new { Code = "SystemSettings", Name = "系统设置", Description = "修改系统设置", Type = 2, Group = "SystemSettings", SortOrder = 11 },
                new { Code = "LogView", Name = "日志查看", Description = "查看系统日志", Type = 2, Group = "LogManagement", SortOrder = 12 }
            };

            foreach (var permission in permissions)
            {
                var sql = @"
                    INSERT OR IGNORE INTO Permissions (Code, Name, Description, Type, [Group], SortOrder, IsEnabled, CreatedTime)
                    VALUES (@Code, @Name, @Description, @Type, @Group, @SortOrder, 1, @CreatedTime)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@Code", permission.Code);
                command.Parameters.AddWithValue("@Name", permission.Name);
                command.Parameters.AddWithValue("@Description", permission.Description);
                command.Parameters.AddWithValue("@Type", permission.Type);
                command.Parameters.AddWithValue("@Group", permission.Group);
                command.Parameters.AddWithValue("@SortOrder", permission.SortOrder);
                command.Parameters.AddWithValue("@CreatedTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.ExecuteNonQuery();
            }

            _logger.LogInformation("系统权限初始化完成");
        }

        /// <summary>
        /// 初始化超级管理员用户
        /// </summary>
        private void InitializeSuperAdmin(SQLiteConnection connection)
        {
            // 检查是否已存在超级管理员
            var checkSql = "SELECT COUNT(*) FROM Users WHERE Username = 'admin'";
            using var checkCommand = new SQLiteCommand(checkSql, connection);
            var count = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (count == 0)
            {
                // 创建超级管理员用户（密码：admin123）
                var passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
                var sql = @"
                    INSERT INTO Users (Username, PasswordHash, DisplayName, Email, Role, Status, IsEnabled, CreatedTime, UpdatedTime)
                    VALUES ('admin', @PasswordHash, '系统管理员', 'admin@example.com', 0, 0, 1, @CreatedTime, @UpdatedTime)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                command.Parameters.AddWithValue("@CreatedTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@UpdatedTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.ExecuteNonQuery();

                _logger.LogInformation("超级管理员用户创建完成");
            }
        }

        /// <summary>
        /// 初始化系统配置
        /// </summary>
        private void InitializeSystemConfig(SQLiteConnection connection)
        {
            var configs = new[]
            {
                new { Key = "DefaultInputPath", Value = "./data/input", Description = "默认输入文件路径" },
                new { Key = "DefaultOutputPath", Value = "./data/output", Description = "默认输出文件路径" },
                new { Key = "ExcelTemplatePath", Value = "./data/templates", Description = "Excel模板文件路径" },
                new { Key = "TempFilePath", Value = "./data/temp", Description = "临时文件路径" },
                new { Key = "UseRelativePath", Value = "true", Description = "是否使用相对路径" },
                new { Key = "LogRetentionDays", Value = "30", Description = "日志保留天数" },
                new { Key = "MaxFileSize", Value = "100", Description = "最大文件大小(MB)" },
                new { Key = "EnableLogin", Value = "true", Description = "是否启用登录" }
            };

            foreach (var config in configs)
            {
                var sql = @"
                    INSERT OR IGNORE INTO SystemConfig (Key, Value, Description, UpdatedTime)
                    VALUES (@Key, @Value, @Description, @UpdatedTime)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@Key", config.Key);
                command.Parameters.AddWithValue("@Value", config.Value);
                command.Parameters.AddWithValue("@Description", config.Description);
                command.Parameters.AddWithValue("@UpdatedTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.ExecuteNonQuery();
            }

            _logger.LogInformation("系统配置初始化完成");
        }

        /// <summary>
        /// 初始化默认数据源
        /// </summary>
        private void InitializeDefaultDataSource(SQLiteConnection connection)
        {
            // 检查是否已存在默认数据源
            var checkSql = "SELECT COUNT(*) FROM DataSourceConfig WHERE IsDefault = 1";
            using var checkCommand = new SQLiteCommand(checkSql, connection);
            var count = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (count == 0)
            {
                // 创建默认数据源
                var sql = @"
                    INSERT INTO DataSourceConfig (Id, Name, Type, Description, ConnectionString, IsConnected, Status, LastTestTime, IsEnabled, IsDefault, CreatedTime, UpdatedTime)
                    VALUES (@Id, @Name, @Type, @Description, @ConnectionString, @IsConnected, @Status, @LastTestTime, @IsEnabled, @IsDefault, @CreatedTime, @UpdatedTime)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("@Name", "默认importDB");
                command.Parameters.AddWithValue("@Type", "SQLite");
                command.Parameters.AddWithValue("@Description", "系统默认数据源，用于存储导入的数据");
                command.Parameters.AddWithValue("@ConnectionString", "Data Source=./data/ExcelProcessor.db;");
                command.Parameters.AddWithValue("@IsConnected", 1);
                command.Parameters.AddWithValue("@Status", "已连接");
                command.Parameters.AddWithValue("@LastTestTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@IsEnabled", 1);
                command.Parameters.AddWithValue("@IsDefault", 1);
                command.Parameters.AddWithValue("@CreatedTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@UpdatedTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.ExecuteNonQuery();

                _logger.LogInformation("默认数据源创建完成");
            }
        }

        /// <summary>
        /// 创建SQL执行历史表
        /// </summary>
        private void CreateSqlExecutionHistoryTable(SQLiteConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS SqlExecutionHistory (
                    Id TEXT PRIMARY KEY,
                    SqlConfigId TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT,
                    Duration INTEGER NOT NULL DEFAULT 0,
                    AffectedRows INTEGER NOT NULL DEFAULT 0,
                    ErrorMessage TEXT,
                    ResultData TEXT,
                    ExecutedBy TEXT,
                    ExecutionParameters TEXT,
                    FOREIGN KEY (SqlConfigId) REFERENCES SqlConfigs(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ExecutedBy) REFERENCES Users(Id) ON DELETE SET NULL
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
            _logger.LogInformation("SQL执行历史表创建完成");
        }

    }
} 