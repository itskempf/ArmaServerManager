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

    public ObservableCollection<ArmaMod> Mods => _modManager.Mods;

    [ObservableProperty]
    private string workshopId = string.Empty;

    [ObservableProperty]
    private bool isInstalling;

    [ObservableProperty]
    private string installProgress = string.Empty;

    public ModsViewModel(ModManager modManager)
    {
        _modManager = modManager;
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
    private void ImportPreset()
    {
        // Open file picker for HTML preset files
        // Parse and install mods from preset
    }

    [RelayCommand]
    private void ExportPreset()
    {
        var enabledMods = Mods.Where(m => m.IsEnabled).Select(m => m.WorkshopId).ToList();
        // Export logic here
    }
}