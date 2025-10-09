using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArmaServerManager.Core;

public class ServerResourceData
{
    public string ServerId { get; set; } = string.Empty;
    public float CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ResourceAlert
{
    public string ServerId { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public float Threshold { get; set; }
    public float CurrentValue { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ResourceMonitor
{
    private readonly ConcurrentDictionary<string, List<ServerResourceData>> _serverData = new();
    private readonly ConcurrentDictionary<int, DateTime> _processCpuTimes = new();
    private readonly ConcurrentDictionary<int, TimeSpan> _lastCpuTimes = new();
    private readonly Timer _monitorTimer;
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _ramCounter;
    private readonly LoggingService _logger;
    
    public float CpuAlertThreshold { get; set; } = 80.0f;
    public long MemoryAlertThreshold { get; set; } = 1024 * 1024 * 1024; // 1GB
    
    public event Action<ResourceAlert>? AlertTriggered;
    public event Action<ServerResourceData>? DataUpdated;

    public ResourceMonitor(LoggingService logger)
    {
        _logger = logger;
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        _monitorTimer = new Timer(UpdateMetrics, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
    }

    public float GetCpuUsage() => _cpuCounter.NextValue();
    public long GetAvailableRam() => (long)_ramCounter.NextValue() * 1024 * 1024;
    public string GetSystemUptime() => TimeSpan.FromMilliseconds(Environment.TickCount).ToString(@"d\d\ h\h\ m\m");

    public IEnumerable<ServerResourceData> GetServerData(string serverId, int maxPoints = 100)
    {
        if (_serverData.TryGetValue(serverId, out var data))
            return data.TakeLast(maxPoints);
        return Enumerable.Empty<ServerResourceData>();
    }

    private void UpdateMetrics(object? state)
    {
        Task.Run(() =>
        {
            try
            {
                var processes = Process.GetProcessesByName("arma3server_x64");
                foreach (var process in processes)
                {
                    try
                    {
                        var serverId = $"{process.ProcessName}_{process.Id}";
                        var cpuUsage = GetProcessCpuUsage(process);
                        var memoryUsage = process.WorkingSet64;
                        
                        var data = new ServerResourceData
                        {
                            ServerId = serverId,
                            CpuUsage = cpuUsage,
                            MemoryUsage = memoryUsage,
                            Timestamp = DateTime.Now
                        };
                        
                        _serverData.AddOrUpdate(serverId, 
                            new List<ServerResourceData> { data },
                            (key, existing) =>
                            {
                                lock (existing)
                                {
                                    existing.Add(data);
                                    if (existing.Count > 1000)
                                        existing.RemoveAt(0);
                                }
                                return existing;
                            });
                            
                        DataUpdated?.Invoke(data);
                        CheckAlerts(data);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get metrics for process {ProcessId}", process.Id);
                    }
                    finally
                    {
                        process?.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update resource metrics");
            }
        });
    }


    private float GetProcessCpuUsage(Process process)
    {
        try
        {
            var processId = process.Id;
            var currentTime = DateTime.UtcNow;
            var currentCpuTime = process.TotalProcessorTime;
            
            if (_processCpuTimes.TryGetValue(processId, out var lastTime) && 
                _lastCpuTimes.TryGetValue(processId, out var lastCpuTime))
            {
                var timeDiff = (currentTime - lastTime).TotalMilliseconds;
                var cpuDiff = (currentCpuTime - lastCpuTime).TotalMilliseconds;
                
                if (timeDiff > 0)
                {
                    var cpuUsage = (cpuDiff / (timeDiff * Environment.ProcessorCount)) * 100;
                    _processCpuTimes[processId] = currentTime;
                    _lastCpuTimes[processId] = currentCpuTime;
                    return Math.Min(100, Math.Max(0, (float)cpuUsage));
                }
            }
            
            _processCpuTimes[processId] = currentTime;
            _lastCpuTimes[processId] = currentCpuTime;
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private void CheckAlerts(ServerResourceData data)
    {
        if (data.CpuUsage > CpuAlertThreshold)
        {
            AlertTriggered?.Invoke(new ResourceAlert
            {
                ServerId = data.ServerId,
                AlertType = "CPU",
                Threshold = CpuAlertThreshold,
                CurrentValue = data.CpuUsage,
                Timestamp = DateTime.Now
            });
        }
        
        if (data.MemoryUsage > MemoryAlertThreshold)
        {
            AlertTriggered?.Invoke(new ResourceAlert
            {
                ServerId = data.ServerId,
                AlertType = "Memory",
                Threshold = MemoryAlertThreshold,
                CurrentValue = data.MemoryUsage,
                Timestamp = DateTime.Now
            });
        }
    }

    public void Dispose()
    {
        _monitorTimer?.Dispose();
        _cpuCounter?.Dispose();
        _ramCounter?.Dispose();
    }
}