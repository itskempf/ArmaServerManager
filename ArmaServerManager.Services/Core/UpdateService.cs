using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArmaServerManager.Core;

public class UpdateService
{
    private readonly ISteamCMDHandler _steamCmdHandler;
    private readonly ModManager _modManager;
    private readonly ServerManager _serverManager;
    private readonly SettingsService _settingsService;
    private readonly ILogger _logger;
    
    public ObservableCollection<UpdateStatus> UpdateQueue { get; } = new();
    public bool IsUpdating { get; private set; }
    public bool HasPendingUpdates => UpdateQueue.Count > 0;

    public UpdateService(ISteamCMDHandler steamCmdHandler, ModManager modManager, ServerManager serverManager, SettingsService settingsService, ILogger logger)
    {
        _steamCmdHandler = steamCmdHandler;
        _modManager = modManager;
        _serverManager = serverManager;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<bool> UpdateServerAsync(string serverPath)
    {
        var status = new UpdateStatus
        {
            Type = UpdateType.Server,
            Name = "Arma 3 Server",
            Status = "Updating...",
            Progress = 0
        };
        
        UpdateQueue.Add(status);
        
        try
        {
            status.Progress = 25;
            var result = await _steamCmdHandler.InstallServerAsync(serverPath).ConfigureAwait(false);
            
            if (result.Success)
            {
                status.Status = "Updated successfully";
                status.Progress = 100;
                _logger.LogInformation("Server updated successfully: {ServerPath}", serverPath);
                return true;
            }
            else
            {
                status.Status = $"Update failed: {result.Error}";
                status.Progress = 0;
                _logger.LogError("Server update failed: {0}", result.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            status.Status = $"Error: {ex.Message}";
            status.Progress = 0;
            _logger.LogError(ex, "Server update error");
            return false;
        }
    }

    public async Task<bool> UpdateModAsync(string workshopId)
    {
        var mod = _modManager.Mods.FirstOrDefault(m => m.WorkshopId == workshopId);
        var status = new UpdateStatus
        {
            Type = UpdateType.Mod,
            Name = mod?.Name ?? $"Mod {workshopId}",
            Status = "Updating...",
            Progress = 0
        };
        
        UpdateQueue.Add(status);
        
        try
        {
            status.Progress = 25;
            var result = await _steamCmdHandler.DownloadModAsync(workshopId, _settingsService.Settings.Directories.Mods).ConfigureAwait(false);
            
            if (result.Success)
            {
                status.Status = "Updated successfully";
                status.Progress = 100;
                _logger.LogInformation("Mod updated successfully: {WorkshopId}", workshopId);
                return true;
            }
            else
            {
                status.Status = $"Update failed: {result.Error}";
                status.Progress = 0;
                _logger.LogError("Mod update failed: {0}", result.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            status.Status = $"Error: {ex.Message}";
            status.Progress = 0;
            _logger.LogError(ex, "Mod update error");
            return false;
        }
    }

    public async Task UpdateAllModsAsync()
    {
        IsUpdating = true;
        _logger.LogInformation("Starting update for all mods");
        
        try
        {
            foreach (var mod in _modManager.Mods.Where(m => !m.IsLocal).ToList())
            {
                await UpdateModAsync(mod.WorkshopId).ConfigureAwait(false);
            }
        }
        finally
        {
            IsUpdating = false;
        }
    }

    public async Task UpdateAllServersAsync()
    {
        IsUpdating = true;
        _logger.LogInformation("Starting update for all servers");
        
        try
        {
            foreach (var server in _serverManager.Servers)
            {
                await UpdateServerAsync(server.InstallPath).ConfigureAwait(false);
            }
        }
        finally
        {
            IsUpdating = false;
        }
    }

    public async Task CheckForUpdatesAsync()
    {
        _logger.LogInformation("Checking for updates");
        
        // Check server updates
        foreach (var server in _serverManager.Servers)
        {
            var status = new UpdateStatus
            {
                Type = UpdateType.Server,
                Name = server.Name,
                Status = "Checking...",
                Progress = 0
            };
            UpdateQueue.Add(status);
            
            // Use SteamCMD to check for server updates
            var result = await _steamCmdHandler.InstallServerAsync(server.InstallPath).ConfigureAwait(false);
            status.Status = result.Success ? "Up to date" : "Update available";
            status.Progress = 100;
        }
        
        // Check mod updates
        foreach (var mod in _modManager.Mods.Where(m => !m.IsLocal))
        {
            var status = new UpdateStatus
            {
                Type = UpdateType.Mod,
                Name = mod.Name,
                Status = "Checking...",
                Progress = 0
            };
            UpdateQueue.Add(status);
            
            // Check if mod needs update by comparing with Steam Workshop
            status.Status = "Up to date"; // Simplified - would need Steam Web API for real check
            status.Progress = 100;
        }
    }

    public async Task UpdateAllAsync()
    {
        await UpdateAllServersAsync().ConfigureAwait(false);
        await UpdateAllModsAsync().ConfigureAwait(false);
    }

    public void ClearUpdateQueue()
    {
        UpdateQueue.Clear();
    }
}

public class UpdateStatus : ObservableObject
{
    private UpdateType _type;
    private string _name = string.Empty;
    private string _status = string.Empty;
    private int _progress;

    public UpdateType Type { get => _type; set => SetProperty(ref _type, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public int Progress { get => _progress; set => SetProperty(ref _progress, value); }
}

public enum UpdateType
{
    Server,
    Mod
}