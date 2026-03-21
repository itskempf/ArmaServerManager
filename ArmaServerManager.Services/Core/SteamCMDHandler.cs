using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ArmaServerManager.Core;

/// <summary>
/// Handles interaction with the SteamCMD command-line tool.
/// </summary>
public class SteamCMDHandler : ISteamCMDHandler
{
    private const string Arma3ClientAppId = "107410";
    private const string Arma3ServerAppId = "233780";

    private readonly string _steamCmdPath;
    private readonly ILogger _logger;
    
    /// <summary>
    /// Fired when SteamCMD produces a new line of output.
    /// </summary>
    public event Action<string>? OutputReceived;

    /// <summary>
    /// Fired when progress information is parsed from SteamCMD output.
    /// </summary>
    public event Action<int>? ProgressChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="SteamCMDHandler"/> class.
    /// </summary>
    /// <param name="steamCmdPath">The full path to the steamcmd.exe executable.</param>
    /// <param name="logger">The service for logging.</param>
    public SteamCMDHandler(string steamCmdPath, ILogger logger)
    {
        _steamCmdPath = steamCmdPath;
        _logger = logger;
    }
    
    /// <summary>
    /// Checks if the SteamCMD executable is available at the configured path.
    /// </summary>
    /// <returns>True if steamcmd.exe exists, otherwise false.</returns>
    public bool IsSteamCmdAvailable() => !string.IsNullOrEmpty(_steamCmdPath) && File.Exists(_steamCmdPath);

    /// <summary>
    /// Executes the SteamCMD command to install or update the Arma 3 dedicated server.
    /// </summary>
    /// <param name="installPath">The directory to install the server to.</param>
    /// <returns>A <see cref="SteamCmdResult"/> with the outcome of the operation.</returns>
    public async Task<SteamCmdResult> InstallServerAsync(string installPath)
    {
        if (string.IsNullOrEmpty(_steamCmdPath) || !File.Exists(_steamCmdPath))
        {
            var error = "SteamCMD not found. Please configure SteamCMD path in Settings.";
            OutputReceived?.Invoke(error);
            return new SteamCmdResult { Success = false, Error = error };
        }

        if (installPath.Any(c => Path.GetInvalidPathChars().Contains(c)) || installPath.Contains('\"'))
        {
            var error = "Invalid install path. Path contains invalid characters or quotes.";
            _logger.LogError(error);
            return new SteamCmdResult { Success = false, Error = error };
        }

        OutputReceived?.Invoke($"Installing Arma 3 server to: {installPath}");
        Directory.CreateDirectory(installPath);
        
        var args = $"+force_install_dir \"{installPath}\" +login anonymous +app_update {Arma3ServerAppId} validate +quit";
        OutputReceived?.Invoke("Starting SteamCMD server installation...");
        return await ExecuteSteamCmdAsync(args);
    }

    /// <summary>
    /// Executes the SteamCMD command to download a workshop mod.
    /// </summary>
    /// <param name="modId">The Steam Workshop ID of the mod.</param>
    /// <param name="installPath">The root directory for mod installations.</param>
    /// <returns>A <see cref="SteamCmdResult"/> with the outcome of the operation.</returns>
    public async Task<SteamCmdResult> DownloadModAsync(string modId, string installPath)
    {
        if (string.IsNullOrEmpty(_steamCmdPath) || !File.Exists(_steamCmdPath))
        {
            var error = "SteamCMD not found. Please configure SteamCMD path in Settings.";
            OutputReceived?.Invoke(error);
            return new SteamCmdResult { Success = false, Error = error };
        }

        if (!long.TryParse(modId, out _))
        {
            var error = $"Invalid Mod ID: {modId}. Mod ID must be numeric.";
            _logger.LogError(error);
            return new SteamCmdResult { Success = false, Error = error };
        }

        if (installPath.Any(c => Path.GetInvalidPathChars().Contains(c)) || installPath.Contains('\"'))
        {
            var error = "Invalid install path. Path contains invalid characters or quotes.";
            _logger.LogError(error);
            return new SteamCmdResult { Success = false, Error = error };
        }

        OutputReceived?.Invoke($"Downloading Workshop mod {modId}...");
        var modPath = Path.Combine(installPath, @"steamapps\workshop\content\107410", modId);
        Directory.CreateDirectory(Path.GetDirectoryName(modPath)!);
        
        var args = $"+login anonymous +workshop_download_item {Arma3ClientAppId} {modId} +quit";
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
    
    /// <summary>
    /// Verifies that the SteamCMD executable can be successfully executed.
    /// </summary>
    /// <returns>True if SteamCMD runs and exits cleanly, otherwise false.</returns>
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

/// <summary>
/// Represents the result of a SteamCMD operation.
/// </summary>
public class SteamCmdResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sets the exit code of the SteamCMD process.
    /// </summary>
    public int ExitCode { get; set; }
    /// <summary>
    /// Gets or sets the standard output from the SteamCMD process.
    /// </summary>
    public string Output { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the standard error output from the SteamCMD process.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}