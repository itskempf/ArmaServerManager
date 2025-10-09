using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArmaServerManager.Core;
using System.Threading.Tasks;

namespace ArmaServerManager.UI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private string steamCmdPath = string.Empty;

    [ObservableProperty]
    private string steamUsername = string.Empty;

    [ObservableProperty]
    private string serversDirectory = string.Empty;

    [ObservableProperty]
    private string modsDirectory = string.Empty;

    [ObservableProperty]
    private string configsDirectory = string.Empty;

    [ObservableProperty]
    private string presetsDirectory = string.Empty;

    [ObservableProperty]
    private string logsDirectory = string.Empty;

    [ObservableProperty]
    private bool autoStartServers;

    [ObservableProperty]
    private bool minimizeToTray;

    [ObservableProperty]
    private bool checkUpdates;

    [ObservableProperty]
    private string selectedTheme = "Dark";

    [ObservableProperty]
    private string selectedBranch = "Stable";

    public string[] AvailableThemes { get; } = { "Light", "Dark", "Default" };
    public string[] AvailableBranches { get; } = { "Stable", "Dev", "Community" };

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Settings;
        
        SteamCmdPath = settings.SteamCMD.Path;
        SteamUsername = settings.SteamCMD.Username;
        ServersDirectory = settings.Directories.Servers;
        ModsDirectory = settings.Directories.Mods;
        ConfigsDirectory = settings.Directories.Configs;
        PresetsDirectory = settings.Directories.Presets;
        LogsDirectory = settings.Directories.Logs;
        AutoStartServers = settings.Application.AutoStartServers;
        MinimizeToTray = settings.Application.MinimizeToTray;
        CheckUpdates = settings.Application.CheckUpdates;
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        _settingsService.UpdateSteamCmdPath(SteamCmdPath);
        _settingsService.UpdateSteamUsername(SteamUsername);
        _settingsService.UpdateTheme(SelectedTheme);
        _settingsService.UpdateDirectories(ServersDirectory, ModsDirectory, ConfigsDirectory, PresetsDirectory, LogsDirectory);
        
        _settingsService.Settings.Application.AutoStartServers = AutoStartServers;
        _settingsService.Settings.Application.MinimizeToTray = MinimizeToTray;
        _settingsService.Settings.Application.CheckUpdates = CheckUpdates;
        
        await _settingsService.SaveSettingsAsync();
    }

    [RelayCommand]
    private void BrowseSteamCmdPath()
    {
        // Open file picker for steamcmd.exe
    }

    [RelayCommand]
    private void BrowseServersDirectory()
    {
        // Open folder picker
    }

    [RelayCommand]
    private void BrowseModsDirectory()
    {
        // Open folder picker
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        var defaultSettings = new AppSettings();
        SteamCmdPath = defaultSettings.SteamCMD.Path;
        SteamUsername = defaultSettings.SteamCMD.Username;
        ServersDirectory = defaultSettings.Directories.Servers;
        ModsDirectory = defaultSettings.Directories.Mods;
        ConfigsDirectory = defaultSettings.Directories.Configs;
        PresetsDirectory = defaultSettings.Directories.Presets;
        LogsDirectory = defaultSettings.Directories.Logs;
        AutoStartServers = defaultSettings.Application.AutoStartServers;
        MinimizeToTray = defaultSettings.Application.MinimizeToTray;
        CheckUpdates = defaultSettings.Application.CheckUpdates;
    }
}