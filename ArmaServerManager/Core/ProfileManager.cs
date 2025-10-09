using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArmaServerManager.Core;

public class ProfileManager
{
    private readonly ConfigManager _configManager;
    private readonly SettingsService _settingsService;
    private readonly LoggingService _logger;
    private readonly string _profilesPath;
    
    public ObservableCollection<ServerProfile> Profiles { get; } = new();
    public ServerProfile? CurrentProfile { get; private set; }

    public ProfileManager(ConfigManager configManager, SettingsService settingsService, LoggingService logger)
    {
        _configManager = configManager;
        _settingsService = settingsService;
        _logger = logger;
        _profilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Profiles");
        Directory.CreateDirectory(_profilesPath);
        LoadProfiles();
    }

    public async Task SaveProfileAsync(string name, ArmaServer server, string description = "")
    {
        try
        {
            var profile = new ServerProfile
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Server = server,
                Description = description,
                CreatedDate = DateTime.Now,
                LastModified = DateTime.Now
            };
            
            var existingProfile = Profiles.FirstOrDefault(p => p.Name == name);
            if (existingProfile != null)
            {
                profile.Id = existingProfile.Id;
                profile.CreatedDate = existingProfile.CreatedDate;
                Profiles.Remove(existingProfile);
            }
            
            Profiles.Add(profile);
            await _configManager.SaveConfigAsync(server).ConfigureAwait(false);
            await SaveProfileToFileAsync(profile).ConfigureAwait(false);
            
            _logger.LogInformation("Profile saved: {ProfileName}", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save profile: {ProfileName}", name);
            throw;
        }
    }

    public void LoadProfile(ServerProfile profile)
    {
        CurrentProfile = profile;
        _logger.LogInformation("Profile loaded: {ProfileName}", profile.Name);
    }

    public Task DeleteProfileAsync(ServerProfile profile)
    {
        try
        {
            Profiles.Remove(profile);
            var profileFile = Path.Combine(_profilesPath, $"{profile.Id}.json");
            if (File.Exists(profileFile))
                File.Delete(profileFile);
            
            if (CurrentProfile == profile)
                CurrentProfile = Profiles.FirstOrDefault();
            
            _logger.LogInformation("Profile deleted: {ProfileName}", profile.Name);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete profile: {ProfileName}", profile.Name);
            throw;
        }
    }

    private void LoadProfiles()
    {
        try
        {
            if (Directory.Exists(_profilesPath))
            {
                foreach (var file in Directory.GetFiles(_profilesPath, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var profile = JsonSerializer.Deserialize<ServerProfile>(json);
                        if (profile != null)
                            Profiles.Add(profile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load profile from: {File}", file);
                    }
                }
            }
            
            if (Profiles.Count == 0)
            {
                var defaultProfile = new ServerProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Default",
                    Server = _configManager.GenerateDefaultConfig("Default Server", 
                        Path.Combine(_settingsService.Settings.Directories.Servers, "Default")),
                    Description = "Default server profile",
                    CreatedDate = DateTime.Now,
                    LastModified = DateTime.Now
                };
                
                Profiles.Add(defaultProfile);
            }
            
            CurrentProfile = Profiles.FirstOrDefault();
            _logger.LogInformation("Loaded {Count} profiles", Profiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profiles");
        }
    }
    
    private async Task SaveProfileToFileAsync(ServerProfile profile)
    {
        var profileFile = Path.Combine(_profilesPath, $"{profile.Id}.json");
        var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(profileFile, json).ConfigureAwait(false);
    }
}

public class ServerProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ArmaServer Server { get; set; } = new();
    public DateTime CreatedDate { get; set; }
    public DateTime LastModified { get; set; }
    public string Description { get; set; } = string.Empty;
}