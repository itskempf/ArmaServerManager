using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArmaServerManager.Core;

public class ServerManager
{
    private readonly LoggingService _logger;
    private readonly SettingsService _settingsService;
    private readonly ConcurrentDictionary<string, int> _serverProcessIds = new();
    
    public ObservableCollection<ArmaServer> Servers { get; } = new();
    
    public ServerManager(LoggingService logger, SettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
        LoadServers();
    }

    public async Task<bool> StartServerAsync(ArmaServer server)
    {
        try
        {
            var serverExe = Path.Combine(server.InstallPath, "arma3server_x64.exe");
            if (!File.Exists(serverExe))
            {
                _logger.LogError("Server executable not found: {ServerExe}", serverExe);
                return false;
            }
            
            var startInfo = new ProcessStartInfo
            {
                FileName = serverExe,
                Arguments = BuildServerArguments(server),
                WorkingDirectory = server.InstallPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            var process = Process.Start(startInfo);
            if (process != null)
            {
                _serverProcessIds[server.Name] = process.Id;
                server.IsRunning = true;
                server.ProcessId = process.Id;
                _logger.LogInformation("Server started: {ServerName} (PID: {ProcessId})", server.Name, process.Id);
                
                _ = Task.Run(async () =>
                {
                    await process.WaitForExitAsync().ConfigureAwait(false);
                    server.IsRunning = false;
                    _serverProcessIds.TryRemove(server.Name, out _);
                    _logger.LogInformation("Server stopped: {ServerName}", server.Name);
                });
                
                if (server.EnableHeadlessClient)
                {
                    await StartHeadlessClientAsync(server).ConfigureAwait(false);
                }
                
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start server: {ServerName}", server.Name);
        }
        
        return false;
    }
    
    public Task<bool> StartHeadlessClientAsync(ArmaServer server)
    {
        try
        {
            var serverExe = Path.Combine(server.InstallPath, "arma3server_x64.exe");
            var startInfo = new ProcessStartInfo
            {
                FileName = serverExe,
                Arguments = $"-client -connect=127.0.0.1 -port={server.Port} -password={server.Password}",
                WorkingDirectory = server.InstallPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            var process = Process.Start(startInfo);
            if (process != null)
            {
                server.IsHeadlessClientRunning = true;
                _logger.LogInformation("Headless client started for server: {ServerName}", server.Name);
                return Task.FromResult(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start headless client for server: {ServerName}", server.Name);
        }
        
        return Task.FromResult(false);
    }
    
    public bool StopHeadlessClient(ArmaServer server)
    {
        try
        {
            var processes = Process.GetProcessesByName("arma3server_x64")
                .Where(p => p.StartInfo.Arguments?.Contains("-client") == true);
            
            foreach (var process in processes)
            {
                process.Kill();
                process.WaitForExit(5000);
            }
            
            server.IsHeadlessClientRunning = false;
            _logger.LogInformation("Headless client stopped for server: {ServerName}", server.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop headless client for server: {ServerName}", server.Name);
            return false;
        }
    }

    public bool StopServer(string serverName)
    {
        try
        {
            var server = Servers.FirstOrDefault(s => s.Name == serverName);
            if (server == null) return false;
            
            if (_serverProcessIds.TryGetValue(serverName, out var processId))
            {
                try
                {
                    var process = Process.GetProcessById(processId);
                    process.Kill(true);
                    process.WaitForExit(5000);
                }
                catch (ArgumentException)
                {
                    _logger.LogWarning("Process {ProcessId} already exited", processId);
                }
                
                _serverProcessIds.TryRemove(serverName, out _);
            }
            
            server.IsRunning = false;
            if (server.IsHeadlessClientRunning)
            {
                StopHeadlessClient(server);
            }
            
            _logger.LogInformation("Server stopped: {ServerName}", serverName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop server: {ServerName}", serverName);
            return false;
        }
    }

    public bool IsServerRunning(string serverName)
    {
        if (_serverProcessIds.TryGetValue(serverName, out var processId))
        {
            try
            {
                var process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch (ArgumentException)
            {
                _serverProcessIds.TryRemove(serverName, out _);
                var server = Servers.FirstOrDefault(s => s.Name == serverName);
                if (server != null) server.IsRunning = false;
                return false;
            }
        }
        return false;
    }
    
    public async Task RestartServerAsync(string serverId)
    {
        var server = Servers.FirstOrDefault(s => s.Name == serverId);
        if (server == null) return;
        
        _logger.LogInformation("Restarting server: {ServerName}", serverId);
        
        StopServer(serverId);
        await Task.Delay(3000).ConfigureAwait(false);
        await StartServerAsync(server).ConfigureAwait(false);
    }
    
    private string BuildServerArguments(ArmaServer server)
    {
        var args = $"-port={server.Port} -config={server.ConfigPath}";
        
        if (!string.IsNullOrEmpty(server.CommandLineParams))
            args += $" {server.CommandLineParams}";
            
        return args;
    }
    
    private void LoadServers()
    {
        // Load servers from configuration or create default
        if (Servers.Count == 0)
        {
            var defaultServer = new ArmaServer
            {
                Name = "Default Server",
                InstallPath = Path.Combine(_settingsService.Settings.Directories.Servers, "default"),
                ConfigPath = "server.cfg",
                Port = 2302,
                MaxPlayers = 64
            };
            
            Servers.Add(defaultServer);
        }
    }
    
    public void Cleanup()
    {
        foreach (var server in Servers.Where(s => s.IsRunning).ToList())
        {
            StopServer(server.Name);
        }
    }
    
    public void AddServer(ArmaServer server)
    {
        Servers.Add(server);
        _logger.LogInformation("Server added: {ServerName}", server.Name);
    }
    
    public void RemoveServer(string serverName)
    {
        var server = Servers.FirstOrDefault(s => s.Name == serverName);
        if (server != null)
        {
            if (server.IsRunning)
                StopServer(serverName);
                
            Servers.Remove(server);
            _logger.LogInformation("Server removed: {ServerName}", serverName);
        }
    }
    
    public ServerStatus GetServerStatus(string serverName)
    {
        var server = Servers.FirstOrDefault(s => s.Name == serverName);
        if (server == null)
            return new ServerStatus { IsRunning = false, Status = "Server not found" };
        
        if (!server.IsRunning)
            return new ServerStatus { IsRunning = false, Status = "Stopped" };
        
        try
        {
            if (_serverProcessIds.TryGetValue(serverName, out var processId))
            {
                var process = Process.GetProcessById(processId);
                return new ServerStatus
                {
                    IsRunning = true,
                    Status = "Running",
                    ProcessId = processId,
                    CpuUsage = 0,
                    MemoryUsage = process.WorkingSet64,
                    Uptime = DateTime.Now - process.StartTime
                };
            }
        }
        catch (ArgumentException)
        {
            server.IsRunning = false;
            _serverProcessIds.TryRemove(serverName, out _);
        }
        
        return new ServerStatus { IsRunning = false, Status = "Unknown" };
    }
    
    public async Task<bool> ValidateServerInstallation(string installPath)
    {
        var serverExe = Path.Combine(installPath, "arma3server_x64.exe");
        return await Task.Run(() => File.Exists(serverExe));
    }
}

public class ServerStatus
{
    public bool IsRunning { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public float CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
    public TimeSpan Uptime { get; set; }
}

public class ArmaServer : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private string _name = string.Empty;
    private string _installPath = string.Empty;
    private string _configPath = string.Empty;
    private int _port = 2302;
    private int _maxPlayers = 64;
    private string _password = string.Empty;
    private string _adminPassword = string.Empty;
    private bool _isRunning;
    private bool _enableHeadlessClient;
    private bool _isHeadlessClientRunning;
    private string _commandLineParams = string.Empty;
    private int _currentPlayers;
    private int _processId;
    
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string InstallPath { get => _installPath; set => SetProperty(ref _installPath, value); }
    public string ConfigPath { get => _configPath; set => SetProperty(ref _configPath, value); }
    public int Port { get => _port; set => SetProperty(ref _port, value); }
    public int MaxPlayers { get => _maxPlayers; set => SetProperty(ref _maxPlayers, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string AdminPassword { get => _adminPassword; set => SetProperty(ref _adminPassword, value); }
    public bool IsRunning { get => _isRunning; set { SetProperty(ref _isRunning, value); OnPropertyChanged(nameof(StatusText)); OnPropertyChanged(nameof(CanStart)); OnPropertyChanged(nameof(CanStop)); } }
    public bool EnableHeadlessClient { get => _enableHeadlessClient; set => SetProperty(ref _enableHeadlessClient, value); }
    public bool IsHeadlessClientRunning { get => _isHeadlessClientRunning; set => SetProperty(ref _isHeadlessClientRunning, value); }
    public string CommandLineParams { get => _commandLineParams; set => SetProperty(ref _commandLineParams, value); }
    public int CurrentPlayers { get => _currentPlayers; set { SetProperty(ref _currentPlayers, value); OnPropertyChanged(nameof(PlayerInfo)); } }
    public int ProcessId { get => _processId; set => SetProperty(ref _processId, value); }
    
    public string StatusText => IsRunning ? "Running" : "Stopped";
    public string PlayerInfo => $"{CurrentPlayers}/{MaxPlayers} players";
    public bool CanStart => !IsRunning;
    public bool CanStop => IsRunning;
}