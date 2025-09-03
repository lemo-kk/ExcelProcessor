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
        private readonly ISqlService _sqlService;
        private readonly IDataSourceService _dataSourceService;
        private readonly IImportExportHistoryService _historyService;

        public JobPackageService(
            ILogger<JobPackageService> logger,
            IJobService jobService,
            IExcelConfigService excelConfigService,
            ISqlService sqlService,
            IDataSourceService dataSourceService,
            IImportExportHistoryService historyService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
            _excelConfigService = excelConfigService ?? throw new ArgumentNullException(nameof(excelConfigService));
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
                    Contents = new PackageContents
                    {
                        JobConfigCount = 1,
                        JobConfigs = new List<ContentItem> { MapToContentItem(job) }
                    },
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

                    // 导入作业配置
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
                    return (false, "配置包文件不存在", null);
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
                        return (false, "配置包格式错误：缺少包信息文件", null);
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
                return (false, $"验证失败: {ex.Message}", null);
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

        private async Task<string> CreateZipPackageAsync(string tempDir, string jobName, string targetPath)
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
            return targetPath;
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

        private async Task ImportJobConfigAsync(string tempDir, ImportOptions options)
        {
            var jobConfigDir = Path.Combine(tempDir, "job-config");
            if (!Directory.Exists(jobConfigDir)) return;

            foreach (var file in Directory.GetFiles(jobConfigDir, "*.json"))
            {
                var jobJson = await File.ReadAllTextAsync(file);
                var job = JsonSerializer.Deserialize<JobConfig>(jobJson);
                
                if (job != null)
                {
                    if (options.OverwriteExisting)
                    {
                        await _jobService.UpdateJobAsync(job);
                    }
                    else
                    {
                        await _jobService.CreateJobAsync(job);
                    }
                }
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

        #endregion
    }
} 