using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ArmaServerManager.Core;

/// <summary>
/// Manages Arma 3 mods, including installation, discovery, and configuration.
/// </summary>
public class ModManager
{
    private const string Arma3AppId = "107410";
    private readonly string _modsPath;
    private readonly ISteamCMDHandler _steamCmdHandler;
    private readonly ILogger _logger;
    private readonly string _modsConfigPath;
    
    /// <summary>
    /// Collection of currently loaded mods.
    /// </summary>
    public ObservableCollection<ArmaMod> Mods { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ModManager"/> class.
    /// </summary>
    /// <param name="modsPath">The root directory for mods.</param>
    /// <param name="steamCmdHandler">Handler for SteamCMD operations.</param>
    /// <param name="logger">Service for logging.</param>
    public ModManager(string modsPath, ISteamCMDHandler steamCmdHandler, ILogger logger)
    {
        _modsPath = string.IsNullOrEmpty(modsPath) ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Mods") : modsPath;
        _steamCmdHandler = steamCmdHandler;
        _logger = logger;
        _modsConfigPath = Path.Combine(_modsPath, "mods.json");
        
        Directory.CreateDirectory(_modsPath);
        LoadInstalledMods();
    }

    /// <summary>
    /// Installs a mod from the Steam Workshop.
    /// </summary>
    /// <param name="workshopId">The Steam Workshop ID of the mod.</param>
    /// <returns>True if installation is successful, otherwise false.</returns>
    public async Task<bool> InstallModAsync(string workshopId)
    {
        try
        {
            _logger.LogInformation("Installing mod: {WorkshopId}", workshopId);
            
            var result = await _steamCmdHandler.DownloadModAsync(workshopId, _modsPath).ConfigureAwait(false);
            if (!result.Success)
            {
                _logger.LogError("Failed to download mod {0}: {1}", workshopId, result.Error);
                return false;
            }

            var modPath = Path.Combine(_modsPath, "steamapps", "workshop", "content", Arma3AppId, workshopId);
            var modName = GetModName(modPath) ?? $"Workshop Mod {workshopId}";
            
            var mod = new ArmaMod
            {
                WorkshopId = workshopId,
                Name = modName,
                Path = modPath,
                IsEnabled = true,
                IsLocal = false
            };
            
            Mods.Add(mod);
            await SaveModsConfigAsync().ConfigureAwait(false);
            
            _logger.LogInformation("Mod installed successfully: {ModName} ({WorkshopId})", modName, workshopId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install mod: {WorkshopId}", workshopId);
            return false;
        }
    }

    /// <summary>
    /// Adds a local mod from a specified path.
    /// </summary>
    /// <param name="modPath">Path to the local mod directory.</param>
    /// <param name="modName">Name of the mod.</param>
    /// <returns>True if successful, otherwise false.</returns>
    public async Task<bool> AddLocalModAsync(string modPath, string modName)
    {
        try
        {
            if (!Directory.Exists(modPath))
            {
                _logger.LogWarning("Local mod path does not exist: {ModPath}", modPath);
                return false;
            }

            var mod = new ArmaMod
            {
                WorkshopId = $"local_{modName}",
                Name = modName,
                Path = modPath,
                IsEnabled = true,
                IsLocal = true
            };
            
            Mods.Add(mod);
            await SaveModsConfigAsync().ConfigureAwait(false);
            
            _logger.LogInformation("Local mod added: {ModName}", modName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add local mod: {ModPath}", modPath);
            return false;
        }
    }

    /// <summary>
    /// Removes a mod from the manager.
    /// </summary>
    /// <param name="workshopId">Workshop ID of the mod to remove.</param>
    public async Task RemoveModAsync(string workshopId)
    {
        var mod = Mods.FirstOrDefault(m => m.WorkshopId == workshopId);
        if (mod != null)
        {
            Mods.Remove(mod);
            await SaveModsConfigAsync().ConfigureAwait(false);
            _logger.LogInformation("Mod removed: {ModName}", mod.Name);
        }
    }

    /// <summary>
    /// Loads all installed mods from the configuration file or discovers them from the directory.
    /// </summary>
    public void LoadInstalledMods()
    {
        try
        {
            if (File.Exists(_modsConfigPath))
            {
                    var json = File.ReadAllText(_modsConfigPath);
                var mods = JsonSerializer.Deserialize<ArmaMod[]>(json) ?? Array.Empty<ArmaMod>();
                
                Mods.Clear();
                foreach (var mod in mods)
                {
                    Mods.Add(mod);
                }
            }
            else
            {
                LoadWorkshopMods();
                LoadLocalMods();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load mods configuration");
        }
    }
    
    private void LoadWorkshopMods()
    {
        var workshopPath = Path.Combine(_modsPath, "steamapps", "workshop", "content", Arma3AppId);
        if (!Directory.Exists(workshopPath)) return; 
        
        foreach (var modDir in Directory.GetDirectories(workshopPath))
        {
            var workshopId = Path.GetFileName(modDir);
            var modName = GetModName(modDir) ?? $"Workshop Mod {workshopId}";
            
            Mods.Add(new ArmaMod
            {
                WorkshopId = workshopId,
                Name = modName,
                Path = modDir,
                IsEnabled = true,
                IsLocal = false
            });
        }
    }
    
    private void LoadLocalMods()
    {
        var localModsPath = Path.Combine(_modsPath, "Local");
        if (!Directory.Exists(localModsPath)) return; 
        
        foreach (var modDir in Directory.GetDirectories(localModsPath))
        {
            var modName = Path.GetFileName(modDir);
            
            Mods.Add(new ArmaMod
            {
                WorkshopId = $"local_{Path.GetFileName(modDir)}",
                Name = modName,
                Path = modDir,
                IsEnabled = true,
                IsLocal = true
            });
        }
    }
    
    private string? GetModName(string modPath)
    {
        var metaFile = Path.Combine(modPath, "meta.cpp");
        if (File.Exists(metaFile))
        {
            var content = File.ReadAllText(metaFile);
            // Simple parsing for name = "ModName";
            var nameStart = content.IndexOf("name = \"");
            if (nameStart >= 0)
            {
                nameStart += 8;
                var nameEnd = content.IndexOf("\"", nameStart);
                if (nameEnd > nameStart)
                {
                    return content.Substring(nameStart, nameEnd - nameStart);
                }
            }
        }
        return null;
    }
    
    private async Task SaveModsConfigAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(Mods.ToArray(), new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_modsConfigPath, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save mods configuration");
        }
    }
    
    /// <summary>
    /// Verifies if a mod is properly installed by checking its directory and for .pbo files.
    /// </summary>
    /// <param name="workshopId">Workshop ID of the mod to verify.</param>
    /// <returns>True if the mod appears to be installed correctly.</returns>
    public bool VerifyModInstallation(string workshopId)
    {
        var modPath = Path.Combine(_modsPath, "steamapps", "workshop", "content", Arma3AppId, workshopId);
        return Directory.Exists(modPath) && Directory.GetFiles(modPath, "*.pbo", SearchOption.AllDirectories).Length > 0;
    }
    
    /// <summary>
    /// Calculates the total size of a mod on disk.
    /// </summary>
    /// <param name="workshopId">Workshop ID of the mod.</param>
    /// <returns>The total size of the mod in bytes.</returns>
    public long GetModSize(string workshopId)
    {
        var modPath = Path.Combine(_modsPath, "steamapps", "workshop", "content", Arma3AppId, workshopId);
        if (!Directory.Exists(modPath))
            return 0;
        
        try
        {
            return new DirectoryInfo(modPath)
                .GetFiles("*", SearchOption.AllDirectories)
                .Sum(file => file.Length);
        }
        catch
        {
            return 0;
        }
    }
    
    /// <summary>
    /// Gets a string listing all .bikey files for a given mod.
    /// </summary>
    /// <param name="workshopId">Workshop ID of the mod.</param>
    /// <returns>A comma-separated string of key file names, or a not found message.</returns>
    public string GetModKeys(string workshopId)
    {
        var modPath = Path.Combine(_modsPath, "steamapps", "workshop", "content", Arma3AppId, workshopId);
        var keysPath = Path.Combine(modPath, "Keys");
        
        if (Directory.Exists(keysPath))
        {
            var keyFiles = Directory.GetFiles(keysPath, "*.bikey");
            return string.Join(", ", keyFiles.Select(Path.GetFileName));
        }
        
        return "No keys found";
    }
    
    /// <summary>
    /// Updates a mod by re-downloading it.
    /// </summary>
    /// <param name="workshopId">Workshop ID of the mod to update.</param>
    /// <returns>True if the update is successful, otherwise false.</returns>
    public async Task<bool> UpdateModAsync(string workshopId)
    {
        _logger.LogInformation("Updating mod: {WorkshopId}", workshopId);
        return await InstallModAsync(workshopId).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Copies all .bikey files from a mod's 'Keys' folder to a server's 'keys' folder.
    /// </summary>
    /// <param name="workshopId">Workshop ID of the mod.</param>
    /// <param name="serverKeysPath">Path to the server's 'keys' directory.</param>
    public void CopyModKeysToServer(string workshopId, string serverKeysPath)
    {
        try
        {
            var modPath = Path.Combine(_modsPath, "steamapps", "workshop", "content", Arma3AppId, workshopId);
            var modKeysPath = Path.Combine(modPath, "Keys");
            
            if (!Directory.Exists(modKeysPath))
            {
                _logger.LogWarning("No keys folder found for mod: {WorkshopId}", workshopId);
                return;
            }
            
            Directory.CreateDirectory(serverKeysPath);
            
            foreach (var keyFile in Directory.GetFiles(modKeysPath, "*.bikey"))
            {
                var fileName = Path.GetFileName(keyFile);
                var destPath = Path.Combine(serverKeysPath, fileName);
                File.Copy(keyFile, destPath, true);
                _logger.LogInformation("Copied key: {KeyFile} to server", fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy keys for mod: {WorkshopId}", workshopId);
        }
    }
    
    /// <summary>
    /// Copies keys for all enabled mods to the server's key directory.
    /// </summary>
    /// <param name="serverKeysPath">Path to the server's 'keys' directory.</param>
    public void CopyAllEnabledModKeys(string serverKeysPath)
    {
        foreach (var mod in Mods.Where(m => m.IsEnabled && !m.IsLocal))
        {
            CopyModKeysToServer(mod.WorkshopId, serverKeysPath);
        }
    }
}

/// <summary>
/// Represents an Arma 3 mod.
/// </summary>
public class ArmaMod
{
    /// <summary>
    /// Gets or sets the Steam Workshop ID. For local mods, this is a generated ID.
    /// </summary>
    public string WorkshopId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the name of the mod.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the file path to the mod directory.
    /// </summary>
    public string Path { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether the mod is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the mod is a local mod (not from Steam Workshop).
    /// </summary>
    public bool IsLocal { get; set; }
    
    /// <summary>
    /// Gets a value indicating whether the local mod indicator should be visible in the UI.
    /// </summary>
    public string IsLocalVisible => IsLocal ? "Visible" : "Collapsed";
}