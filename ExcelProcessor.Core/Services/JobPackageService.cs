using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ExcelProcessor.Core.Interfaces;
using ExcelProcessor.Models;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 作业配置包服务实现
    /// </summary>
    public class JobPackageService : IJobPackageService
    {
        private readonly ILogger<JobPackageService> _logger;
        private readonly IJobService _jobService;
        private readonly IExcelConfigService _excelConfigService;
        private readonly IExcelService _excelService;
        private readonly ISqlService _sqlService;
        private readonly IDataSourceService _dataSourceService;
        private readonly IImportExportHistoryService _historyService;

        public JobPackageService(
            ILogger<JobPackageService> logger,
            IJobService jobService,
            IExcelConfigService excelConfigService,
            IExcelService excelService,
            ISqlService sqlService,
            IDataSourceService dataSourceService,
            IImportExportHistoryService historyService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
            _excelConfigService = excelConfigService ?? throw new ArgumentNullException(nameof(excelConfigService));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _sqlService = sqlService ?? throw new ArgumentNullException(nameof(sqlService));
            _dataSourceService = dataSourceService ?? throw new ArgumentNullException(nameof(dataSourceService));
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        }

        #region 导出功能

        public async Task<(bool success, string message)> ExportJobPackageAsync(
            string jobId, 
            string filePath,
            ExportOptions options)
        {
            try
            {
                _logger.LogInformation("开始导出作业配置包，作业ID: {JobId}", jobId);

                // 验证作业是否存在
                var job = await _jobService.GetJobByIdAsync(jobId);
                if (job == null)
                {
                    return (false, "指定的作业不存在");
                }

                // 创建临时目录
                var tempDir = Path.Combine(Path.GetTempPath(), $"JobPackage_{jobId}_{DateTime.Now:yyyyMMddHHmmss}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // 创建包信息文件
                    var packageInfo = CreatePackageInfo(job, options);
                    var packageInfoPath = Path.Combine(tempDir, "package-info.json");
                    await File.WriteAllTextAsync(packageInfoPath, JsonSerializer.Serialize(packageInfo, new JsonSerializerOptions { WriteIndented = true }));

                    // 导出作业配置
                    if (options.IncludeJobConfig)
                    {
                        await ExportJobConfigAsync(job, tempDir);
                    }

                    // 导出Excel配置
                    if (options.IncludeExcelConfigs)
                    {
                        await ExportExcelConfigsAsync(job, tempDir);
                    }

                    // 导出字段映射配置
                    if (options.IncludeFieldMappings)
                    {
                        await ExportFieldMappingsAsync(job, tempDir);
                    }

                    // 导出SQL配置
                    if (options.IncludeSqlScripts)
                    {
                        await ExportSqlConfigsAsync(job, tempDir);
                    }

                    // 导出数据源配置
                    if (options.IncludeDataSources)
                    {
                        await ExportDataSourceConfigsAsync(job, tempDir);
                    }

                    // 导出作业步骤配置
                    if (options.IncludeJobSteps)
                    {
                        await ExportJobStepsAsync(job, tempDir);
                    }

                    // 创建ZIP包
                    var packagePath = await CreateZipPackageAsync(tempDir, job.Name, filePath);

                    // 记录导出历史
                    await RecordExportHistoryAsync(job, packagePath, options);

                    _logger.LogInformation("作业配置包导出成功，路径: {PackagePath}", packagePath);
                    return (true, "作业配置包导出成功");
                }
                finally
                {
                    // 清理临时目录
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出作业配置包失败，作业ID: {JobId}", jobId);
                return (false, $"导出失败: {ex.Message}");
            }
        }

        public async Task<JobPackagePreviewInfo> GenerateJobPackagePreviewAsync(string jobId)
        {
            try
            {
                _logger.LogInformation("开始生成作业配置包预览，作业ID: {JobId}", jobId);

                // 验证作业是否存在
                var job = await _jobService.GetJobByIdAsync(jobId);
                if (job == null)
                {
                    throw new ArgumentException("指定的作业不存在");
                }

                // 创建预览信息
                var previewInfo = new JobPackagePreviewInfo
                {
                    BasicInfo = new PackageBasicInfo
                    {
                        PackageName = $"{job.Name}_ConfigPackage",
                        Version = "1.0.0",
                        CreatedTime = DateTime.Now,
                        CreatedBy = "System",
                        Description = $"作业 '{job.Name}' 的配置包",
                        Compatibility = "ExcelProcessor V1.0+",
                        Tags = new List<string> { "JobConfig", "ExcelProcessor" }
                    },
                    Contents = await BuildPackageContentsAsync(job),
                    Dependencies = new List<PackageDependency>(),
                    PotentialConflicts = new List<PackageConflict>(),
                    ImportRecommendations = new List<string>
                    {
                        "建议在导入前备份现有配置",
                        "检查数据源连接是否可用",
                        "验证字段映射配置是否正确"
                    },
                    EstimatedImportTimeSeconds = 30,
                    PackageSize = 0,
                    CanImportDirectly = true,
                    ConfirmationRequired = new List<string>
                    {
                        "确认覆盖现有配置",
                        "确认数据源连接信息"
                    }
                };

                _logger.LogInformation("作业配置包预览生成完成，作业ID: {JobId}", jobId);
                return previewInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成作业配置包预览失败，作业ID: {JobId}", jobId);
                throw;
            }
        }

        #endregion

        #region 导入功能

        public async Task<(bool success, string message)> ImportJobPackageAsync(string filePath, ImportOptions options)
        {
            try
            {
                _logger.LogInformation("开始导入作业配置包，路径: {FilePath}", filePath);

                if (!File.Exists(filePath))
                {
                    return (false, "配置包文件不存在");
                }

                // 创建临时目录
                var tempDir = Path.Combine(Path.GetTempPath(), $"Import_{DateTime.Now:yyyyMMddHHmmss}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // 解压配置包
                    ZipFile.ExtractToDirectory(filePath, tempDir);

                    // 读取包信息
                    var packageInfoPath = Path.Combine(tempDir, "package-info.json");
                    if (!File.Exists(packageInfoPath))
                    {
                        return (false, "配置包格式错误：缺少包信息文件");
                    }

                    var packageInfo = JsonSerializer.Deserialize<PackageBasicInfo>(
                        await File.ReadAllTextAsync(packageInfoPath));

                    // 导入各种配置（按依赖顺序）
                    // 1. 先导入数据源配置（其他配置的依赖）
                    if (Directory.Exists(Path.Combine(tempDir, "data-source-configs")))
                    {
                        await ImportDataSourceConfigsAsync(tempDir, options);
                    }

                    // 2. 导入Excel配置
                    if (Directory.Exists(Path.Combine(tempDir, "excel-configs")))
                    {
                        await ImportExcelConfigsAsync(tempDir, options);
                    }

                    // 3. 导入SQL配置
                    if (Directory.Exists(Path.Combine(tempDir, "sql-configs")))
                    {
                        await ImportSqlConfigsAsync(tempDir, options);
                    }

                    // 4. 导入字段映射配置
                    if (Directory.Exists(Path.Combine(tempDir, "field-mappings")))
                    {
                        await ImportFieldMappingsAsync(tempDir, options);
                    }

                    // 5. 最后导入作业配置（包含步骤配置）
                    if (Directory.Exists(Path.Combine(tempDir, "job-config")))
                    {
                        await ImportJobConfigAsync(tempDir, options);
                    }

                    // 记录导入历史
                    if (packageInfo != null)
                    {
                        await RecordImportHistoryAsync(packageInfo, filePath, options);
                    }

                    _logger.LogInformation("作业配置包导入成功，路径: {FilePath}", filePath);
                    return (true, "作业配置包导入成功");
                }
                finally
                {
                    // 清理临时目录
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入作业配置包失败，路径: {FilePath}", filePath);
                return (false, $"导入失败: {ex.Message}");
            }
        }

        public async Task<(bool isValid, string message, object validationDetails)> ValidateJobPackageAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("开始验证作业配置包，路径: {FilePath}", filePath);

                if (!File.Exists(filePath))
                {
                    return (false, "配置包文件不存在", (object)null!);
                }

                // 创建临时目录
                var tempDir = Path.Combine(Path.GetTempPath(), $"Validate_{DateTime.Now:yyyyMMddHHmmss}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // 解压配置包
                    ZipFile.ExtractToDirectory(filePath, tempDir);

                    // 读取包信息
                    var packageInfoPath = Path.Combine(tempDir, "package-info.json");
                    if (!File.Exists(packageInfoPath))
                    {
                        return (false, "配置包格式错误：缺少包信息文件", (object)null!);
                    }

                    var packageInfo = JsonSerializer.Deserialize<PackageBasicInfo>(
                        await File.ReadAllTextAsync(packageInfoPath));

                    var validationDetails = new
                    {
                        PackageInfo = packageInfo,
                        FileSize = new FileInfo(filePath).Length,
                        ExtractedFiles = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories).Length,
                        HasJobConfig = Directory.Exists(Path.Combine(tempDir, "job-config"))
                    };

                    _logger.LogInformation("作业配置包验证成功，路径: {FilePath}", filePath);
                    return (true, "配置包验证成功", validationDetails);
                }
                finally
                {
                    // 清理临时目录
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证作业配置包失败，路径: {FilePath}", filePath);
                return (false, $"验证失败: {ex.Message}", (object)null!);
            }
        }

        #endregion

        #region 内容分析

        public async Task<JobPackageContent> AnalyzePackageContentAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("开始分析配置包内容，路径: {FilePath}", filePath);

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("配置包文件不存在", filePath);
                }

                // 创建临时目录
                var tempDir = Path.Combine(Path.GetTempPath(), $"Analyze_{DateTime.Now:yyyyMMddHHmmss}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // 解压配置包
                    ZipFile.ExtractToDirectory(filePath, tempDir);

                    // 读取包信息
                    var packageInfoPath = Path.Combine(tempDir, "package-info.json");
                    if (!File.Exists(packageInfoPath))
                    {
                        throw new InvalidOperationException("配置包格式错误：缺少包信息文件");
                    }

                    var packageInfo = JsonSerializer.Deserialize<PackageBasicInfo>(
                        await File.ReadAllTextAsync(packageInfoPath));

                    // 分析包内容
                    var content = new JobPackageContent();

                    // 分析作业配置
                    var jobConfigDir = Path.Combine(tempDir, "job-config");
                    if (Directory.Exists(jobConfigDir))
                    {
                        var jobConfigFiles = Directory.GetFiles(jobConfigDir, "*.json");
                        var jobConfigs = await AnalyzeJobConfigsAsync(jobConfigFiles);
                        content.JobConfigs = jobConfigs.Select(j => j.Name).ToList();
                    }

                    _logger.LogInformation("配置包内容分析完成，路径: {FilePath}", filePath);
                    return content;
                }
                finally
                {
                    // 清理临时目录
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析配置包内容失败，路径: {FilePath}", filePath);
                throw;
            }
        }

        public async Task<List<string>> DetectConflictsAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("开始检测配置冲突，路径: {FilePath}", filePath);

                var conflicts = new List<string>();

                if (!File.Exists(filePath))
                {
                    conflicts.Add("配置包文件不存在");
                    return conflicts;
                }

                // 创建临时目录
                var tempDir = Path.Combine(Path.GetTempPath(), $"DetectConflicts_{DateTime.Now:yyyyMMddHHmmss}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // 解压配置包
                    ZipFile.ExtractToDirectory(filePath, tempDir);

                    // 读取包信息
                    var packageInfoPath = Path.Combine(tempDir, "package-info.json");
                    if (!File.Exists(packageInfoPath))
                    {
                        conflicts.Add("配置包格式错误：缺少包信息文件");
                        return conflicts;
                    }

                    var packageInfo = JsonSerializer.Deserialize<PackageBasicInfo>(
                        await File.ReadAllTextAsync(packageInfoPath));

                    // 检测作业配置冲突
                    conflicts.AddRange(await DetectJobConfigConflictsAsync(tempDir));

                    _logger.LogInformation("配置冲突检测完成，发现 {ConflictCount} 个冲突，路径: {FilePath}", conflicts.Count, filePath);
                    return conflicts;
                }
                finally
                {
                    // 清理临时目录
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检测配置冲突失败，路径: {FilePath}", filePath);
                return new List<string> { $"检测失败: {ex.Message}" };
            }
        }

        #endregion

        #region 私有方法

        private PackageBasicInfo CreatePackageInfo(JobConfig job, ExportOptions options)
        {
            return new PackageBasicInfo
            {
                PackageName = $"{job.Name}_ConfigPackage",
                Version = "1.0.0",
                CreatedTime = DateTime.Now,
                CreatedBy = "System",
                Description = $"作业 '{job.Name}' 的配置包",
                Compatibility = "ExcelProcessor V1.0+",
                Tags = new List<string> { "JobConfig", "ExcelProcessor" }
            };
        }

        private async Task ExportJobConfigAsync(JobConfig job, string tempDir)
        {
            var jobConfigDir = Path.Combine(tempDir, "job-config");
            Directory.CreateDirectory(jobConfigDir);

            var jobPath = Path.Combine(jobConfigDir, $"{job.Id}.json");
            await File.WriteAllTextAsync(jobPath, JsonSerializer.Serialize(job, new JsonSerializerOptions { WriteIndented = true }));
        }

        private async Task ExportExcelConfigsAsync(JobConfig job, string tempDir)
        {
            if (job.Steps == null || !job.Steps.Any())
                return;

            var excelConfigDir = Path.Combine(tempDir, "excel-configs");
            Directory.CreateDirectory(excelConfigDir);

            var excelConfigIds = new HashSet<string>();
            foreach (var step in job.Steps)
            {
                if (step.Type == StepType.ExcelImport && !string.IsNullOrEmpty(step.ExcelConfigId))
                {
                    excelConfigIds.Add(step.ExcelConfigId);
                }
            }

            foreach (var configId in excelConfigIds)
            {
                try
                {
                    var excelConfig = await _excelConfigService.GetConfigByIdAsync(configId);
                    if (excelConfig != null)
                    {
                        var configPath = Path.Combine(excelConfigDir, $"{excelConfig.Id}.json");
                        await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(excelConfig, new JsonSerializerOptions { WriteIndented = true }));
                        _logger.LogInformation("导出Excel配置: {ConfigName} ({ConfigId})", excelConfig.ConfigName, excelConfig.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "导出Excel配置失败: {ConfigId}", configId);
                }
            }
        }

        private async Task ExportFieldMappingsAsync(JobConfig job, string tempDir)
        {
            if (job.Steps == null || !job.Steps.Any())
                return;

            var fieldMappingDir = Path.Combine(tempDir, "field-mappings");
            Directory.CreateDirectory(fieldMappingDir);

            var excelConfigIds = new HashSet<string>();
            foreach (var step in job.Steps)
            {
                if (step.Type == StepType.ExcelImport && !string.IsNullOrEmpty(step.ExcelConfigId))
                {
                    excelConfigIds.Add(step.ExcelConfigId);
                }
            }

            foreach (var configId in excelConfigIds)
            {
                try
                {
                    var fieldMappings = await _excelService.GetFieldMappingsAsync(configId);
                    if (fieldMappings != null && fieldMappings.Any())
                    {
                        var mappingPath = Path.Combine(fieldMappingDir, $"{configId}_field-mappings.json");
                        await File.WriteAllTextAsync(mappingPath, JsonSerializer.Serialize(fieldMappings, new JsonSerializerOptions { WriteIndented = true }));
                        _logger.LogInformation("导出字段映射配置: {ConfigId} ({Count}个映射)", configId, fieldMappings.Count());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "导出字段映射配置失败: {ConfigId}", configId);
                }
            }
        }

        private async Task ExportSqlConfigsAsync(JobConfig job, string tempDir)
        {
            if (job.Steps == null || !job.Steps.Any())
                return;

            var sqlConfigDir = Path.Combine(tempDir, "sql-configs");
            Directory.CreateDirectory(sqlConfigDir);

            var sqlConfigIds = new HashSet<string>();
            foreach (var step in job.Steps)
            {
                if (step.Type == StepType.SqlExecution && !string.IsNullOrEmpty(step.SqlConfigId))
                {
                    sqlConfigIds.Add(step.SqlConfigId);
                }
            }

            foreach (var configId in sqlConfigIds)
            {
                try
                {
                    var sqlConfig = await _sqlService.GetSqlConfigByIdAsync(configId);
                    if (sqlConfig != null)
                    {
                        var configPath = Path.Combine(sqlConfigDir, $"{sqlConfig.Id}.json");
                        await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(sqlConfig, new JsonSerializerOptions { WriteIndented = true }));
                        _logger.LogInformation("导出SQL配置: {ConfigName} ({ConfigId})", sqlConfig.Name, sqlConfig.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "导出SQL配置失败: {ConfigId}", configId);
                }
            }
        }

        private async Task ExportDataSourceConfigsAsync(JobConfig job, string tempDir)
        {
            if (job.Steps == null || !job.Steps.Any())
                return;

            var dataSourceConfigDir = Path.Combine(tempDir, "data-source-configs");
            Directory.CreateDirectory(dataSourceConfigDir);

            var dataSourceIds = new HashSet<string>();
            
            // 从Excel配置中获取数据源ID
            foreach (var step in job.Steps)
            {
                if (step.Type == StepType.ExcelImport && !string.IsNullOrEmpty(step.ExcelConfigId))
                {
                    try
                    {
                        var excelConfig = await _excelConfigService.GetConfigByIdAsync(step.ExcelConfigId);
                                    if (excelConfig != null && !string.IsNullOrEmpty(excelConfig.TargetDataSourceId))
                                    {
                                        dataSourceIds.Add(excelConfig.TargetDataSourceId);
                                    }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "获取Excel配置数据源失败: {ExcelConfigId}", step.ExcelConfigId);
                    }
                }
            }

            // 从SQL配置中获取数据源ID
            foreach (var step in job.Steps)
            {
                if (step.Type == StepType.SqlExecution && !string.IsNullOrEmpty(step.SqlConfigId))
                {
                    try
                    {
                        var sqlConfig = await _sqlService.GetSqlConfigByIdAsync(step.SqlConfigId);
                        if (sqlConfig != null && !string.IsNullOrEmpty(sqlConfig.DataSourceId))
                        {
                            dataSourceIds.Add(sqlConfig.DataSourceId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "获取SQL配置数据源失败: {SqlConfigId}", step.SqlConfigId);
                    }
                }
            }

            foreach (var dataSourceId in dataSourceIds)
            {
                try
                {
                    var dataSourceConfig = await _dataSourceService.GetDataSourceByIdAsync(dataSourceId);
                    if (dataSourceConfig != null)
                    {
                        var configPath = Path.Combine(dataSourceConfigDir, $"{dataSourceConfig.Id}.json");
                        await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(dataSourceConfig, new JsonSerializerOptions { WriteIndented = true }));
                        _logger.LogInformation("导出数据源配置: {DataSourceName} ({DataSourceId})", dataSourceConfig.Name, dataSourceConfig.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "导出数据源配置失败: {DataSourceId}", dataSourceId);
                }
            }
        }

        private async Task ExportJobStepsAsync(JobConfig job, string tempDir)
        {
            if (job.Steps == null || !job.Steps.Any())
            {
                _logger.LogInformation("作业 {JobName} 没有步骤配置，跳过导出", job.Name);
                return;
            }

            var jobStepsDir = Path.Combine(tempDir, "job-steps");
            Directory.CreateDirectory(jobStepsDir);

            foreach (var step in job.Steps)
            {
                try
                {
                    var stepPath = Path.Combine(jobStepsDir, $"{step.Id}.json");
                    await File.WriteAllTextAsync(stepPath, JsonSerializer.Serialize(step, new JsonSerializerOptions { WriteIndented = true }));
                    _logger.LogInformation("导出作业步骤: {StepName} ({StepId}) - 类型: {StepType}", step.Name, step.Id, step.Type);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "导出作业步骤失败: {StepId}", step.Id);
                }
            }

            // 导出步骤顺序配置文件
            try
            {
                var stepOrder = job.Steps.Select((step, index) => new { StepId = step.Id, Order = index + 1, Name = step.Name }).ToList();
                var orderPath = Path.Combine(jobStepsDir, "step-order.json");
                await File.WriteAllTextAsync(orderPath, JsonSerializer.Serialize(stepOrder, new JsonSerializerOptions { WriteIndented = true }));
                _logger.LogInformation("导出作业步骤顺序配置，共 {Count} 个步骤", stepOrder.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "导出作业步骤顺序配置失败");
            }
        }

        private Task<string> CreateZipPackageAsync(string tempDir, string jobName, string targetPath)
        {
            // 如果目标路径是目录，则生成文件名
            if (Directory.Exists(targetPath))
            {
                var packageName = $"{jobName}_ConfigPackage_{DateTime.Now:yyyyMMddHHmmss}.zip";
                targetPath = Path.Combine(targetPath, packageName);
            }
            
            // 确保目标目录存在
            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }
            
            ZipFile.CreateFromDirectory(tempDir, targetPath);
            return Task.FromResult(targetPath);
        }

        private async Task RecordExportHistoryAsync(JobConfig job, string packagePath, ExportOptions options)
        {
            var history = new ImportExportHistory
            {
                Id = Guid.NewGuid().ToString(),
                OperationTime = DateTime.Now,
                OperationType = "Export",
                Status = "Success",
                JobName = job.Name,
                PackagePath = packagePath,
                Description = $"导出作业 '{job.Name}' 的配置包",
                OperatedBy = "System",
                CanReImport = false,
                ResultMessage = "导出成功",
                DurationMs = 0,
                FileSize = new FileInfo(packagePath).Length,
                Details = JsonSerializer.Serialize(options)
            };

            await _historyService.AddHistoryAsync(history);
        }

        private async Task RecordImportHistoryAsync(PackageBasicInfo packageInfo, string packagePath, ImportOptions options)
        {
            var history = new ImportExportHistory
            {
                Id = Guid.NewGuid().ToString(),
                OperationTime = DateTime.Now,
                OperationType = "Import",
                Status = "Success",
                JobName = packageInfo.PackageName,
                PackagePath = packagePath,
                Description = $"导入配置包 '{packageInfo.PackageName}'",
                OperatedBy = "System",
                CanReImport = true,
                ResultMessage = "导入成功",
                DurationMs = 0,
                FileSize = new FileInfo(packagePath).Length,
                Details = JsonSerializer.Serialize(options)
            };

            await _historyService.AddHistoryAsync(history);
        }

        private async Task ImportDataSourceConfigsAsync(string tempDir, ImportOptions options)
        {
            var dataSourceConfigDir = Path.Combine(tempDir, "data-source-configs");
            if (!Directory.Exists(dataSourceConfigDir)) return;

            _logger.LogInformation("开始导入数据源配置");

            foreach (var file in Directory.GetFiles(dataSourceConfigDir, "*.json"))
            {
                try
                {
                    var configJson = await File.ReadAllTextAsync(file);
                    var dataSource = JsonSerializer.Deserialize<DataSourceConfig>(configJson);
                    
                    if (dataSource != null)
                    {
                        // 特殊处理：如果导入的数据源名称是默认importDB
                        if (dataSource.Name == "默认importDB" || dataSource.Name == "importDB")
                        {
                            await HandleDefaultImportDBAsync(dataSource, options);
                        }
                        else
                        {
                            // 普通数据源的处理逻辑
                            if (options.OverwriteExisting)
                            {
                                await _dataSourceService.UpdateDataSourceAsync(dataSource);
                                _logger.LogInformation("更新数据源配置: {DataSourceName} ({DataSourceId})", dataSource.Name, dataSource.Id);
                            }
                            else
                            {
                                await _dataSourceService.SaveDataSourceAsync(dataSource);
                                _logger.LogInformation("创建数据源配置: {DataSourceName} ({DataSourceId})", dataSource.Name, dataSource.Id);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "导入数据源配置失败: {File}", file);
                }
            }
        }

        /// <summary>
        /// 处理默认importDB数据源的特殊导入逻辑
        /// </summary>
        private async Task HandleDefaultImportDBAsync(DataSourceConfig importedDataSource, ImportOptions options)
        {
            try
            {
                _logger.LogInformation("处理默认importDB数据源导入: {DataSourceId}", importedDataSource.Id);

                // 检查本地是否存在默认importDB
                DataSourceConfig? localDefaultDataSource = null;
                try
                {
                    localDefaultDataSource = await _dataSourceService.GetDataSourceByNameAsync("默认importDB");
                }
                catch (InvalidOperationException)
                {
                    // 本地没有默认importDB，这是正常情况
                    _logger.LogInformation("本地不存在默认importDB，将创建新的");
                }

                if (localDefaultDataSource != null)
                {
                    // 本地存在默认importDB，用导入的替换本地的
                    _logger.LogInformation("本地存在默认importDB (ID: {LocalId})，将被导入的 (ID: {ImportedId}) 替换", 
                        localDefaultDataSource.Id, importedDataSource.Id);

                    // 删除本地的默认importDB
                    await _dataSourceService.DeleteDataSourceAsync(localDefaultDataSource.Id);
                    _logger.LogInformation("已删除本地默认importDB: {LocalId}", localDefaultDataSource.Id);

                    // 保存导入的默认importDB
                    await _dataSourceService.SaveDataSourceAsync(importedDataSource);
                    _logger.LogInformation("已保存导入的默认importDB: {ImportedId}", importedDataSource.Id);
                }
                else
                {
                    // 本地不存在默认importDB，直接保存导入的
                    await _dataSourceService.SaveDataSourceAsync(importedDataSource);
                    _logger.LogInformation("本地不存在默认importDB，已创建新的: {ImportedId}", importedDataSource.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理默认importDB数据源导入失败: {DataSourceId}", importedDataSource.Id);
                throw;
            }
        }

        private async Task ImportExcelConfigsAsync(string tempDir, ImportOptions options)
        {
            var excelConfigDir = Path.Combine(tempDir, "excel-configs");
            if (!Directory.Exists(excelConfigDir)) return;

            _logger.LogInformation("开始导入Excel配置");

            foreach (var file in Directory.GetFiles(excelConfigDir, "*.json"))
            {
                try
                {
                    var configJson = await File.ReadAllTextAsync(file);
                    var excelConfig = JsonSerializer.Deserialize<ExcelConfig>(configJson);
                    
                    if (excelConfig != null)
                    {
                        // 检查配置是否已存在
                        var existingConfig = await _excelConfigService.GetConfigByIdAsync(excelConfig.Id);
                        
                        if (existingConfig != null)
                        {
                            if (options.OverwriteExisting)
                            {
                                await _excelConfigService.UpdateConfigAsync(excelConfig);
                                _logger.LogInformation("更新Excel配置: {ConfigName} ({ConfigId})", excelConfig.ConfigName, excelConfig.Id);
                            }
                            else
                            {
                                _logger.LogInformation("Excel配置已存在，跳过: {ConfigName} ({ConfigId})", excelConfig.ConfigName, excelConfig.Id);
                            }
                        }
                        else
                        {
                            // 配置不存在，直接创建（保持原有ID）
                            await _excelConfigService.SaveConfigAsync(excelConfig);
                            _logger.LogInformation("创建Excel配置: {ConfigName} ({ConfigId})", excelConfig.ConfigName, excelConfig.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "导入Excel配置失败: {File}", file);
                }
            }
        }

        private async Task ImportSqlConfigsAsync(string tempDir, ImportOptions options)
        {
            var sqlConfigDir = Path.Combine(tempDir, "sql-configs");
            if (!Directory.Exists(sqlConfigDir)) return;

            _logger.LogInformation("开始导入SQL配置");

            foreach (var file in Directory.GetFiles(sqlConfigDir, "*.json"))
            {
                try
                {
                    var configJson = await File.ReadAllTextAsync(file);
                    var sqlConfig = JsonSerializer.Deserialize<SqlConfig>(configJson);
                    
                    if (sqlConfig != null)
                    {
                        // 检查SQL配置是否已存在
                        var existingConfig = await _sqlService.GetSqlConfigByIdAsync(sqlConfig.Id);
                        
                        if (existingConfig != null)
                        {
                            if (options.OverwriteExisting)
                            {
                                await _sqlService.UpdateSqlConfigAsync(sqlConfig);
                                _logger.LogInformation("更新SQL配置: {ConfigName} ({ConfigId})", sqlConfig.Name, sqlConfig.Id);
                            }
                            else
                            {
                                _logger.LogInformation("SQL配置已存在，跳过: {ConfigName} ({ConfigId})", sqlConfig.Name, sqlConfig.Id);
                            }
                        }
                        else
                        {
                            // 配置不存在，直接创建
                            await _sqlService.CreateSqlConfigAsync(sqlConfig);
                            _logger.LogInformation("创建SQL配置: {ConfigName} ({ConfigId})", sqlConfig.Name, sqlConfig.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "导入SQL配置失败: {File}", file);
                }
            }
        }

        private async Task ImportFieldMappingsAsync(string tempDir, ImportOptions options)
        {
            var fieldMappingDir = Path.Combine(tempDir, "field-mappings");
            if (!Directory.Exists(fieldMappingDir)) return;

            _logger.LogInformation("开始导入字段映射配置");

            foreach (var file in Directory.GetFiles(fieldMappingDir, "*_field-mappings.json"))
            {
                try
                {
                    var mappingJson = await File.ReadAllTextAsync(file);
                    var fieldMappings = JsonSerializer.Deserialize<List<ExcelFieldMapping>>(mappingJson);
                    
                    if (fieldMappings != null && fieldMappings.Any())
                    {
                        // 从文件名提取Excel配置ID
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var excelConfigId = fileName.Replace("_field-mappings", "");
                        
                        // 验证Excel配置是否存在
                        var excelConfig = await _excelConfigService.GetConfigByIdAsync(excelConfigId);
                        if (excelConfig == null)
                        {
                            _logger.LogWarning("Excel配置不存在，跳过字段映射导入: {ExcelConfigId}", excelConfigId);
                            continue;
                        }
                        
                        if (options.OverwriteExisting)
                        {
                            // 获取现有映射并删除
                            var existingMappings = await _excelService.GetFieldMappingsAsync(excelConfigId);
                            if (existingMappings != null)
                            {
                                foreach (var existingMapping in existingMappings)
                                {
                                    await _excelService.DeleteFieldMappingAsync(existingMapping.Id);
                                }
                            }
                        }
                        
                        // 批量创建字段映射
                        foreach (var mapping in fieldMappings)
                        {
                            // 确保ExcelConfigId正确设置
                            mapping.ExcelConfigId = excelConfigId;
                            await _excelService.CreateFieldMappingAsync(mapping);
                        }
                        
                        _logger.LogInformation("导入字段映射配置: {ExcelConfigId} ({Count}个映射)", excelConfigId, fieldMappings.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "导入字段映射配置失败: {File}", file);
                }
            }
        }

        private async Task ImportJobConfigAsync(string tempDir, ImportOptions options)
        {
            var jobConfigDir = Path.Combine(tempDir, "job-config");
            if (!Directory.Exists(jobConfigDir)) return;

            _logger.LogInformation("开始导入作业配置");

            foreach (var file in Directory.GetFiles(jobConfigDir, "*.json"))
            {
                try
            {
                var jobJson = await File.ReadAllTextAsync(file);
                var job = JsonSerializer.Deserialize<JobConfig>(jobJson);
                
                if (job != null)
                    {
                        // 检查作业是否已存在
                        var existingJob = await _jobService.GetJobByIdAsync(job.Id);
                        
                        if (existingJob != null)
                {
                    if (options.OverwriteExisting)
                    {
                        await _jobService.UpdateJobAsync(job);
                                _logger.LogInformation("更新作业配置: {JobName} ({JobId})", job.Name, job.Id);
                    }
                    else
                    {
                                _logger.LogInformation("作业配置已存在，跳过: {JobName} ({JobId})", job.Name, job.Id);
                            }
                        }
                        else
                        {
                            // 作业不存在，直接创建
                        await _jobService.CreateJobAsync(job);
                            _logger.LogInformation("创建作业配置: {JobName} ({JobId})", job.Name, job.Id);
                        }

                        // 导入作业步骤
                        await ImportJobStepsAsync(job.Id, tempDir, options);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "导入作业配置失败: {File}", file);
                }
            }
        }

        private async Task ImportJobStepsAsync(string jobId, string tempDir, ImportOptions options)
        {
            var jobStepsDir = Path.Combine(tempDir, "job-steps");
            if (!Directory.Exists(jobStepsDir))
            {
                _logger.LogInformation("作业步骤目录不存在，跳过步骤导入: {JobId}", jobId);
                return;
            }

            _logger.LogInformation("开始导入作业步骤，作业ID: {JobId}", jobId);

            try
            {
                // 获取当前作业配置
                var job = await _jobService.GetJobByIdAsync(jobId);
                if (job == null)
                {
                    _logger.LogWarning("作业配置不存在，跳过步骤导入: {JobId}", jobId);
                    return;
                }

                var steps = new List<JobStep>();

                // 读取步骤顺序配置
                var stepOrderPath = Path.Combine(jobStepsDir, "step-order.json");
                var stepOrder = new List<dynamic>();
                
                if (File.Exists(stepOrderPath))
                {
                    try
                    {
                        var orderJson = await File.ReadAllTextAsync(stepOrderPath);
                        stepOrder = JsonSerializer.Deserialize<List<dynamic>>(orderJson) ?? new List<dynamic>();
                        _logger.LogInformation("读取到步骤顺序配置，共 {Count} 个步骤", stepOrder.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "读取步骤顺序配置失败");
                    }
                }

                // 按顺序导入步骤
                foreach (var orderItem in stepOrder)
                {
                    try
                    {
                        var stepId = orderItem.GetProperty("StepId").GetString();
                        var stepFile = Path.Combine(jobStepsDir, $"{stepId}.json");
                        
                        if (File.Exists(stepFile))
                        {
                            var stepJson = await File.ReadAllTextAsync(stepFile);
                            var step = JsonSerializer.Deserialize<JobStep>(stepJson);
                            
                            if (step != null)
                            {
                                // 设置作业ID
                                step.JobId = jobId;
                                steps.Add(step);
                                _logger.LogInformation("导入作业步骤: {StepName} ({StepId}) - 类型: {StepType}", step.Name, step.Id, step.Type);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var stepId = orderItem.GetProperty("StepId").GetString() ?? "unknown";
                        _logger.LogInformation($"导入作业步骤失败: {stepId}, 错误: {ex.Message}");
                    }
                }

                // 如果没有步骤顺序配置，则导入所有步骤文件
                if (!stepOrder.Any())
                {
                    foreach (var stepFile in Directory.GetFiles(jobStepsDir, "*.json"))
                    {
                        try
                        {
                            var fileName = Path.GetFileNameWithoutExtension(stepFile);
                            if (fileName == "step-order") continue; // 跳过顺序配置文件

                            var stepJson = await File.ReadAllTextAsync(stepFile);
                            var step = JsonSerializer.Deserialize<JobStep>(stepJson);
                            
                            if (step != null)
                            {
                                // 设置作业ID
                                step.JobId = jobId;
                                steps.Add(step);
                                _logger.LogInformation("导入作业步骤: {StepName} ({StepId}) - 类型: {StepType}", step.Name, step.Id, step.Type);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "导入作业步骤失败: {StepFile}", stepFile);
                        }
                    }
                }

                // 更新作业配置的步骤
                if (steps.Any())
                {
                    job.Steps = steps;
                    await _jobService.UpdateJobAsync(job);
                    _logger.LogInformation("更新作业配置步骤，共 {Count} 个步骤", steps.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入作业步骤失败，作业ID: {JobId}", jobId);
            }
        }

        private async Task<List<JobConfig>> AnalyzeJobConfigsAsync(string[] files)
        {
            var configs = new List<JobConfig>();
            
            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var config = JsonSerializer.Deserialize<JobConfig>(json);
                    if (config != null)
                    {
                        configs.Add(config);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "分析作业配置文件失败: {File}", file);
                }
            }
            
            return configs;
        }

        private async Task<List<string>> DetectJobConfigConflictsAsync(string tempDir)
        {
            var conflicts = new List<string>();
            var jobConfigDir = Path.Combine(tempDir, "job-config");
            
            if (Directory.Exists(jobConfigDir))
            {
                var jobConfigFiles = Directory.GetFiles(jobConfigDir, "*.json");
                foreach (var file in jobConfigFiles)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var config = JsonSerializer.Deserialize<JobConfig>(json);
                        if (config != null)
                        {
                            // 检查是否存在同名作业
                            // 这里可以添加具体的冲突检测逻辑
                        }
                    }
                    catch (Exception ex)
                    {
                        conflicts.Add($"作业配置文件解析失败: {Path.GetFileName(file)} - {ex.Message}");
                    }
                }
            }
            
            return conflicts;
        }

        private ContentItem MapToContentItem(JobConfig job)
        {
            return new ContentItem
            {
                Id = job.Id,
                Name = job.Name,
                Type = "JobConfig",
                Description = job.Description,
                CreatedTime = DateTime.Now,
                LastModifiedTime = DateTime.Now,
                AlreadyExists = false,
                ConflictType = null
            };
        }

        private ContentItem MapToContentItem(ExcelConfig config)
        {
            return new ContentItem
            {
                Id = config.Id,
                Name = config.ConfigName,
                Type = "ExcelConfig",
                Description = config.Description,
                CreatedTime = DateTime.TryParse(config.CreatedAt, out var createdTime) ? createdTime : DateTime.Now,
                LastModifiedTime = DateTime.TryParse(config.UpdatedAt, out var updatedTime) ? updatedTime : DateTime.Now,
                AlreadyExists = false,
                ConflictType = null
            };
        }

        private ContentItem MapToContentItem(SqlConfig config)
        {
            return new ContentItem
            {
                Id = config.Id,
                Name = config.Name,
                Type = "SqlConfig",
                Description = config.Description,
                CreatedTime = config.CreatedDate,
                LastModifiedTime = config.LastModified,
                AlreadyExists = false,
                ConflictType = null
            };
        }

        private ContentItem MapToContentItem(ExcelFieldMapping mapping)
        {
            return new ContentItem
            {
                Id = mapping.Id.ToString(),
                Name = $"{mapping.ExcelColumnName} → {mapping.TargetFieldName}",
                Type = "FieldMapping",
                Description = $"Excel列 '{mapping.ExcelColumnName}' 映射到数据库字段 '{mapping.TargetFieldName}' ({mapping.TargetFieldType})",
                CreatedTime = mapping.CreatedAt,
                LastModifiedTime = mapping.UpdatedAt ?? mapping.CreatedAt,
                AlreadyExists = false,
                ConflictType = null
            };
        }

        /// <summary>
        /// 构建包内容信息
        /// </summary>
        private async Task<PackageContents> BuildPackageContentsAsync(JobConfig job)
        {
            var contents = new PackageContents
            {
                JobConfigCount = 1,
                JobConfigs = new List<ContentItem> { MapToContentItem(job) },
                ExcelConfigs = new List<ContentItem>(),
                SqlScripts = new List<ContentItem>(),
                FieldMappings = new List<ContentItem>()
            };

            // 解析作业步骤配置
            if (job.Steps != null && job.Steps.Any())
            {
                _logger.LogInformation("开始分析作业步骤，共 {Count} 个步骤", job.Steps.Count);

                var excelConfigIds = new HashSet<string>();
                var sqlConfigIds = new HashSet<string>();

                // 收集所有配置ID
                foreach (var step in job.Steps)
                {
                    _logger.LogInformation("分析步骤: {StepName} ({StepType})", step.Name, step.Type);

                    if (step.Type == StepType.ExcelImport && !string.IsNullOrEmpty(step.ExcelConfigId))
                    {
                        excelConfigIds.Add(step.ExcelConfigId);
                        _logger.LogInformation("发现Excel配置ID: {ConfigId}", step.ExcelConfigId);
                    }
                    else if (step.Type == StepType.SqlExecution && !string.IsNullOrEmpty(step.SqlConfigId))
                    {
                        sqlConfigIds.Add(step.SqlConfigId);
                        _logger.LogInformation("发现SQL配置ID: {ConfigId}", step.SqlConfigId);
                    }
                }

                // 获取Excel配置详情
                foreach (var configId in excelConfigIds)
                {
                    try
                    {
                        var excelConfig = await _excelConfigService.GetConfigByIdAsync(configId);
                        if (excelConfig != null)
                        {
                            contents.ExcelConfigs.Add(MapToContentItem(excelConfig));
                            _logger.LogInformation("成功获取Excel配置: {ConfigName}", excelConfig.ConfigName);

                            // 获取字段映射配置
                            try
                            {
                                var fieldMappings = await _excelService.GetFieldMappingsAsync(configId);
                                if (fieldMappings != null && fieldMappings.Any())
                                {
                                    foreach (var mapping in fieldMappings)
                                    {
                                        contents.FieldMappings.Add(MapToContentItem(mapping));
                                    }
                                    _logger.LogInformation("成功获取字段映射配置: {ConfigId} ({Count}个映射)", configId, fieldMappings.Count());
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "获取字段映射配置失败: {ConfigId}", configId);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Excel配置不存在: {ConfigId}", configId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "获取Excel配置失败: {ConfigId}", configId);
                    }
                }

                // 获取SQL配置详情
                foreach (var configId in sqlConfigIds)
                {
                    try
                    {
                        var sqlConfig = await _sqlService.GetSqlConfigByIdAsync(configId);
                        if (sqlConfig != null)
                        {
                            contents.SqlScripts.Add(MapToContentItem(sqlConfig));
                            _logger.LogInformation("成功获取SQL配置: {ConfigName}", sqlConfig.Name);
                        }
                        else
                        {
                            _logger.LogWarning("SQL配置不存在: {ConfigId}", configId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "获取SQL配置失败: {ConfigId}", configId);
                    }
                }
            }
            else
            {
                _logger.LogWarning("作业没有步骤配置或步骤列表为空");
            }

            // 设置计数
            contents.ExcelConfigCount = contents.ExcelConfigs.Count;
            contents.SqlScriptCount = contents.SqlScripts.Count;
            contents.FieldMappingCount = contents.FieldMappings.Count;

            _logger.LogInformation("包内容构建完成: 作业配置={JobCount}, Excel配置={ExcelCount}, SQL配置={SqlCount}, 字段映射={FieldMappingCount}",
                contents.JobConfigCount, contents.ExcelConfigCount, contents.SqlScriptCount, contents.FieldMappingCount);

            return contents;
        }

        #endregion
    }
} 