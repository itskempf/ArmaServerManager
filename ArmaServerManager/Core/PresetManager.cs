using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArmaServerManager.Core;

public class PresetManager
{
    private readonly string _presetsPath;
    private readonly ModManager _modManager;
    private readonly LoggingService _logger;

    public PresetManager(string presetsPath, ModManager modManager, LoggingService logger)
    {
        _presetsPath = string.IsNullOrEmpty(presetsPath) ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Presets") : presetsPath;
        _modManager = modManager;
        _logger = logger;
        Directory.CreateDirectory(_presetsPath);
    }

    public async Task SavePresetAsync(ModPreset preset)
    {
        try
        {
            var presetFile = Path.Combine(_presetsPath, $"{preset.Name}.json");
            var json = JsonSerializer.Serialize(preset, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(presetFile, json).ConfigureAwait(false);
            _logger.LogInformation("Preset saved: {PresetName}", preset.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save preset: {PresetName}", preset.Name);
            throw;
        }
    }

    public async Task<ModPreset?> LoadPresetAsync(string presetName)
    {
        try
        {
            var presetFile = Path.Combine(_presetsPath, $"{presetName}.json");
            if (!File.Exists(presetFile)) return null;
            
            var json = await File.ReadAllTextAsync(presetFile).ConfigureAwait(false);
            return JsonSerializer.Deserialize<ModPreset>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load preset: {PresetName}", presetName);
            return null;
        }
    }
    
    public async Task<string> ExportPresetToHtmlAsync(ModPreset preset)
    {
        try
        {
            var html = new StringBuilder();
            html.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            html.AppendLine("<html>");
            html.AppendLine($"<head><meta name=\"arma:Type\" content=\"preset\"/><meta name=\"arma:PresetName\" content=\"{preset.Name}\"/></head>");
            html.AppendLine("<body>");
            
            foreach (var modId in preset.ModIds)
            {
                html.AppendLine($"<tr data-type=\"ModContainer\"><td data-type=\"DisplayName\">{modId}</td><td data-type=\"Link\">http://steamcommunity.com/sharedfiles/filedetails/?id={modId}</td></tr>");
            }
            
            html.AppendLine("</body></html>");
            
            var htmlFile = Path.Combine(_presetsPath, $"{preset.Name}.html");
            await File.WriteAllTextAsync(htmlFile, html.ToString()).ConfigureAwait(false);
            return htmlFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export preset to HTML: {PresetName}", preset.Name);
            throw;
        }
    }
    
    public async Task<ModPreset?> ImportPresetFromHtmlAsync(string htmlFilePath)
    {
        try
        {
            if (!File.Exists(htmlFilePath)) return null;
            
            var html = await File.ReadAllTextAsync(htmlFilePath).ConfigureAwait(false);
            var preset = new ModPreset();
            
            // Extract preset name from meta tag
            var nameMatch = System.Text.RegularExpressions.Regex.Match(html, @"arma:PresetName""\s+content=""([^""]+)""");
            if (nameMatch.Success)
            {
                preset.Name = nameMatch.Groups[1].Value;
            }
            else
            {
                preset.Name = Path.GetFileNameWithoutExtension(htmlFilePath);
            }
            
            // Extract mod IDs from Steam Workshop links
            var modIds = new List<string>();
            var matches = System.Text.RegularExpressions.Regex.Matches(html, @"steamcommunity\.com/sharedfiles/filedetails/\?id=(\d+)");
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var modId = match.Groups[1].Value;
                if (!modIds.Contains(modId))
                {
                    modIds.Add(modId);
                }
            }
            
            preset.ModIds = modIds;
            preset.Description = $"Imported from {Path.GetFileName(htmlFilePath)} - {modIds.Count} mods";
            
            _logger.LogInformation("Imported preset '{0}' with {1} mods from HTML", preset.Name, modIds.Count);
            return preset;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import preset from HTML: {HtmlFile}", htmlFilePath);
            return null;
        }
    }
    
    public async Task<bool> InstallPresetModsAsync(ModPreset preset)
    {
        try
        {
            _logger.LogInformation("Installing mods for preset: {PresetName}", preset.Name);
            
            foreach (var modId in preset.ModIds)
            {
                if (!_modManager.Mods.Any(m => m.WorkshopId == modId))
                {
                    await _modManager.InstallModAsync(modId).ConfigureAwait(false);
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install preset mods: {PresetName}", preset.Name);
            return false;
        }
    }
    
    public string[] GetPresets()
    {
        return Directory.GetFiles(_presetsPath, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToArray()!;
    }
    
    public void DeletePreset(string presetName)
    {
        var presetFile = Path.Combine(_presetsPath, $"{presetName}.json");
        if (File.Exists(presetFile))
            File.Delete(presetFile);
    }
}

public class ModPreset
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> ModIds { get; set; } = new();
    public DateTime Created { get; set; } = DateTime.Now;
}