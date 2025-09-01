-- SQL配置表结构升级脚本
-- 添加缺失的执行配置字段

-- 1. 添加执行模式字段
ALTER TABLE SqlConfigs ADD COLUMN ExecutionMode TEXT DEFAULT 'Normal';

-- 2. 添加启用详细日志字段
ALTER TABLE SqlConfigs ADD COLUMN EnableLogging INTEGER NOT NULL DEFAULT 1;

-- 3. 添加缓存查询结果字段
ALTER TABLE SqlConfigs ADD COLUMN CacheResults INTEGER NOT NULL DEFAULT 0;

-- 4. 添加参数验证字段
ALTER TABLE SqlConfigs ADD COLUMN ValidateParameters INTEGER NOT NULL DEFAULT 1;

-- 5. 验证字段是否添加成功
SELECT name, type, "notnull", dflt_value 
FROM pragma_table_info('SqlConfigs') 
WHERE name IN ('ExecutionMode', 'EnableLogging', 'CacheResults', 'ValidateParameters');

-- 6. 更新现有记录的默认值
UPDATE SqlConfigs SET 
    ExecutionMode = 'Normal',
    EnableLogging = 1,
    CacheResults = 0,
    ValidateParameters = 1
WHERE ExecutionMode IS NULL OR EnableLogging IS NULL OR CacheResults IS NULL OR ValidateParameters IS NULL;

-- 7. 验证更新结果
SELECT COUNT(*) as TotalRecords,
       COUNT(CASE WHEN ExecutionMode IS NOT NULL THEN 1 END) as ExecutionModeCount,
       COUNT(CASE WHEN EnableLogging IS NOT NULL THEN 1 END) as EnableLoggingCount,
       COUNT(CASE WHEN CacheResults IS NOT NULL THEN 1 END) as CacheResultsCount,
       COUNT(CASE WHEN ValidateParameters IS NOT NULL THEN 1 END) as ValidateParametersCount
FROM SqlConfigs; 