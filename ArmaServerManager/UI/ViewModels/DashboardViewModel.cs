using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArmaServerManager.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ArmaServerManager.UI.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly ResourceMonitor _resourceMonitor;
    private readonly ServerManager _serverManager;
    private readonly NotificationService _notificationService;
    private readonly ILogger _logger;
    private readonly UpdateService _updateService;

    [ObservableProperty]
    private float cpuUsage;

    [ObservableProperty]
    private long availableRamMB;

    [ObservableProperty]
    private string systemUptime = "0d 0h 0m";

    public ObservableCollection<string> ActiveServers { get; } = new();
    
    [ObservableProperty]
    private bool isUpdating;
    
    [ObservableProperty]
    private string updateStatus = "No updates available";

    private System.Threading.Timer? _updateTimer;
    
    public DashboardViewModel(ResourceMonitor resourceMonitor, ServerManager serverManager, 
        NotificationService notificationService, ILogger logger, UpdateService updateService)
    {
        _resourceMonitor = resourceMonitor;
        _serverManager = serverManager;
        _notificationService = notificationService;
        _logger = logger;
        _updateService = updateService;
        
        // Subscribe to resource alerts
        _resourceMonitor.AlertTriggered += OnResourceAlert;
        _resourceMonitor.DataUpdated += OnResourceDataUpdated;
        
        // Initialize with sample data
        UpdateDashboardData();
        
        // Start real-time updates
        _updateTimer = new System.Threading.Timer(_ => UpdateDashboardData(), null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }
    
    private void UpdateDashboardData()
    {
        CpuUsage = _resourceMonitor.GetCpuUsage();
        AvailableRamMB = _resourceMonitor.GetAvailableRam() / (1024 * 1024);
        SystemUptime = _resourceMonitor.GetSystemUptime();
        UpdateActiveServers();
    }

    private void UpdateActiveServers()
    {
        ActiveServers.Clear();
        foreach (var server in _serverManager.Servers)
        {
            if (server.IsRunning)
                ActiveServers.Add(server.Name);
        }
    }



    [RelayCommand]
    private void InstallArmaServer()
    {
        _logger.LogInformation("User initiated Arma server installation");
        _notificationService.ShowInfo("Server Installation", "Navigate to Servers page to install a new server");
    }

    [RelayCommand]
    private void DownloadMods()
    {
        _logger.LogInformation("User navigated to mod downloads");
        _notificationService.ShowInfo("Mod Downloads", "Navigate to Mods page to download Workshop mods");
    }

    [RelayCommand]
    private void CreatePreset()
    {
        _logger.LogInformation("User initiated preset creation");
        _notificationService.ShowInfo("Create Preset", "Navigate to Presets page to create a new mod preset");
    }
    
    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        IsUpdating = true;
        UpdateStatus = "Checking for updates...";
        
        try
        {
            await _updateService.CheckForUpdatesAsync();
            var hasUpdates = _updateService.HasPendingUpdates;
            UpdateStatus = hasUpdates ? "Updates available" : "No updates available";
            
            if (hasUpdates)
                _notificationService.ShowInfo("Updates Available", "New updates are ready to install");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
            _notificationService.ShowError("Update Check Failed", ex.Message);
        }
        finally
        {
            IsUpdating = false;
        }
    }
    
    [RelayCommand]
    private async Task UpdateAllAsync()
    {
        IsUpdating = true;
        UpdateStatus = "Updating all components...";
        
        try
        {
            await _updateService.UpdateAllAsync();
            UpdateStatus = "All components updated";
            _notificationService.ShowSuccess("Update Complete", "All components have been updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update components");
            _notificationService.ShowError("Update Failed", ex.Message);
        }
        finally
        {
            IsUpdating = false;
        }
    }
    
    private void OnResourceAlert(ResourceAlert alert)
    {
        _notificationService.ShowWarning("Resource Alert", 
            $"Server {alert.ServerId}: {alert.AlertType} usage is {alert.CurrentValue:F1}% (threshold: {alert.Threshold:F1}%)");
    }
    
    private void OnResourceDataUpdated(ServerResourceData data)
    {
        // Update UI with latest resource data if needed
        if (data.ServerId == "System")
        {
            CpuUsage = data.CpuUsage;
            AvailableRamMB = data.MemoryUsage / (1024 * 1024);
        }
    }
    
    public void Dispose()
    {
        _updateTimer?.Dispose();
        _resourceMonitor.AlertTriggered -= OnResourceAlert;
        _resourceMonitor.DataUpdated -= OnResourceDataUpdated;
    }
}