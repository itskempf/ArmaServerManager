using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArmaServerManager.Core;

public class ModManager
{
    private readonly string _modsPath;
    private readonly SteamCMDHandler _steamCmdHandler;
    private readonly LoggingService _logger;
    private readonly string _modsConfigPath;
    
    public ObservableCollection<ArmaMod> Mods { get; } = new();

    public ModManager(string modsPath, SteamCMDHandler steamCmdHandler, LoggingService logger)
    {
        _modsPath = modsPath;
        _steamCmdHandler = steamCmdHandler;
        _logger = logger;
        _modsConfigPath = Path.Combine(_modsPath, "mods.json");
        
        Directory.CreateDirectory(_modsPath);
        LoadInstalledMods();
    }

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

            var modPath = Path.Combine(_modsPath, "steamapps", "workshop", "content", "107410", workshopId);
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
                WorkshopId = $"local_{Guid.NewGuid():N}".Substring(0, 14),
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
        var workshopPath = Path.Combine(_modsPath, "steamapps", "workshop", "content", "107410");
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
    
    public bool VerifyModInstallation(string workshopId)
    {
        var modPath = Path.Combine(_modsPath, "steamapps", "workshop", "content", "107410", workshopId);
        return Directory.Exists(modPath) && Directory.GetFiles(modPath, "*.pbo", SearchOption.AllDirectories).Length > 0;
    }
    
    public long GetModSize(string workshopId)
    {
        var modPath = Path.Combine(_modsPath, "steamapps", "workshop", "content", "107410", workshopId);
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
    
    public string GetModKeys(string workshopId)
    {
        var modPath = Path.Combine(_modsPath, "steamapps", "workshop", "content", "107410", workshopId);
        var keysPath = Path.Combine(modPath, "Keys");
        
        if (Directory.Exists(keysPath))
        {
            var keyFiles = Directory.GetFiles(keysPath, "*.bikey");
            return string.Join(", ", keyFiles.Select(Path.GetFileName));
        }
        
        return "No keys found";
    }
    
    public async Task<bool> UpdateModAsync(string workshopId)
    {
        _logger.LogInformation("Updating mod: {WorkshopId}", workshopId);
        return await InstallModAsync(workshopId).ConfigureAwait(false);
    }
}

public class ArmaMod
{
    public string WorkshopId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsLocal { get; set; }
    
    public string IsLocalVisible => IsLocal ? "Visible" : "Collapsed";
}