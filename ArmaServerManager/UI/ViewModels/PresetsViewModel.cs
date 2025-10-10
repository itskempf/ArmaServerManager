using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArmaServerManager.Core;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ArmaServerManager.UI.ViewModels;

public partial class PresetsViewModel : ViewModelBase
{
    private readonly PresetManager _presetManager;
    private readonly ModManager _modManager;
    private readonly NotificationService _notificationService;

    [ObservableProperty]
    private string presetName = string.Empty;

    [ObservableProperty]
    private string presetDescription = string.Empty;

    [ObservableProperty]
    private string selectedPresetName = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    public ObservableCollection<string> AvailablePresets { get; } = new();
    public ObservableCollection<string> PresetModIds { get; } = new();

    public PresetsViewModel(PresetManager presetManager, ModManager modManager, NotificationService notificationService)
    {
        _presetManager = presetManager;
        _modManager = modManager;
        _notificationService = notificationService;
        LoadPresets();
    }

    private void LoadPresets()
    {
        try
        {
            AvailablePresets.Clear();
            var presets = _presetManager.GetPresets();
            foreach (var preset in presets)
            {
                AvailablePresets.Add(preset);
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Load Error", $"Failed to load presets: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CreatePresetAsync()
    {
        if (string.IsNullOrWhiteSpace(PresetName))
        {
            _notificationService.ShowWarning("Invalid Name", "Please enter a preset name");
            return;
        }

        try
        {
            IsLoading = true;
            var enabledMods = _modManager.Mods.Where(m => m.IsEnabled).Select(m => m.WorkshopId).ToList();
            
            if (!enabledMods.Any())
            {
                _notificationService.ShowWarning("No Mods", "No enabled mods to include in preset");
                return;
            }

            var preset = new ModPreset
            {
                Name = PresetName,
                Description = PresetDescription,
                ModIds = enabledMods
            };

            await _presetManager.SavePresetAsync(preset);
            LoadPresets();
            
            PresetName = string.Empty;
            PresetDescription = string.Empty;
            
            _notificationService.ShowSuccess("Preset Created", $"Created preset '{preset.Name}' with {enabledMods.Count} mods");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Create Error", $"Failed to create preset: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadPresetAsync(string presetName)
    {
        if (string.IsNullOrEmpty(presetName)) return;

        try
        {
            IsLoading = true;
            var preset = await _presetManager.LoadPresetAsync(presetName);
            if (preset != null)
            {
                PresetModIds.Clear();
                foreach (var modId in preset.ModIds)
                {
                    PresetModIds.Add(modId);
                }
                SelectedPresetName = presetName;
                _notificationService.ShowInfo("Preset Loaded", $"Loaded preset '{presetName}' with {preset.ModIds.Count} mods");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Load Error", $"Failed to load preset: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task InstallPresetModsAsync()
    {
        if (string.IsNullOrEmpty(SelectedPresetName)) return;

        try
        {
            IsLoading = true;
            var preset = await _presetManager.LoadPresetAsync(SelectedPresetName);
            if (preset != null)
            {
                await _presetManager.InstallPresetModsAsync(preset);
                _notificationService.ShowSuccess("Installation Complete", $"Installed mods for preset '{preset.Name}'");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Install Error", $"Failed to install preset mods: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void DeletePreset(string presetName)
    {
        if (string.IsNullOrEmpty(presetName)) return;

        try
        {
            _presetManager.DeletePreset(presetName);
            LoadPresets();
            if (SelectedPresetName == presetName)
            {
                SelectedPresetName = string.Empty;
                PresetModIds.Clear();
            }
            _notificationService.ShowSuccess("Preset Deleted", $"Deleted preset '{presetName}'");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Delete Error", $"Failed to delete preset: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ImportPresetAsync()
    {
        try
        {
            using var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "HTML Preset Files|*.html|All Files|*.*",
                Title = "Import Arma 3 Preset"
            };
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                IsLoading = true;
                var preset = await _presetManager.ImportPresetFromHtmlAsync(dialog.FileName);
                if (preset != null)
                {
                    await _presetManager.SavePresetAsync(preset);
                    LoadPresets();
                    _notificationService.ShowSuccess("Preset Imported", $"Imported '{preset.Name}' with {preset.ModIds.Count} mods");
                }
                else
                {
                    _notificationService.ShowError("Import Failed", "Failed to parse preset file");
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Import Error", $"Failed to import preset: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportPresetAsync()
    {
        if (string.IsNullOrEmpty(SelectedPresetName)) return;

        try
        {
            IsLoading = true;
            var preset = await _presetManager.LoadPresetAsync(SelectedPresetName);
            if (preset != null)
            {
                var htmlPath = await _presetManager.ExportPresetToHtmlAsync(preset);
                _notificationService.ShowSuccess("Preset Exported", $"Exported to {htmlPath}");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Export Error", $"Failed to export preset: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}