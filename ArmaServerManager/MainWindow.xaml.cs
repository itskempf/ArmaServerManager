using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ArmaServerManager.UI.Pages;
using ArmaServerManager.Core;
using System;

namespace ArmaServerManager;

public sealed partial class MainWindow : Window
{
    private readonly NotificationService _notificationService;

    public MainWindow()
    {
        this.InitializeComponent();
        _notificationService = App.Services.GetService(typeof(NotificationService)) as NotificationService ?? throw new InvalidOperationException();
        _notificationService.NotificationRequested += OnNotificationRequested;
        
        MainNavigation.SelectionChanged += OnNavigationSelectionChanged;
        MainNavigation.SelectedItem = MainNavigation.MenuItems[0];
        
        this.Closed += OnWindowClosed;
    }
    
    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        _notificationService.NotificationRequested -= OnNotificationRequested;
        MainNavigation.SelectionChanged -= OnNavigationSelectionChanged;
    }

    private void OnNotificationRequested(NotificationMessage notification)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            NotificationBar.Title = notification.Title;
            NotificationBar.Message = notification.Message;
            NotificationBar.Severity = notification.Type switch
            {
                NotificationType.Success => InfoBarSeverity.Success,
                NotificationType.Warning => InfoBarSeverity.Warning,
                NotificationType.Error => InfoBarSeverity.Error,
                _ => InfoBarSeverity.Informational
            };
            NotificationBar.IsOpen = true;
            _notificationService.SetCurrentInfoBar(NotificationBar);
        });
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            NavigateToPage(tag);
        }
    }

    private void NavigateToPage(string? pageTag)
    {
        Type? pageType = pageTag switch
        {
            "Dashboard" => typeof(DashboardPage),
            "Servers" => typeof(ServersPage),
            "Mods" => typeof(ModsPage),
            "Presets" => typeof(PresetsPage),
            "Settings" => typeof(SettingsPage),
            _ => null
        };

        if (pageType is not null)
        {
            MainFrame.Navigate(pageType);
        }
    }
}
