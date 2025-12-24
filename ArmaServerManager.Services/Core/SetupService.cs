using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ArmaServerManager.Core;

public class SetupService
{
    private readonly SettingsService _settingsService;
    private readonly ISteamCMDHandler _steamCmdHandler;
    private readonly ILogger _logger;
    
    public event Action<string>? StatusChanged;
    public event Action<bool>? SetupCompleted;

    public SetupService(SettingsService settingsService, ISteamCMDHandler steamCmdHandler, ILogger logger)
    {
        _settingsService = settingsService;
        _steamCmdHandler = steamCmdHandler;
        _logger = logger;
    }

    public async Task<SetupStatus> CheckSetupAsync()
    {
        var status = new SetupStatus();
        
        NotifyStatus("Checking setup requirements...");
        
        // Check SteamCMD
        if (string.IsNullOrEmpty(_settingsService.Settings.SteamCMD.Path))
        {
            status.SteamCmdConfigured = false;
            status.Messages.Add("SteamCMD path not configured");
        }
        else if (!File.Exists(_settingsService.Settings.SteamCMD.Path))
        {
            status.SteamCmdConfigured = false;
            status.Messages.Add($"SteamCMD not found at: {_settingsService.Settings.SteamCMD.Path}");
        }
        else
        {
            NotifyStatus("Verifying SteamCMD...");
            status.SteamCmdConfigured = await _steamCmdHandler.VerifySteamCmdAsync();
            if (status.SteamCmdConfigured)
            {
                status.Messages.Add("SteamCMD verified successfully");
            }
            else
            {
                status.Messages.Add("SteamCMD verification failed");
            }
        }
        
        // Check directories
        status.DirectoriesConfigured = !string.IsNullOrEmpty(_settingsService.Settings.Directories.Servers);
        if (status.DirectoriesConfigured)
        {
            status.Messages.Add("Directories configured");
        }
        else
        {
            status.Messages.Add("Directories not configured");
        }
        
        status.IsComplete = status.SteamCmdConfigured && status.DirectoriesConfigured;
        
        if (status.IsComplete)
        {
            NotifyStatus("Setup complete - ready to manage servers!");
        }
        else
        {
            NotifyStatus("Setup incomplete - please configure SteamCMD in Settings");
        }
        
        SetupCompleted?.Invoke(status.IsComplete);
        return status;
    }

    public async Task<bool> ConfigureSteamCmdAsync(string steamCmdPath)
    {
        try
        {
            NotifyStatus($"Configuring SteamCMD: {steamCmdPath}");
            
            if (!File.Exists(steamCmdPath))
            {
                NotifyStatus("SteamCMD file not found at specified path");
                return false;
            }
            
            _settingsService.UpdateSteamCmdPath(steamCmdPath);
            await _settingsService.SaveSettingsAsync();
            
            NotifyStatus("Verifying SteamCMD configuration...");
            var verified = await _steamCmdHandler.VerifySteamCmdAsync();
            
            if (verified)
            {
                NotifyStatus("SteamCMD configured successfully!");
                return true;
            }
            else
            {
                NotifyStatus("SteamCMD verification failed");
                return false;
            }
        }
        catch (Exception ex)
        {
            NotifyStatus($"Error configuring SteamCMD: {ex.Message}");
            _logger.LogError(ex, "Failed to configure SteamCMD");
            return false;
        }
    }
    
    private void NotifyStatus(string status)
    {
        StatusChanged?.Invoke(status);
        _logger.LogInformation(status);
    }
}

public class SetupStatus
{
    public bool SteamCmdConfigured { get; set; }
    public bool DirectoriesConfigured { get; set; }
    public bool IsComplete { get; set; }
    public List<string> Messages { get; set; } = new();
}