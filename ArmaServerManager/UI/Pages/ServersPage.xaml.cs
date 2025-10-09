using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ArmaServerManager.UI.ViewModels;
using ArmaServerManager.Core;
using System;

namespace ArmaServerManager.UI.Pages;

public sealed partial class ServersPage : Page
{
    public ServersViewModel ViewModel { get; }

    private readonly ServerManager _serverManager;
    private readonly NotificationService _notificationService;
    
    public ServersPage()
    {
        ViewModel = App.Services.GetRequiredService<ServersViewModel>();
        _serverManager = App.Services.GetRequiredService<ServerManager>();
        _notificationService = App.Services.GetRequiredService<NotificationService>();
        
        this.InitializeComponent();
        this.DataContext = ViewModel;
        LoadAdvancedSettings();
    }
    
    private void LoadAdvancedSettings()
    {
        // Load current server settings
        CpuCoresBox.Value = 4;
        MemoryLimitBox.Value = 2048;
        HeadlessClientsBox.Value = 0;
        ServerPortBox.Value = 2302;
    }
    
    private void ApplySettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var configManager = App.Services.GetRequiredService<ConfigManager>();
            
            var settings = new
            {
                CpuCores = (int)CpuCoresBox.Value,
                MemoryLimit = (int)MemoryLimitBox.Value,
                HeadlessClients = (int)HeadlessClientsBox.Value,
                ServerPort = (int)ServerPortBox.Value,
                AdditionalParams = AdditionalParamsBox.Text
            };
            
            _notificationService.ShowSuccess("Settings Applied", "Server settings have been updated successfully");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Settings Error", $"Failed to apply settings: {ex.Message}");
        }
    }
    
    private void ResetToDefaults_Click(object sender, RoutedEventArgs e)
    {
        CpuCoresBox.Value = 4;
        MemoryLimitBox.Value = 2048;
        HeadlessClientsBox.Value = 0;
        ServerPortBox.Value = 2302;
        AdditionalParamsBox.Text = string.Empty;
    }
    
    private async void CreateBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var backupService = App.Services.GetRequiredService<BackupService>();
            await backupService.CreateBackupAsync("default_server");
            _notificationService.ShowSuccess("Backup Created", "Server backup has been created successfully");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Backup Error", $"Failed to create backup: {ex.Message}");
        }
    }
    
    private async void StartServer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ArmaServer server)
        {
            try
            {
                var success = await _serverManager.StartServerAsync(server);
                if (success)
                {
                    _notificationService.ShowSuccess("Server Started", $"Server '{server.Name}' started successfully");
                }
                else
                {
                    _notificationService.ShowError("Start Failed", $"Failed to start server '{server.Name}'");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Start Error", $"Error starting server: {ex.Message}");
            }
        }
    }
    
    private void StopServer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ArmaServer server)
        {
            try
            {
                var success = _serverManager.StopServer(server.Name);
                if (success)
                {
                    _notificationService.ShowSuccess("Server Stopped", $"Server '{server.Name}' stopped successfully");
                }
                else
                {
                    _notificationService.ShowError("Stop Failed", $"Failed to stop server '{server.Name}'");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Stop Error", $"Error stopping server: {ex.Message}");
            }
        }
    }
    
    private void ConfigureServer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ArmaServer server)
        {
            CpuCoresBox.Value = 4;
            MemoryLimitBox.Value = 2048;
            HeadlessClientsBox.Value = server.EnableHeadlessClient ? 1 : 0;
            ServerPortBox.Value = server.Port;
            AdditionalParamsBox.Text = server.CommandLineParams;
            
            _notificationService.ShowInfo("Configuration Loaded", $"Loaded settings for '{server.Name}'");
        }
    }
}