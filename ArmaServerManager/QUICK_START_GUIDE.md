# ArmaServerManager - Quick Start Guide

## Prerequisites

1. **Windows 10/11** (64-bit)
2. **.NET 8 Runtime** or SDK
3. **Visual Studio 2022** (for development)
4. **SteamCMD** - Download from: https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip

## Initial Setup

### 1. Install SteamCMD
```
1. Download SteamCMD from the link above
2. Extract to a folder (e.g., C:\SteamCMD)
3. Run steamcmd.exe once to initialize
4. Close SteamCMD after initialization
```

### 2. Configure Application
```
1. Launch ArmaServerManager
2. Go to Settings
3. Set SteamCMD Path: C:\SteamCMD\steamcmd.exe
4. Configure directories:
   - Servers: C:\ArmaServers\Servers
   - Mods: C:\ArmaServers\Mods
   - Configs: C:\ArmaServers\Configs
   - Presets: C:\ArmaServers\Presets
   - Logs: C:\ArmaServers\Logs
5. Save settings
```

## Core Service Usage

### ServerManager
```csharp
// Get service
var serverManager = App.Services.GetService<ServerManager>();

// Create a server
var server = new ArmaServer
{
    Name = "My Server",
    InstallPath = @"C:\ArmaServers\Servers\MyServer",
    Port = 2302,
    MaxPlayers = 64,
    Password = "mypass",
    AdminPassword = "adminpass"
};

// Add server
serverManager.AddServer(server);

// Start server
await serverManager.StartServerAsync(server);

// Stop server
serverManager.StopServer(server.Name);

// Check status
var status = serverManager.GetServerStatus(server.Name);
```

### SteamCMDHandler
```csharp
// Get service
var steamCmd = App.Services.GetService<SteamCMDHandler>();

// Install Arma 3 Server
var result = await steamCmd.InstallServerAsync(@"C:\ArmaServers\Servers\MyServer");

// Download Workshop Mod
var modResult = await steamCmd.DownloadModAsync("450814997", @"C:\ArmaServers\Mods");

// Check if SteamCMD is available
if (steamCmd.IsSteamCmdAvailable())
{
    // Ready to use
}
```

### ModManager
```csharp
// Get service
var modManager = App.Services.GetService<ModManager>();

// Install Workshop mod
await modManager.InstallModAsync("450814997"); // CBA_A3

// Add local mod
await modManager.AddLocalModAsync(@"C:\MyMods\CustomMod", "Custom Mod");

// Verify mod installation
bool isValid = modManager.VerifyModInstallation("450814997");

// Get mod size
long size = modManager.GetModSize("450814997");

// Get mod keys
string keys = modManager.GetModKeys("450814997");

// Remove mod
await modManager.RemoveModAsync("450814997");
```

### ConfigManager
```csharp
// Get service
var configManager = App.Services.GetService<ConfigManager>();

// Generate default config
var server = configManager.GenerateDefaultConfig("MyServer", @"C:\ArmaServers\Servers\MyServer");

// Save config
await configManager.SaveConfigAsync(server);

// Load config
var loadedServer = await configManager.LoadConfigAsync("MyServer");
```

### PresetManager
```csharp
// Get service
var presetManager = App.Services.GetService<PresetManager>();

// Create preset
var preset = new ModPreset
{
    Name = "My Preset",
    Description = "Essential mods",
    ModIds = new List<string> { "450814997", "463939057" }
};

// Save preset
await presetManager.SavePresetAsync(preset);

// Load preset
var loadedPreset = await presetManager.LoadPresetAsync("My Preset");

// Export to HTML (Arma 3 Launcher format)
string htmlPath = await presetManager.ExportPresetToHtmlAsync(preset);

// Import from HTML
var importedPreset = await presetManager.ImportPresetFromHtmlAsync(@"C:\Presets\preset.html");
```

### BackupService
```csharp
// Get service
var backupService = App.Services.GetService<BackupService>();

// Create backup
await backupService.CreateBackupAsync("MyServer");

// Restore backup
await backupService.RestoreBackupAsync("MyServer_20240101_120000.zip", "MyServer");

// Get all backups
string[] backups = backupService.GetBackups();

// Clean old backups (older than 30 days)
backupService.CleanOldBackups(30);
```

### SchedulerService
```csharp
// Get service
var scheduler = App.Services.GetService<SchedulerService>();

// Schedule server restart every 6 hours
scheduler.ScheduleServerRestart("MyServer", TimeSpan.FromHours(6));

// Schedule mod updates daily
scheduler.ScheduleModUpdates(TimeSpan.FromDays(1));

// Schedule backup every 12 hours
scheduler.ScheduleBackup("MyServer", TimeSpan.FromHours(12));

// Get all tasks
var tasks = scheduler.GetTasks();

// Remove task
scheduler.RemoveTask(taskId);
```

### ResourceMonitor
```csharp
// Get service
var monitor = App.Services.GetService<ResourceMonitor>();

// Get system metrics
float cpuUsage = monitor.GetCpuUsage();
long availableRam = monitor.GetAvailableRam();
string uptime = monitor.GetSystemUptime();

// Get server data
var serverData = monitor.GetServerData("MyServer", maxPoints: 100);

// Subscribe to alerts
monitor.AlertTriggered += (alert) =>
{
    Console.WriteLine($"Alert: {alert.AlertType} - {alert.CurrentValue}");
};

// Set thresholds
monitor.CpuAlertThreshold = 80.0f;
monitor.MemoryAlertThreshold = 1024 * 1024 * 1024; // 1GB
```

