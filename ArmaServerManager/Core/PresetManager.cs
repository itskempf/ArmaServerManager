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
        _presetsPath = presetsPath;
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
            
            // Extract preset name
            var nameStart = html.IndexOf("arma:PresetName\" content=\"");
            if (nameStart >= 0)
            {
                nameStart += 25;
                var nameEnd = html.IndexOf("\"", nameStart);
                if (nameEnd > nameStart)
                    preset.Name = html.Substring(nameStart, nameEnd - nameStart);
            }
            
            // Extract mod IDs
            var modIds = new List<string>();
            var searchPos = 0;
            while ((searchPos = html.IndexOf("filedetails/?id=", searchPos)) >= 0)
            {
                searchPos += 16;
                var endPos = html.IndexOf("\"", searchPos);
                if (endPos > searchPos)
                {
                    var modId = html.Substring(searchPos, endPos - searchPos);
                    modIds.Add(modId);
                }
            }
            
            preset.ModIds = modIds;
            return preset;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import preset from HTML: {HtmlFile}", htmlFilePath);
            return null;
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