using System;
using System.Threading.Tasks;

namespace ArmaServerManager.Core
{
    public interface ISteamCMDHandler
    {
        event Action<string>? OutputReceived;
        event Action<int>? ProgressChanged;
        
        bool IsSteamCmdAvailable();
        Task<SteamCmdResult> InstallServerAsync(string installPath);
        Task<SteamCmdResult> DownloadModAsync(string modId, string installPath);
        Task<bool> VerifySteamCmdAsync();
    }
}
