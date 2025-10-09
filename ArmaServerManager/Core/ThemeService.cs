using Microsoft.UI.Xaml;
using System;

namespace ArmaServerManager.Core;

public class ThemeService
{
    private readonly SettingsService _settingsService;
    private Window? _mainWindow;

    public ThemeService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Initialize(Window mainWindow)
    {
        _mainWindow = mainWindow;
        ApplyTheme(_settingsService.Settings.Application.Theme);
    }

    public void ApplyTheme(string theme)
    {
        if (_mainWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme.ToLower() switch
            {
                "light" => ElementTheme.Light,
                "dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
        _settingsService.Settings.Application.Theme = theme;
    }

    public string GetCurrentTheme() => _settingsService.Settings.Application.Theme;

    public ElementTheme GetElementTheme()
    {
        return _settingsService.Settings.Application.Theme.ToLower() switch
        {
            "light" => ElementTheme.Light,
            "dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }
}