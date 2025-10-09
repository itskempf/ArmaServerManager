using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ArmaServerManager.UI.ViewModels;
using ArmaServerManager.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmaServerManager.UI.Pages;

public sealed partial class ModsPage : Page
{
    public ModsViewModel ViewModel { get; }
    private readonly ModManager _modManager;
    private readonly NotificationService _notificationService;

    public ModsPage()
    {
        ViewModel = App.Services.GetRequiredService<ModsViewModel>();
        _modManager = App.Services.GetRequiredService<ModManager>();
        _notificationService = App.Services.GetRequiredService<NotificationService>();
        
        this.InitializeComponent();
        this.DataContext = ViewModel;
    }
    
    private async void DownloadMod_Click(object sender, RoutedEventArgs e)
    {
        var workshopId = WorkshopIdBox.Text.Trim();
        if (string.IsNullOrEmpty(workshopId))
        {
            _notificationService.ShowWarning("Invalid Input", "Please enter a valid Workshop ID");
            return;
        }
        
        if (!long.TryParse(workshopId, out _))
        {
            _notificationService.ShowWarning("Invalid Input", "Workshop ID must be a number");
            return;
        }
        
        try
        {
            WorkshopIdBox.IsEnabled = false;
            _notificationService.ShowInfo("Downloading", $"Downloading mod {workshopId}... This may take several minutes.");
            var success = await _modManager.InstallModAsync(workshopId);
            
            if (success)
            {
                _notificationService.ShowSuccess("Download Complete", $"Mod {workshopId} downloaded successfully");
                WorkshopIdBox.Text = string.Empty;
            }
            else
            {
                _notificationService.ShowError("Download Failed", $"Failed to download mod {workshopId}");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Download Error", $"Error downloading mod: {ex.Message}");
        }
        finally
        {
            WorkshopIdBox.IsEnabled = true;
        }
    }
    
    private async void UpdateAll_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _notificationService.ShowInfo("Updating", "Updating all mods...");
            
            var updateTasks = new List<Task<bool>>();
            foreach (var mod in _modManager.Mods.Where(m => !m.IsLocal))
            {
                updateTasks.Add(_modManager.InstallModAsync(mod.WorkshopId));
            }
            
            await Task.WhenAll(updateTasks);
            _notificationService.ShowSuccess("Update Complete", "All mods have been updated");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Update Error", $"Error updating mods: {ex.Message}");
        }
    }
    
    private void AddLocalMod_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.ShowInfo("Add Local Mod", "Enter mod path in settings or use file explorer to copy mods to the Mods/Local folder");
    }
    
    private async void DeleteMod_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ArmaMod mod)
        {
            try
            {
                await _modManager.RemoveModAsync(mod.WorkshopId);
                _notificationService.ShowSuccess("Mod Removed", $"Removed mod: {mod.Name}");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Remove Error", $"Error removing mod: {ex.Message}");
            }
        }
    }
}