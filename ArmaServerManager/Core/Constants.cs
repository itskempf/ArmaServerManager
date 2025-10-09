namespace ArmaServerManager.Core;

public static class Constants
{
    // Application
    public const string AppName = "Arma Server Manager";
    public const string AppVersion = "1.0.0";
    
    // Steam
    public const int Arma3AppId = 233780;
    public const int Arma3WorkshopId = 107410;
    public const string SteamWorkshopUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=";
    
    // Server
    public const int DefaultServerPort = 2302;
    public const int DefaultMaxPlayers = 64;
    public const string ServerExecutable = "arma3server_x64.exe";
    
    // Paths
    public const string DataDirectory = "Data";
    public const string ConfigsDirectory = "Configs";
    public const string ModsDirectory = "Mods";
    public const string PresetsDirectory = "Presets";
    public const string LogsDirectory = "Logs";
    public const string BackupsDirectory = "Backups";
    public const string ProfilesDirectory = "Profiles";
    
    // Files
    public const string SettingsFile = "settings.json";
    public const string ModsConfigFile = "mods.json";
    public const string ServerConfigFile = "server.cfg";
    public const string LogFile = "manager.log";
    
    // Timeouts
    public const int SteamCmdTimeoutMinutes = 30;
    public const int ServerStartTimeoutSeconds = 30;
    public const int ServerStopTimeoutSeconds = 10;
    
    // Limits
    public const int MaxServerNameLength = 64;
    public const int MinServerNameLength = 3;
    public const int MaxBackupAgeDays = 30;
    public const int MaxLogEntries = 1000;
    public const int MaxResourceDataPoints = 1000;
}
