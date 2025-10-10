using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArmaServerManager.Core;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ArmaServerManager.UI.ViewModels;

public partial class ModsViewModel : ViewModelBase
{
    private readonly ModManager _modManager;
    private readonly PresetManager _presetManager;
    private readonly NotificationService _notificationService;

    public ObservableCollection<ArmaMod> Mods => _modManager.Mods;

    [ObservableProperty]
    private string workshopId = string.Empty;

    [ObservableProperty]
    private bool isInstalling;

    [ObservableProperty]
    private string installProgress = string.Empty;

    public ModsViewModel(ModManager modManager, PresetManager presetManager, NotificationService notificationService)
    {
        _modManager = modManager;
        _presetManager = presetManager;
        _notificationService = notificationService;
        _modManager.LoadInstalledMods();
    }

    [RelayCommand]
    private async Task InstallModAsync()
    {
        if (string.IsNullOrWhiteSpace(WorkshopId) || IsInstalling)
            return;

        IsInstalling = true;
        InstallProgress = "Starting installation...";

        try
        {
            var success = await _modManager.InstallModAsync(WorkshopId);
            if (success)
            {
                WorkshopId = string.Empty;
                InstallProgress = "Installation completed successfully";
            }
            else
            {
                InstallProgress = "Installation failed";
            }
        }
        catch (System.Exception ex)
        {
            InstallProgress = $"Installation error: {ex.Message}";
        }
        finally
        {
            IsInstalling = false;
        }
    }

    [RelayCommand]
    private void ToggleModEnabled(ArmaMod mod)
    {
        if (mod != null)
        {
            mod.IsEnabled = !mod.IsEnabled;
        }
    }

    [RelayCommand]
    private async Task DeleteModAsync(ArmaMod mod)
    {
        if (mod == null) return;

        await _modManager.RemoveModAsync(mod.WorkshopId);
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
                var preset = await _presetManager.ImportPresetFromHtmlAsync(dialog.FileName);
                if (preset != null)
                {
                    await _presetManager.SavePresetAsync(preset);
                    await _presetManager.InstallPresetModsAsync(preset);
                    _notificationService.ShowSuccess("Preset Imported", $"Imported {preset.ModIds.Count} mods from {preset.Name}");
                }
                else
                {
                    _notificationService.ShowError("Import Failed", "Failed to parse preset file");
                }
            }
        }
        catch (System.Exception ex)
        {
            _notificationService.ShowError("Import Error", $"Failed to import preset: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExportPresetAsync()
    {
        try
        {
            var enabledMods = Mods.Where(m => m.IsEnabled).Select(m => m.WorkshopId).ToList();
            if (!enabledMods.Any())
            {
                _notificationService.ShowWarning("No Mods", "No enabled mods to export");
                return;
            }
            
            var preset = new ModPreset
            {
                Name = $"Export_{System.DateTime.Now:yyyyMMdd_HHmmss}",
                Description = $"Exported preset with {enabledMods.Count} mods",
                ModIds = enabledMods
            };
            
            await _presetManager.SavePresetAsync(preset);
            var htmlPath = await _presetManager.ExportPresetToHtmlAsync(preset);
            
            _notificationService.ShowSuccess("Preset Exported", $"Exported to {htmlPath}");
        }
        catch (System.Exception ex)
        {
            _notificationService.ShowError("Export Error", $"Failed to export preset: {ex.Message}");
        }
    }
}