### UpdateService
```csharp
// Get service
var updateService = App.Services.GetService<UpdateService>();

// Update server
await updateService.UpdateServerAsync(@"C:\ArmaServers\Servers\MyServer");

// Update specific mod
await updateService.UpdateModAsync("450814997");

// Update all mods
await updateService.UpdateAllModsAsync();

// Update all servers
await updateService.UpdateAllServersAsync();

// Clear update queue
updateService.ClearUpdateQueue();
```

### NotificationService
```csharp
// Get service
var notifications = App.Services.GetService<NotificationService>();

// Show notifications
notifications.ShowInfo("Info", "Server started successfully");
notifications.ShowSuccess("Success", "Mod installed");
notifications.ShowWarning("Warning", "High CPU usage detected");
notifications.ShowError("Error", "Failed to connect to Steam");

// Subscribe to notifications
notifications.NotificationRequested += (notification) =>
{
    // Display in UI
};
```

### LoggingService
```csharp
// Get service
var logger = App.Services.GetService<LoggingService>();

// Log messages
logger.LogInformation("Server started");
logger.LogWarning("High memory usage");
logger.LogError(exception, "Failed to start server");

// Access log entries (for UI binding)
var logs = logger.LogEntries;

// Read log file
string logContent = await logger.ReadLogFileAsync();
```

### ThemeService
```csharp
// Get service
var themeService = App.Services.GetService<ThemeService>();

// Initialize with window
themeService.Initialize(mainWindow);

// Apply theme
themeService.ApplyTheme("Dark");
themeService.ApplyTheme("Light");

// Get current theme
string currentTheme = themeService.GetCurrentTheme();
```

### SteamAuthManager
```csharp
// Get service
var authManager = App.Services.GetService<SteamAuthManager>();

// Save credentials (encrypted)
var credentials = new SteamCredentials
{
    Username = "myusername",
    GuardToken = "XXXXX",
    RememberCredentials = true
};
authManager.SaveCredentials(credentials);

// Load credentials
var loaded = authManager.LoadCredentials();

// Clear credentials
authManager.ClearCredentials();
```

## Utilities

### Format Helpers
```csharp
// Format bytes
string size = Utilities.FormatBytes(1024 * 1024 * 500); // "500 MB"

// Format uptime
string uptime = Utilities.FormatUptime(TimeSpan.FromHours(25)); // "1d 1h 0m"
```

### Validation
```csharp
// Validate Workshop ID
bool valid = Utilities.IsValidWorkshopId("450814997");

// Validate port
bool validPort = Utilities.IsValidPort(2302);

// Validate server name
bool validName = Utilities.IsValidServerName("My Server");
```

### File Operations
```csharp
// Sanitize filename
string safe = Utilities.SanitizeFileName("My:Server*Name"); // "My_Server_Name"

// Get directory size
long size = Utilities.GetDirectorySize(@"C:\ArmaServers\Mods");

// Check if process is running
bool running = Utilities.IsProcessRunning(processId);
```

## Constants

```csharp
// Application
Constants.AppName // "Arma Server Manager"
Constants.AppVersion // "1.0.0"

// Steam
Constants.Arma3AppId // 233780
Constants.Arma3WorkshopId // 107410
Constants.SteamWorkshopUrl // Base URL

// Server
Constants.DefaultServerPort // 2302
Constants.DefaultMaxPlayers // 64
Constants.ServerExecutable // "arma3server_x64.exe"

// Timeouts
Constants.SteamCmdTimeoutMinutes // 30
Constants.ServerStartTimeoutSeconds // 30
```

## Common Workflows

### Setup New Server
```csharp
1. Install server files via SteamCMD
2. Create server configuration
3. Add server to ServerManager
4. Start server
5. Monitor with ResourceMonitor
```

### Install Mod Pack
```csharp
1. Create or import preset
2. Install each mod via ModManager
3. Verify installations
4. Apply preset to server
```

### Scheduled Maintenance
```csharp
1. Schedule daily backups
2. Schedule weekly mod updates
3. Schedule server restarts
4. Monitor via alerts
```

## Troubleshooting

### SteamCMD Issues
- Ensure SteamCMD path is correct
- Run SteamCMD manually first to initialize
- Check for Steam Guard requirements
- Verify internet connection

### Server Won't Start
- Check if port is available
- Verify server files are installed
- Check server.cfg syntax
- Review logs in LoggingService

### Mod Installation Fails
- Verify Workshop ID is correct
- Check SteamCMD is working
- Ensure sufficient disk space
- Check mod permissions

## Best Practices

1. **Always backup** before major changes
2. **Schedule regular updates** for mods and server
3. **Monitor resources** to prevent crashes
4. **Use presets** for consistent mod configurations
5. **Enable logging** for troubleshooting
6. **Test configurations** before production use
7. **Keep SteamCMD updated**
8. **Use profiles** for different server setups

## Support

- Check logs in: `Data/Logs/manager.log`
- Review documentation in project README
- Check CORE_SERVICES_IMPLEMENTATION.md for technical details
