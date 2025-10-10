using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArmaServerManager.Core;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ArmaServerManager.UI.ViewModels;

public partial class ServersViewModel : ViewModelBase
{
    private readonly ServerManager _serverManager;
    private readonly ConfigManager _configManager;

    public ObservableCollection<ArmaServer> Servers => _serverManager.Servers;

    [ObservableProperty]
    private ArmaServer? selectedServer;

    [ObservableProperty]
    private bool isLoading;
    
    [ObservableProperty]
    private string selectedProfileName = "Default";
    
    public string[] AvailableProfiles { get; } = { "Default", "PvP Setup", "Co-op Mission" };

    public ServersViewModel(ServerManager serverManager, ConfigManager configManager)
    {
        _serverManager = serverManager;
        _configManager = configManager;
    }

    [RelayCommand]
    private async Task AddNewServerAsync()
    {
        var newServer = new ArmaServer
        {
            Name = $"Server {Servers.Count + 1}",
            InstallPath = @"C:\ArmaServers\Server" + (Servers.Count + 1),
            ConfigPath = "server.cfg",
            Port = 2302 + Servers.Count,
            MaxPlayers = 64
        };
        
        await _serverManager.AddServerAsync(newServer);
        SelectedServer = newServer;
    }

    [RelayCommand]
    private async Task StartServerAsync(ArmaServer server)
    {
        if (server == null) return;
        
        IsLoading = true;
        await _serverManager.StartServerAsync(server);
        IsLoading = false;
    }

    [RelayCommand]
    private void StopServer(ArmaServer server)
    {
        if (server == null) return;
        
        _serverManager.StopServer(server.Name);
    }

    [RelayCommand]
    private void SaveServer(ArmaServer server)
    {
        if (server == null) return;
        
        _configManager.SaveConfig(server);
    }

    [RelayCommand]
    private async Task DeleteServerAsync(ArmaServer server)
    {
        if (server == null) return;
        
        await _serverManager.RemoveServerAsync(server.Name);
        
        if (SelectedServer == server)
            SelectedServer = null;
    }

    [RelayCommand]
    private void EditServer(ArmaServer server)
    {
        SelectedServer = server;
    }
    
    [RelayCommand]
    private void SaveProfile()
    {
        if (SelectedServer != null)
        {
            _configManager.SaveConfig(SelectedServer);
        }
    }
    
    [RelayCommand]
    private void LoadProfile(string profileName)
    {
        SelectedProfileName = profileName;
        // Profile loading would be implemented with actual profile system
    }
    
    [RelayCommand]
    private void ToggleHeadlessClient(ArmaServer server)
    {
        if (server != null)
        {
            server.EnableHeadlessClient = !server.EnableHeadlessClient;
        }
    }
}