using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using ArmaServerManager.Core;
using ArmaServerManager.UI.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ArmaServerManager;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    private Window? _window;

    public App()
    {
        this.InitializeComponent();
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        
        // Core Services
        services.AddSingleton<SettingsService>();
        services.AddSingleton<SteamCMDHandler>(provider =>
        {
            var settings = provider.GetRequiredService<SettingsService>();
            var logger = provider.GetRequiredService<LoggingService>();
            return new SteamCMDHandler(settings.Settings.SteamCMD.Path, logger);
        });
        services.AddSingleton<ServerManager>();
        services.AddSingleton<ConfigManager>(provider =>
        {
            var settings = provider.GetRequiredService<SettingsService>();
            var logger = provider.GetRequiredService<LoggingService>();
            return new ConfigManager(settings.Settings.Directories.Configs, logger);
        });
        services.AddSingleton<ModManager>(provider =>
        {
            var settings = provider.GetRequiredService<SettingsService>();
            var steamCmd = provider.GetRequiredService<SteamCMDHandler>();
            var logger = provider.GetRequiredService<LoggingService>();
            return new ModManager(settings.Settings.Directories.Mods, steamCmd, logger);
        });
        services.AddSingleton<ResourceMonitor>(provider =>
        {
            var logger = provider.GetRequiredService<LoggingService>();
            return new ResourceMonitor(logger);
        });
        services.AddSingleton<ThemeService>();
        services.AddSingleton<UpdateService>(provider =>
        {
            var steamCmd = provider.GetRequiredService<SteamCMDHandler>();
            var modManager = provider.GetRequiredService<ModManager>();
            var serverManager = provider.GetRequiredService<ServerManager>();
            var settings = provider.GetRequiredService<SettingsService>();
            var logger = provider.GetRequiredService<LoggingService>();
            return new UpdateService(steamCmd, modManager, serverManager, settings, logger);
        });
        services.AddSingleton<ProfileManager>(provider =>
        {
            var configManager = provider.GetRequiredService<ConfigManager>();
            var settings = provider.GetRequiredService<SettingsService>();
            var logger = provider.GetRequiredService<LoggingService>();
            return new ProfileManager(configManager, settings, logger);
        });
        services.AddSingleton<SteamAuthManager>(provider =>
        {
            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            return new SteamAuthManager(dataDir);
        });
        services.AddSingleton<LoggingService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<BackupService>();
        services.AddSingleton<SchedulerService>();
        services.AddSingleton<PresetManager>(provider =>
        {
            var settings = provider.GetRequiredService<SettingsService>();
            var modManager = provider.GetRequiredService<ModManager>();
            var logger = provider.GetRequiredService<LoggingService>();
            return new PresetManager(settings.Settings.Directories.Presets, modManager, logger);
        });
        services.AddSingleton<PluginLoader>(provider =>
        {
            var logger = provider.GetRequiredService<LoggingService>();
            return new PluginLoader(provider, logger);
        });
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        // ViewModels
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ServersViewModel>();
        services.AddTransient<ModsViewModel>();
        services.AddTransient<SettingsViewModel>();
        
        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Closed += OnWindowClosed;
        _window.Activate();
        
        // Initialize async services
        Task.Run(async () =>
        {
            var pluginLoader = Services.GetService<PluginLoader>();
            if (pluginLoader != null)
                await pluginLoader.LoadPluginsAsync();
        });
    }
    
    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        try
        {
            var serverManager = Services.GetService<ServerManager>();
            serverManager?.Cleanup();
            
            var resourceMonitor = Services.GetService<ResourceMonitor>();
            resourceMonitor?.Dispose();
            
            var schedulerService = Services.GetService<SchedulerService>();
            schedulerService?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
        }
    }
}