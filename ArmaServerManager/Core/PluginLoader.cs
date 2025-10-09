using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ArmaServerManager.Core;

public class PluginInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public bool IsLoaded { get; set; }
    public IPlugin? Instance { get; set; }
}

public class PluginLoader
{
    private readonly string _pluginsDirectory;
    private readonly List<PluginInfo> _plugins = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly LoggingService _logger;

    public PluginLoader(IServiceProvider serviceProvider, LoggingService logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _pluginsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        Directory.CreateDirectory(_pluginsDirectory);
    }

    public async Task LoadPluginsAsync()
    {
        _plugins.Clear();
        
        if (!Directory.Exists(_pluginsDirectory))
            return;
            
        var dllFiles = Directory.GetFiles(_pluginsDirectory, "*.dll");

        foreach (var dllFile in dllFiles)
        {
            await Task.Run(async () =>
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dllFile);
                    var pluginTypes = assembly.GetTypes()
                        .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var pluginType in pluginTypes)
                    {
                        try
                        {
                            var plugin = (IPlugin?)Activator.CreateInstance(pluginType);
                            if (plugin != null)
                            {
                                await plugin.InitializeAsync(_serviceProvider).ConfigureAwait(false);
                                
                                _plugins.Add(new PluginInfo
                                {
                                    Name = plugin.Name,
                                    Version = plugin.Version,
                                    Description = plugin.Description,
                                    Author = plugin.Author,
                                    FilePath = dllFile,
                                    IsLoaded = true,
                                    Instance = plugin
                                });
                                
                                _logger.LogInformation("Plugin loaded: {PluginName} v{Version}", plugin.Name, plugin.Version);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to instantiate plugin type: {TypeName}", pluginType.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load plugin from: {DllFile}", dllFile);
                }
            }).ConfigureAwait(false);
        }
    }

    public async Task ExecutePluginAsync(string pluginName)
    {
        try
        {
            var plugin = _plugins.FirstOrDefault(p => p.Name == pluginName && p.IsLoaded);
            if (plugin?.Instance != null)
            {
                bool canExecute = true;
                try
                {
                    canExecute = await plugin.Instance.CanExecuteAsync().ConfigureAwait(false);
                }
                catch (NotImplementedException)
                {
                    canExecute = true;
                }
                
                if (canExecute)
                {
                    await plugin.Instance.ExecuteAsync().ConfigureAwait(false);
                    _logger.LogInformation("Plugin executed successfully: {PluginName}", pluginName);
                }
                else
                {
                    _logger.LogWarning("Plugin cannot execute at this time: {PluginName}", pluginName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute plugin: {PluginName}", pluginName);
            throw;
        }
    }

    public IReadOnlyList<PluginInfo> GetPlugins() => _plugins.AsReadOnly();

    public void UnloadPlugin(string pluginName)
    {
        var plugin = _plugins.FirstOrDefault(p => p.Name == pluginName);
        if (plugin != null)
        {
            try
            {
                plugin.Instance?.Dispose();
                plugin.IsLoaded = false;
                plugin.Instance = null;
                _logger.LogInformation("Plugin unloaded: {PluginName}", pluginName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unload plugin: {PluginName}", pluginName);
            }
        }
    }
    
    public async Task NotifyServerStartedAsync(string serverId)
    {
        foreach (var plugin in _plugins.Where(p => p.IsLoaded && p.Instance != null))
        {
            try
            {
                await plugin.Instance!.OnServerStartedAsync(serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plugin {PluginName} failed to handle server started event", plugin.Name);
            }
        }
    }
    
    public async Task NotifyServerStoppedAsync(string serverId)
    {
        foreach (var plugin in _plugins.Where(p => p.IsLoaded && p.Instance != null))
        {
            try
            {
                await plugin.Instance!.OnServerStoppedAsync(serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plugin {PluginName} failed to handle server stopped event", plugin.Name);
            }
        }
    }
}