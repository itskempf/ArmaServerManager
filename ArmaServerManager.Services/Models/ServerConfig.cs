using System.Collections.Generic;

namespace ArmaServerManager.Models;

public class ServerConfig
{
    public string Hostname { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public int MaxPlayers { get; set; } = 64;
    public List<string> Motd { get; set; } = new() { "Welcome to the server!" };
    public List<string> Admins { get; set; } = new();
    public List<string> HeadlessClients { get; set; } = new() { "127.0.0.1" };
    public List<string> LocalClient { get; set; } = new() { "127.0.0.1" };
    public int VerifySignatures { get; set; } = 2;
    public bool KickDuplicate { get; set; } = true;
    public int AllowedFilePatching { get; set; } = 0;
    public int MinBandwidth { get; set; } = 131072;
    public int MaxBandwidth { get; set; } = 2147483647;
    public int MaxMsgSend { get; set; } = 256;
    public int MaxSizeGuaranteed { get; set; } = 512;
    public int MaxSizeNonguaranteed { get; set; } = 256;
    public string TimeStampFormat { get; set; } = "short";
    public string LogFile { get; set; } = "server_console.log";
    public List<Mission> Missions { get; set; } = new();
}

public class Mission
{
    public string Template { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "regular";
    public string Name { get; set; } = string.Empty;
}
