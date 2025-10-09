using ArmaServerManager.Core;
using System;
using System.Threading.Tasks;

namespace ArmaServerManager.Plugins;

public class SamplePlugin : PluginBase
{
    public override string Name => "Sample Plugin";
    public override string Version => "1.0.0";
    public override string Description => "A sample plugin demonstrating the plugin system";
    public override string Author => "ArmaServerManager Team";

    public override async Task ExecuteAsync()
    {
        // Sample plugin functionality
        await Task.Delay(1000);
        
        if (ServiceProvider != null)
        {
            var notificationService = ServiceProvider.GetService(typeof(NotificationService)) as NotificationService;
            notificationService?.ShowSuccess("Plugin Executed", "Sample plugin executed successfully!");
        }
    }
    
    public override async Task OnServerStartedAsync(string serverId)
    {
        if (ServiceProvider != null)
        {
            var notificationService = ServiceProvider.GetService(typeof(NotificationService)) as NotificationService;
            notificationService?.ShowInfo("Server Started", $"Plugin detected server start: {serverId}");
        }
        await Task.CompletedTask;
    }
    
    public override async Task OnServerStoppedAsync(string serverId)
    {
        if (ServiceProvider != null)
        {
            var notificationService = ServiceProvider.GetService(typeof(NotificationService)) as NotificationService;
            notificationService?.ShowInfo("Server Stopped", $"Plugin detected server stop: {serverId}");
        }
        await Task.CompletedTask;
    }
}