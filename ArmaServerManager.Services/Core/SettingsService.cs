using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArmaServerManager.Core;

public class SettingsService
{
    private readonly string _settingsFilePath;
    public AppSettings Settings { get; private set; } = new();

    public SettingsService(string? dataDirectory = null)
    {
        var dataDir = dataDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDir);
        _settingsFilePath = Path.Combine(dataDir, "settings.json");
        LoadSettings();
        InitializeDefaultPaths();
    }
    
    private void InitializeDefaultPaths()
    {
        if (!string.IsNullOrEmpty(Settings.SteamCMD.Path) && string.IsNullOrEmpty(Settings.SteamCMD.InstallDirectory))
        {
            var steamCmdDir = Path.GetDirectoryName(Settings.SteamCMD.Path);
            if (!string.IsNullOrEmpty(steamCmdDir))
            {
                Settings.SteamCMD.InstallDirectory = Path.Combine(steamCmdDir, "ArmaServers");
                Settings.Directories.Servers = Path.Combine(Settings.SteamCMD.InstallDirectory, "Servers");
                Settings.Directories.Mods = Path.Combine(Settings.SteamCMD.InstallDirectory, "Mods");
                Settings.Directories.Configs = Path.Combine(Settings.SteamCMD.InstallDirectory, "Configs");
                Settings.Directories.Presets = Path.Combine(Settings.SteamCMD.InstallDirectory, "Presets");
                Settings.Directories.Logs = Path.Combine(Settings.SteamCMD.InstallDirectory, "Logs");
            }
        }
    }

    public void UpdateSteamCmdPath(string path)
    {
        Settings.SteamCMD.Path = path;
        InitializeDefaultPaths();
    }

    public void UpdateSteamUsername(string username)
    {
        Settings.SteamCMD.Username = username;
    }

    public void UpdateTheme(string theme)
    {
        Settings.Application.Theme = theme;
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save settings: {ex.Message}", ex);
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            Settings = new AppSettings();
        }
    }

    public void UpdateDirectories(string servers, string mods, string configs, string presets, string logs)
    {
        Settings.Directories.Servers = servers;
        Settings.Directories.Mods = mods;
        Settings.Directories.Configs = configs;
        Settings.Directories.Presets = presets;
        Settings.Directories.Logs = logs;
    }
}

public class AppSettings
{
    public SteamCMDSettings SteamCMD { get; set; } = new();
    public DirectorySettings Directories { get; set; } = new();
    public ApplicationSettings Application { get; set; } = new();
}

public class SteamCMDSettings
{
    public string Path { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string InstallDirectory { get; set; } = string.Empty;
}

public class DirectorySettings
{
    public string Servers { get; set; } = string.Empty;
    public string Mods { get; set; } = string.Empty;
    public string Configs { get; set; } = string.Empty;
    public string Presets { get; set; } = string.Empty;
    public string Logs { get; set; } = string.Empty;
}

public class ApplicationSettings
{
    public bool AutoStartServers { get; set; }
    public bool MinimizeToTray { get; set; }
    public bool CheckUpdates { get; set; } = true;
    public string Theme { get; set; } = "Dark";
}