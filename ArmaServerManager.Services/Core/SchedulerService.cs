using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ArmaServerManager.Core;

public class ScheduledTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime NextRun { get; set; }
    public TimeSpan Interval { get; set; }
    public Func<Task> Action { get; set; } = () => Task.CompletedTask;
    public bool IsEnabled { get; set; } = true;
}

public class SchedulerService
{
    private readonly List<ScheduledTask> _tasks = new();
    private readonly Timer _timer;
    private readonly ServerManager _serverManager;
    private readonly UpdateService _updateService;
    private readonly BackupService _backupService;
    private readonly LoggingService _logger;
    private readonly string _tasksFilePath;

    public SchedulerService(ServerManager serverManager, UpdateService updateService, BackupService backupService, LoggingService logger)
    {
        _serverManager = serverManager;
        _updateService = updateService;
        _backupService = backupService;
        _logger = logger;
        _tasksFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "scheduled_tasks.json");
        
        LoadTasks();
        _timer = new Timer(CheckTasks, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public void ScheduleServerRestart(string serverId, TimeSpan interval)
    {
        var task = new ScheduledTask
        {
            Name = $"Restart Server {serverId}",
            NextRun = DateTime.Now.Add(interval),
            Interval = interval,
            Action = () => _serverManager.RestartServerAsync(serverId)
        };
        _tasks.Add(task);
        SaveTasks();
    }

    public void ScheduleModUpdates(TimeSpan interval)
    {
        var task = new ScheduledTask
        {
            Name = "Update All Mods",
            NextRun = DateTime.Now.Add(interval),
            Interval = interval,
            Action = () => _updateService.UpdateAllModsAsync()
        };
        _tasks.Add(task);
        SaveTasks();
    }

    public void ScheduleBackup(string serverId, TimeSpan interval)
    {
        var task = new ScheduledTask
        {
            Name = $"Backup Server {serverId}",
            NextRun = DateTime.Now.Add(interval),
            Interval = interval,
            Action = () => _backupService.CreateBackupAsync(serverId)
        };
        _tasks.Add(task);
        SaveTasks();
    }

    private async void CheckTasks(object? state)
    {
        var now = DateTime.Now;
        var tasksToRun = _tasks.Where(t => t.IsEnabled && t.NextRun <= now).ToArray();
        
        foreach (var task in tasksToRun)
        {
            try
            {
                await task.Action().ConfigureAwait(false);
                task.NextRun = now.Add(task.Interval);
                _logger.LogInformation("Scheduled task executed: {TaskName}, Next run: {NextRun}", task.Name, task.NextRun);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute scheduled task: {TaskName}", task.Name);
            }
        }
        
        if (tasksToRun.Length > 0)
            SaveTasks();
    }

    public void RemoveTask(string taskId)
    {
        _tasks.RemoveAll(t => t.Id == taskId);
        SaveTasks();
    }
    
    public IReadOnlyList<ScheduledTask> GetTasks() => _tasks.AsReadOnly();
    
    private void LoadTasks()
    {
        try
        {
            if (File.Exists(_tasksFilePath))
            {
                var json = File.ReadAllText(_tasksFilePath);
                var taskData = JsonSerializer.Deserialize<List<ScheduledTaskData>>(json) ?? new();
                
                foreach (var data in taskData)
                {
                    Func<Task> action = data.Name switch
                    {
                        var n when n.StartsWith("Restart Server ") => () => _serverManager.RestartServerAsync(n.Replace("Restart Server ", "")),
                        "Update All Mods" => () => _updateService.UpdateAllModsAsync(),
                        var n when n.StartsWith("Backup Server ") => () => _backupService.CreateBackupAsync(n.Replace("Backup Server ", "")),
                        _ => () => Task.CompletedTask
                    };
                    
                    _tasks.Add(new ScheduledTask
                    {
                        Id = data.Id,
                        Name = data.Name,
                        NextRun = data.NextRun,
                        Interval = data.Interval,
                        IsEnabled = data.IsEnabled,
                        Action = action
                    });
                }
                
                _logger.LogInformation("Loaded {Count} scheduled tasks", _tasks.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load scheduled tasks");
        }
    }
    
    private void SaveTasks()
    {
        try
        {
            var taskData = _tasks.Select(t => new ScheduledTaskData
            {
                Id = t.Id,
                Name = t.Name,
                NextRun = t.NextRun,
                Interval = t.Interval,
                IsEnabled = t.IsEnabled
            }).ToList();
            
            var json = JsonSerializer.Serialize(taskData, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(_tasksFilePath)!);
            File.WriteAllText(_tasksFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save scheduled tasks");
        }
    }
    
    public void Dispose()
    {
        _timer?.Dispose();
        SaveTasks();
    }
}

public class ScheduledTaskData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime NextRun { get; set; }
    public TimeSpan Interval { get; set; }
    public bool IsEnabled { get; set; }
}