using System;
using System.Threading.Tasks;

namespace ArmaServerManager.Core;

public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    string Author { get; }

    Task InitializeAsync(IServiceProvider serviceProvider);
    Task<bool> CanExecuteAsync();
    Task ExecuteAsync();
    Task OnServerStartedAsync(string serverId);
    Task OnServerStoppedAsync(string serverId);
    void Dispose();
}

public abstract class PluginBase : IPlugin
{
    public abstract string Name { get; }
    public abstract string Version { get; }
    public abstract string Description { get; }
    public abstract string Author { get; }

    protected IServiceProvider? ServiceProvider { get; private set; }

    public virtual Task InitializeAsync(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        return Task.CompletedTask;
    }

    public virtual Task<bool> CanExecuteAsync() => Task.FromResult(true);
    public abstract Task ExecuteAsync();
    public virtual Task OnServerStartedAsync(string serverId) => Task.CompletedTask;
    public virtual Task OnServerStoppedAsync(string serverId) => Task.CompletedTask;
    public virtual void Dispose() { }
}