using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace ArmaServerManager.Core;

public class BackupService
{
    private readonly string _backupDirectory;
    private readonly SettingsService _settingsService;
    private readonly LoggingService _logger;
    private readonly ServerManager _serverManager;

    public BackupService(SettingsService settingsService, LoggingService logger, ServerManager serverManager)
    {
        _settingsService = settingsService;
        _logger = logger;
        _serverManager = serverManager;
        _backupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Backups");
        Directory.CreateDirectory(_backupDirectory);
    }

    public async Task CreateBackupAsync(string serverId)
    {
        try
        {
            var serverPath = Path.Combine(_settingsService.Settings.Directories.Servers, serverId);
            if (!Directory.Exists(serverPath))
            {
                _logger.LogWarning("Server path not found for backup: {ServerId}", serverId);
                return;
            }

            var backupName = $"{serverId}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            var backupPath = Path.Combine(_backupDirectory, backupName);
            var tempPath = Path.Combine(_backupDirectory, $"temp_{Guid.NewGuid():N}");

            await Task.Run(() =>
            {
                try
                {
                    Directory.CreateDirectory(tempPath);
                    CopyDirectory(serverPath, tempPath, true);
                    ZipFile.CreateFromDirectory(tempPath, backupPath, CompressionLevel.Optimal, false);
                }
                finally
                {
                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath, true);
                }
            }).ConfigureAwait(false);
            
            _logger.LogInformation("Backup created successfully: {BackupName}", backupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup for server: {ServerId}", serverId);
            throw;
        }
    }

    public async Task RestoreBackupAsync(string backupFileName, string serverId)
    {
        try
        {
            // Check if server is running and prevent restore
            if (_serverManager.IsServerRunning(serverId))
            {
                _logger.LogWarning("Cannot restore backup while server is running: {ServerId}", serverId);
                throw new InvalidOperationException($"Server {serverId} must be stopped before restoring backup");
            }

            var backupPath = Path.Combine(_backupDirectory, backupFileName);
            if (!File.Exists(backupPath))
            {
                _logger.LogWarning("Backup file not found: {BackupFileName}", backupFileName);
                return;
            }

            var serverPath = Path.Combine(_settingsService.Settings.Directories.Servers, serverId);
            
            // Create backup of current state before restore
            if (Directory.Exists(serverPath))
            {
                var preRestoreBackup = Path.Combine(_backupDirectory, $"{serverId}_pre_restore_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                await Task.Run(() => ZipFile.CreateFromDirectory(serverPath, preRestoreBackup)).ConfigureAwait(false);
                Directory.Delete(serverPath, true);
            }

            await Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(backupPath, serverPath);
            }).ConfigureAwait(false);
            
            _logger.LogInformation("Backup restored successfully: {BackupFileName} to {ServerId}", backupFileName, serverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup: {BackupFileName} for server: {ServerId}", backupFileName, serverId);
            throw;
        }
    }

    public string[] GetBackups() => Directory.GetFiles(_backupDirectory, "*.zip");

    public void DeleteBackup(string backupFileName)
    {
        var backupPath = Path.Combine(_backupDirectory, backupFileName);
        if (File.Exists(backupPath))
            File.Delete(backupPath);
    }

    public void CleanOldBackups(int keepDays = 30)
    {
        var cutoffDate = DateTime.Now.AddDays(-keepDays);
        foreach (var file in Directory.GetFiles(_backupDirectory, "*.zip"))
        {
            if (File.GetCreationTime(file) < cutoffDate)
                File.Delete(file);
        }
    }
    
    private void CopyDirectory(string sourceDir, string destDir, bool recursive)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        DirectoryInfo[] dirs = dir.GetDirectories();
        Directory.CreateDirectory(destDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            try
            {
                string targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, false);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Skipped locked file: {FileName}", file.Name);
            }
        }

        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}