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
        try
        {
            this.InitializeComponent();
            Services = ConfigureServices();
        }
        catch (Exception ex)
        {
            LogStartupError(ex);
            throw;
        }
    }

    private void LogStartupError(Exception ex)
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.txt");
            var message = $"[{DateTime.Now}] CRITICAL STARTUP ERROR:\n{ex}\nStack Trace:\n{ex.StackTrace}\n\n";
            File.AppendAllText(path, message);
        }
        catch { /* Debugging fails, nothing to do */ }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        
        // Core Services
        // Core Services
        services.AddSingleton<SettingsService>(provider => new SettingsService());
        services.AddSingleton<SteamCMDHandler>(provider =>
        {
            var settings = provider.GetRequiredService<SettingsService>();
            var logger = provider.GetRequiredService<ILogger>();
            return new SteamCMDHandler(settings.Settings.SteamCMD.Path, logger);
        });
        services.AddSingleton<ISteamCMDHandler>(provider => provider.GetRequiredService<SteamCMDHandler>());

        services.AddSingleton<ServerManager>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger>();
            var settings = provider.GetRequiredService<SettingsService>();
            return new ServerManager(logger, settings);
        });
        services.AddSingleton<ConfigManager>(provider =>
        {
            var settings = provider.GetRequiredService<SettingsService>();
            var logger = provider.GetRequiredService<ILogger>();
            return new ConfigManager(settings.Settings.Directories.Configs, logger);
        });
        services.AddSingleton<ModManager>(provider =>
        {
            var settings = provider.GetRequiredService<SettingsService>();
            var steamCmd = provider.GetRequiredService<ISteamCMDHandler>();
            var logger = provider.GetRequiredService<ILogger>();
            return new ModManager(settings.Settings.Directories.Mods, steamCmd, logger);
        });
        services.AddSingleton<ResourceMonitor>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger>();
            return new ResourceMonitor(logger);
        });
        services.AddSingleton<ThemeService>();
        services.AddSingleton<UpdateService>(provider =>
        {
            var steamCmd = provider.GetRequiredService<ISteamCMDHandler>();
            var modManager = provider.GetRequiredService<ModManager>();
            var serverManager = provider.GetRequiredService<ServerManager>();
            var settings = provider.GetRequiredService<SettingsService>();
            var logger = provider.GetRequiredService<ILogger>();
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
        services.AddSingleton<SetupService>();
        services.AddSingleton<LoggingService>();
        services.AddSingleton<ILogger>(provider => provider.GetRequiredService<LoggingService>());
        services.AddSingleton<NotificationService>();
        services.AddSingleton<BackupService>(provider =>
        {
            var settings = provider.GetRequiredService<SettingsService>();
            var logger = provider.GetRequiredService<LoggingService>();
            var serverManager = provider.GetRequiredService<ServerManager>();
            return new BackupService(settings, logger, serverManager);
        });
        services.AddSingleton<SchedulerService>(provider =>
        {
            var serverManager = provider.GetRequiredService<ServerManager>();
            var updateService = provider.GetRequiredService<UpdateService>();
            var backupService = provider.GetRequiredService<BackupService>();
            var logger = provider.GetRequiredService<LoggingService>();
            return new SchedulerService(serverManager, updateService, backupService, logger);
        });
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
        services.AddTransient<ModsViewModel>(provider =>
        {
            var modManager = provider.GetRequiredService<ModManager>();
            var presetManager = provider.GetRequiredService<PresetManager>();
            var notificationService = provider.GetRequiredService<NotificationService>();
            return new ModsViewModel(modManager, presetManager, notificationService);
        });
        services.AddTransient<PresetsViewModel>(provider =>
        {
            var presetManager = provider.GetRequiredService<PresetManager>();
            var modManager = provider.GetRequiredService<ModManager>();
            var notificationService = provider.GetRequiredService<NotificationService>();
            return new PresetsViewModel(presetManager, modManager, notificationService);
        });
        services.AddTransient<SettingsViewModel>(provider =>
        {
            var settingsService = provider.GetRequiredService<SettingsService>();
            var setupService = provider.GetRequiredService<SetupService>();
            var notificationService = provider.GetRequiredService<NotificationService>();
            return new SettingsViewModel(settingsService, setupService, notificationService);
        });
        
        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            _window.Closed += OnWindowClosed;
            _window.Activate();
            
            // Initialize async services
            Task.Run(async () =>
            {
                try
                {
                    var pluginLoader = Services.GetService<PluginLoader>();
                    if (pluginLoader != null)
                        await pluginLoader.LoadPluginsAsync();
                }
                catch (Exception ex)
                {
                    LogStartupError(ex);
                }
            });
        }
        catch (Exception ex)
        {
            LogStartupError(ex);
            throw;
        }
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