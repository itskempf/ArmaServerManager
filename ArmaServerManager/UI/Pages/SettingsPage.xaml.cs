using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ArmaServerManager.UI.ViewModels;
using ArmaServerManager.Core;
using System;
using System.Diagnostics;
using System.IO;

namespace ArmaServerManager.UI.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }
    private readonly PluginLoader _pluginLoader;
    private readonly SettingsService _settingsService;
    private readonly NotificationService _notificationService;

    public SettingsPage()
    {
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        _pluginLoader = App.Services.GetRequiredService<PluginLoader>();
        _settingsService = App.Services.GetRequiredService<SettingsService>();
        _notificationService = App.Services.GetRequiredService<NotificationService>();
        
        this.InitializeComponent();
        this.DataContext = ViewModel;
        
        LoadSettings();
        LoadPlugins();
    }
    
    private void LoadSettings()
    {
        SteamCmdPathBox.Text = _settingsService.Settings.SteamCMD.Path;
        SteamUsernameBox.Text = _settingsService.Settings.SteamCMD.Username;
        
        var themeIndex = _settingsService.Settings.Application.Theme switch
        {
            "Light" => 0,
            "Dark" => 1,
            _ => 2
        };
        ThemeComboBox.SelectedIndex = themeIndex;
    }

    private async void LoadPlugins()
    {
        await _pluginLoader.LoadPluginsAsync();
        PluginsList.ItemsSource = _pluginLoader.GetPlugins();
    }

    private async void ReloadPlugins_Click(object sender, RoutedEventArgs e)
    {
        await _pluginLoader.LoadPluginsAsync();
        PluginsList.ItemsSource = _pluginLoader.GetPlugins();
    }

    private async void ExecutePlugin_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string pluginName)
        {
            await _pluginLoader.ExecutePluginAsync(pluginName);
        }
    }

    private void ViewLogs_Click(object sender, RoutedEventArgs e)
    {
        var notificationService = App.Services.GetService(typeof(NotificationService)) as NotificationService;
        notificationService?.ShowInfo("Logs", "View Logs feature will be available in next update");
    }

    private void OpenPluginsFolder_Click(object sender, RoutedEventArgs e)
    {
        var pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        Directory.CreateDirectory(pluginsPath);
        Process.Start("explorer.exe", pluginsPath);
    }
    
    private async void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var steamCmdPath = SteamCmdPathBox.Text.Trim();
            if (!string.IsNullOrEmpty(steamCmdPath) && !File.Exists(steamCmdPath))
            {
                _notificationService.ShowWarning("Invalid Path", "SteamCMD executable not found at specified path");
                return;
            }
            
            _settingsService.UpdateSteamCmdPath(steamCmdPath);
            _settingsService.UpdateSteamUsername(SteamUsernameBox.Text.Trim());
            
            var selectedTheme = (ThemeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Dark";
            _settingsService.UpdateTheme(selectedTheme);
            
            await _settingsService.SaveSettingsAsync();
            
            _notificationService.ShowSuccess("Settings Saved", "Your settings have been saved successfully");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Save Failed", $"Failed to save settings: {ex.Message}");
        }
    }
}