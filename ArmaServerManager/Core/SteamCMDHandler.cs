using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ArmaServerManager.Core;

public class SteamCMDHandler
{
    private readonly string _steamCmdPath;
    private readonly LoggingService _logger;
    
    public event Action<string>? OutputReceived;
    public event Action<int>? ProgressChanged;

    public SteamCMDHandler(string steamCmdPath, LoggingService logger)
    {
        _steamCmdPath = steamCmdPath;
        _logger = logger;
    }
    
    public bool IsSteamCmdAvailable() => !string.IsNullOrEmpty(_steamCmdPath) && File.Exists(_steamCmdPath);

    public async Task<SteamCmdResult> InstallServerAsync(string installPath)
    {
        if (string.IsNullOrEmpty(_steamCmdPath) || !File.Exists(_steamCmdPath))
        {
            var error = "SteamCMD not found. Please configure SteamCMD path in Settings.";
            OutputReceived?.Invoke(error);
            return new SteamCmdResult { Success = false, Error = error };
        }

        OutputReceived?.Invoke($"Installing Arma 3 server to: {installPath}");
        Directory.CreateDirectory(installPath);
        
        var args = $"+force_install_dir \"{installPath}\" +login anonymous +app_update 233780 validate +quit";
        OutputReceived?.Invoke("Starting SteamCMD server installation...");
        return await ExecuteSteamCmdAsync(args);
    }

    public async Task<SteamCmdResult> DownloadModAsync(string modId, string installPath)
    {
        if (string.IsNullOrEmpty(_steamCmdPath) || !File.Exists(_steamCmdPath))
        {
            var error = "SteamCMD not found. Please configure SteamCMD path in Settings.";
            OutputReceived?.Invoke(error);
            return new SteamCmdResult { Success = false, Error = error };
        }

        OutputReceived?.Invoke($"Downloading Workshop mod {modId}...");
        var modPath = Path.Combine(installPath, "steamapps\\workshop\\content\\107410", modId);
        Directory.CreateDirectory(Path.GetDirectoryName(modPath)!);
        
        var args = $"+login anonymous +workshop_download_item 107410 {modId} +quit";
        return await ExecuteSteamCmdAsync(args);
    }

    private async Task<SteamCmdResult> ExecuteSteamCmdAsync(string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _steamCmdPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return new SteamCmdResult { Success = false, Error = "Failed to start SteamCMD process" };
            }

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    OutputReceived?.Invoke(e.Data);
                    ParseProgress(e.Data);
                }
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    errorBuilder.AppendLine(e.Data);
            };
            
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(Constants.SteamCmdTimeoutMinutes));
            var processTask = process.WaitForExitAsync();
            
            if (await Task.WhenAny(processTask, timeoutTask).ConfigureAwait(false) == timeoutTask)
            {
                process.Kill(true);
                return new SteamCmdResult { Success = false, Error = $"SteamCMD operation timed out after {Constants.SteamCmdTimeoutMinutes} minutes" };
            }

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();
            
            // Check for Steam login errors
            if (output.Contains("FAILED login") || output.Contains("Two-factor code") || output.Contains("Steam Guard"))
            {
                return new SteamCmdResult { Success = false, Error = "Steam authentication required. Please login manually first.", Output = output };
            }

            var result = new SteamCmdResult
            {
                Success = process.ExitCode == 0 && !output.Contains("Error!"),
                ExitCode = process.ExitCode,
                Output = output,
                Error = error
            };

            _logger.LogInformation("SteamCMD executed: {Args}, Exit Code: {ExitCode}", arguments, process.ExitCode);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute SteamCMD: {Args}", arguments);
            return new SteamCmdResult { Success = false, Error = ex.Message };
        }
    }
    
    private void ParseProgress(string output)
    {
        if (output.Contains("progress:"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(output, @"progress:\s*(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var progress))
            {
                ProgressChanged?.Invoke(progress);
            }
        }
    }
    
    public async Task<bool> VerifySteamCmdAsync()
    {
        if (!IsSteamCmdAvailable())
            return false;
        
        try
        {
            var result = await ExecuteSteamCmdAsync("+quit").ConfigureAwait(false);
            return result.Success || result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

public class SteamCmdResult
{
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}