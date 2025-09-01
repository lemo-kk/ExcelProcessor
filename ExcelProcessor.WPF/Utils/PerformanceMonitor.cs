using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.WPF.Utils
{
    public class PerformanceMonitor
    {
        private static readonly Stopwatch _stopwatch = new Stopwatch();
        private static readonly Dictionary<string, TimeSpan> _timings = new Dictionary<string, TimeSpan>();
        private static readonly Dictionary<string, long> _memoryUsage = new Dictionary<string, long>();

        public static void StartOperation(string operation)
        {
            _stopwatch.Restart();
            var process = Process.GetCurrentProcess();
            _memoryUsage[operation] = process.WorkingSet64;
        }

        public static void StopOperation(string operation)
        {
            _stopwatch.Stop();
            _timings[operation] = _stopwatch.Elapsed;
            
            var process = Process.GetCurrentProcess();
            var memoryDiff = process.WorkingSet64 - _memoryUsage[operation];
            _memoryUsage[operation] = memoryDiff;
        }

        public static void LogPerformance(ILogger logger)
        {
            logger.LogInformation("=== 启动性能报告 ===");
            
            foreach (var timing in _timings)
            {
                var memoryMB = _memoryUsage[timing.Key] / 1024.0 / 1024.0;
                logger.LogInformation("操作: {Operation}, 耗时: {Elapsed}ms, 内存变化: {MemoryMB:F2}MB", 
                    timing.Key, timing.Value.TotalMilliseconds, memoryMB);
            }
            
            var totalTime = TimeSpan.Zero;
            foreach (var timing in _timings.Values)
            {
                totalTime += timing;
            }
            
            logger.LogInformation("总启动时间: {TotalTime}ms", totalTime.TotalMilliseconds);
        }
    }
} 