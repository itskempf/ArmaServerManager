using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArmaServerManager.Core;
using System;
using System.Threading.Tasks;

namespace ArmaServerManager.UI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly SetupService _setupService;
    private readonly NotificationService _notificationService;

    [ObservableProperty]
    private string steamCmdPath = string.Empty;
    
    [ObservableProperty]
    private string statusMessage = "Ready";
    
    [ObservableProperty]
    private bool isSetupComplete;
    
    [ObservableProperty]
    private bool isVerifying;

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

    public SettingsViewModel(SettingsService settingsService, SetupService setupService, NotificationService notificationService)
    {
        _settingsService = settingsService;
        _setupService = setupService;
        _notificationService = notificationService;
        
        _setupService.StatusChanged += OnStatusChanged;
        _setupService.SetupCompleted += OnSetupCompleted;
        
        LoadSettings();
        _ = CheckSetupAsync();
    }
    
    private void OnStatusChanged(string status)
    {
        StatusMessage = status;
    }
    
    private void OnSetupCompleted(bool isComplete)
    {
        IsSetupComplete = isComplete;
        if (isComplete)
        {
            _notificationService.ShowSuccess("Setup Complete", "ArmaServerManager is ready to use!");
        }
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
        IsVerifying = true;
        StatusMessage = "Saving settings...";
        
        try
        {
            var steamCmdChanged = _settingsService.Settings.SteamCMD.Path != SteamCmdPath;
            
            _settingsService.UpdateSteamCmdPath(SteamCmdPath);
            _settingsService.UpdateSteamUsername(SteamUsername);
            _settingsService.UpdateTheme(SelectedTheme);
            _settingsService.UpdateDirectories(ServersDirectory, ModsDirectory, ConfigsDirectory, PresetsDirectory, LogsDirectory);
            
            _settingsService.Settings.Application.AutoStartServers = AutoStartServers;
            _settingsService.Settings.Application.MinimizeToTray = MinimizeToTray;
            _settingsService.Settings.Application.CheckUpdates = CheckUpdates;
            
            await _settingsService.SaveSettingsAsync();
            
            if (steamCmdChanged && !string.IsNullOrEmpty(SteamCmdPath))
            {
                await _setupService.ConfigureSteamCmdAsync(SteamCmdPath);
            }
            
            await CheckSetupAsync();
            _notificationService.ShowSuccess("Settings Saved", "Configuration saved successfully");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Save Failed", $"Failed to save settings: {ex.Message}");
        }
        finally
        {
            IsVerifying = false;
        }
    }
    
    [RelayCommand]
    private async Task CheckSetupAsync()
    {
        IsVerifying = true;
        await _setupService.CheckSetupAsync();
        IsVerifying = false;
    }

    [RelayCommand]
    private void BrowseSteamCmdPath()
    {
        try
        {
            using var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "SteamCMD Executable|steamcmd.exe|All Files|*.*",
                Title = "Select SteamCMD Executable"
            };
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SteamCmdPath = dialog.FileName;
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Browse Error", $"Failed to open file dialog: {ex.Message}");
        }
    }

    [RelayCommand]
    private void BrowseServersDirectory()
    {
        try
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Servers Directory"
            };
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ServersDirectory = dialog.SelectedPath;
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Browse Error", $"Failed to open folder dialog: {ex.Message}");
        }
    }

    [RelayCommand]
    private void BrowseModsDirectory()
    {
        try
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Mods Directory"
            };
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ModsDirectory = dialog.SelectedPath;
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Browse Error", $"Failed to open folder dialog: {ex.Message}");
        }
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