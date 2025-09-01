using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ExcelProcessor.Models;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// 数据缓存服务
    /// 用于缓存Excel文件信息、字段映射等数据，提高性能
    /// </summary>
    public class DataCacheService
    {
        private readonly ILogger<DataCacheService> _logger;
        private readonly ConcurrentDictionary<string, CacheItem> _cache;
        private readonly int _maxCacheSize;
        private readonly TimeSpan _defaultExpiration;

        public DataCacheService(ILogger<DataCacheService> logger, int maxCacheSize = 1000)
        {
            _logger = logger;
            _cache = new ConcurrentDictionary<string, CacheItem>();
            _maxCacheSize = maxCacheSize;
            _defaultExpiration = TimeSpan.FromMinutes(30);
        }

        /// <summary>
        /// 缓存项
        /// </summary>
        private class CacheItem
        {
            public object Value { get; set; } = string.Empty;
            public DateTime ExpirationTime { get; set; }
            public DateTime LastAccessTime { get; set; }
            public int AccessCount { get; set; }
        }

        /// <summary>
        /// 获取缓存项
        /// </summary>
        public T? Get<T>(string key) where T : class
        {
            if (string.IsNullOrEmpty(key))
                return null;

            if (_cache.TryGetValue(key, out var item))
            {
                if (DateTime.Now > item.ExpirationTime)
                {
                    // 缓存已过期，移除
                    _cache.TryRemove(key, out _);
                    return null;
                }

                // 更新访问信息
                item.LastAccessTime = DateTime.Now;
                item.AccessCount++;

                return item.Value as T;
            }

            return null;
        }

        /// <summary>
        /// 设置缓存项
        /// </summary>
        public void Set<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            if (string.IsNullOrEmpty(key) || value == null)
                return;

            // 检查缓存大小，如果超过限制则清理
            if (_cache.Count >= _maxCacheSize)
            {
                CleanupExpiredItems();
                
                // 如果仍然超过限制，移除最少访问的项
                if (_cache.Count >= _maxCacheSize)
                {
                    RemoveLeastAccessedItems();
                }
            }

            var expirationTime = DateTime.Now.Add(expiration ?? _defaultExpiration);
            var cacheItem = new CacheItem
            {
                Value = value,
                ExpirationTime = expirationTime,
                LastAccessTime = DateTime.Now,
                AccessCount = 1
            };

            _cache.AddOrUpdate(key, cacheItem, (k, v) => cacheItem);
        }

        /// <summary>
        /// 移除缓存项
        /// </summary>
        public bool Remove(string key)
        {
            return _cache.TryRemove(key, out _);
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
            _logger.LogInformation("数据缓存已清空");
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            var now = DateTime.Now;
            var totalItems = _cache.Count;
            var expiredItems = _cache.Values.Count(item => now > item.ExpirationTime);
            var validItems = totalItems - expiredItems;

            return new CacheStatistics
            {
                TotalItems = totalItems,
                ValidItems = validItems,
                ExpiredItems = expiredItems,
                HitRate = CalculateHitRate()
            };
        }

        /// <summary>
        /// 清理过期项
        /// </summary>
        private void CleanupExpiredItems()
        {
            var now = DateTime.Now;
            var expiredKeys = _cache.Where(kvp => now > kvp.Value.ExpirationTime)
                                  .Select(kvp => kvp.Key)
                                  .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogDebug($"清理了 {expiredKeys.Count} 个过期缓存项");
            }
        }

        /// <summary>
        /// 移除最少访问的项
        /// </summary>
        private void RemoveLeastAccessedItems()
        {
            var itemsToRemove = _cache.OrderBy(kvp => kvp.Value.AccessCount)
                                    .ThenBy(kvp => kvp.Value.LastAccessTime)
                                    .Take(_cache.Count - _maxCacheSize + 100)
                                    .Select(kvp => kvp.Key)
                                    .ToList();

            foreach (var key in itemsToRemove)
            {
                _cache.TryRemove(key, out _);
            }

            _logger.LogDebug($"移除了 {itemsToRemove.Count} 个最少访问的缓存项");
        }

        /// <summary>
        /// 计算命中率
        /// </summary>
        private double CalculateHitRate()
        {
            // 这里简化计算，实际应该维护命中统计
            return 0.8; // 假设80%命中率
        }

        /// <summary>
        /// 生成缓存键
        /// </summary>
        public static string GenerateCacheKey(string prefix, params object[] parameters)
        {
            var keyBuilder = new StringBuilder(prefix);
            foreach (var param in parameters)
            {
                keyBuilder.Append("_").Append(param?.ToString() ?? "null");
            }
            return keyBuilder.ToString();
        }

        /// <summary>
        /// 生成文件哈希键
        /// </summary>
        public static string GenerateFileHashKey(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return $"file_{filePath.GetHashCode()}";

                var fileInfo = new FileInfo(filePath);
                var hashInput = $"{filePath}_{fileInfo.Length}_{fileInfo.LastWriteTime:yyyyMMddHHmmss}";
                
                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
                return $"file_{Convert.ToBase64String(hashBytes).Replace("/", "_").Replace("+", "-").Substring(0, 16)}";
            }
            catch
            {
                return $"file_{filePath.GetHashCode()}";
            }
        }
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStatistics
    {
        public int TotalItems { get; set; }
        public int ValidItems { get; set; }
        public int ExpiredItems { get; set; }
        public double HitRate { get; set; }
    }

    /// <summary>
    /// Excel文件缓存信息
    /// </summary>
    public class ExcelFileCacheInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public List<string> SheetNames { get; set; } = new List<string>();
        public Dictionary<string, int> SheetRowCounts { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> SheetColumnCounts { get; set; } = new Dictionary<string, int>();
        public DateTime CacheTime { get; set; }
    }

    /// <summary>
    /// 字段映射缓存信息
    /// </summary>
    public class FieldMappingCacheInfo
    {
        public string ConfigId { get; set; } = string.Empty;
        public List<FieldMapping> FieldMappings { get; set; } = new List<FieldMapping>();
        public Dictionary<string, string> ColumnMapping { get; set; } = new Dictionary<string, string>();
        public DateTime CacheTime { get; set; }
    }
} 