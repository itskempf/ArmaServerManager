using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ArmaServerManager.Core;

/// <summary>
/// Manages creation, deletion, and import/export of mod presets.
/// </summary>
public class PresetManager
{
    private readonly string _presetsPath;
    private readonly ModManager _modManager;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PresetManager"/> class.
    /// </summary>
    /// <param name="presetsPath">The directory where presets are stored.</param>
    /// <param name="modManager">The manager for Arma mods.</param>
    /// <param name="logger">The service for logging.</param>
    public PresetManager(string presetsPath, ModManager modManager, ILogger logger)
    {
        _presetsPath = string.IsNullOrEmpty(presetsPath) ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Presets") : presetsPath;
        _modManager = modManager;
        _logger = logger;
        Directory.CreateDirectory(_presetsPath);
    }

    /// <summary>
    /// Saves a mod preset to a JSON file.
    /// </summary>
    /// <param name="preset">The preset to save.</param>
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

    /// <summary>
    /// Loads a mod preset from a JSON file.
    /// </summary>
    /// <param name="presetName">The name of the preset to load.</param>
    /// <returns>The loaded <see cref="ModPreset"/> or null if not found.</returns>
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
    
    /// <summary>
    /// Exports a mod preset to an HTML file compatible with the Arma 3 launcher.
    /// </summary>
    /// <param name="preset">The preset to export.</param>
    /// <returns>The file path of the exported HTML file.</returns>
    public async Task<string> ExportPresetToHtmlAsync(ModPreset preset)
    {
        try
        {
            var html = new StringBuilder();
            // The HTML format is specifically structured for compatibility with the Arma 3 launcher.
            html.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            html.AppendLine("<html>");
            html.AppendLine("<head><meta name=\"arma:Type\" content=\"preset\"/><meta name=\"arma:PresetName\" content=\"" + preset.Name + "\" /></head>");
            html.AppendLine("<body>");
            
            foreach (var modId in preset.ModIds)
            {
                html.AppendLine("<tr data-type=\"ModContainer\"><td data-type=\"DisplayName\">" + modId + "</td><td data-type=\"Link\">http://steamcommunity.com/sharedfiles/filedetails/?id=" + modId + "</td></tr>");
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
    
    /// <summary>
    /// Imports a mod preset from an Arma 3 launcher HTML file.
    /// </summary>
    /// <param name="htmlFilePath">The path to the HTML preset file.</param>
    /// <returns>The imported <see cref="ModPreset"/> or null if import fails.</returns>
    public async Task<ModPreset?> ImportPresetFromHtmlAsync(string htmlFilePath)
    {
        try
        {
            if (!File.Exists(htmlFilePath)) return null;
            
            var html = await File.ReadAllTextAsync(htmlFilePath).ConfigureAwait(false);
            var preset = new ModPreset();
            
            // Extract preset name from meta tag
            var nameMatch = System.Text.RegularExpressions.Regex.Match(html, "<meta name=\"arma:PresetName\" content=\"(.*?)\" />");
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
    
    /// <summary>
    /// Installs all mods included in a given preset.
    /// </summary>
    /// <param name="preset">The preset whose mods should be installed.</param>
    /// <returns>True if all mods were scheduled for installation, otherwise false.</returns>
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
    
    /// <summary>
    /// Gets a list of all available preset names.
    /// </summary>
    /// <returns>An array of preset names.</returns>
    public async Task<string[]> GetPresetsAsync()
    {
        return await Task.Run(() =>
            Directory.GetFiles(_presetsPath, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToArray()!);
    }
    
    /// <summary>
    /// Deletes a preset file.
    /// </summary>
    /// <param name="presetName">The name of the preset to delete.</param>
    public async Task DeletePresetAsync(string presetName)
    {
        await Task.Run(() =>
        {
            var presetFile = Path.Combine(_presetsPath, $"{presetName}.json");
            if (File.Exists(presetFile))
                File.Delete(presetFile);
        });
    }
}

/// <summary>
/// Represents a mod preset, which is a collection of mod IDs.
/// </summary>
public class ModPreset
{
    /// <summary>
    /// Gets or sets the name of the preset.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description of the preset.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the list of Steam Workshop IDs for the mods in this preset.
    /// </summary>
    public List<string> ModIds { get; set; } = new();
    /// <summary>
    /// Gets or sets the creation date and time of the preset.
    /// </summary>
    public DateTime Created { get; set; } = DateTime.Now;
}