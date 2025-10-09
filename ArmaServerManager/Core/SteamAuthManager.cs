using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ArmaServerManager.Core;

public class SteamAuthManager
{
    private readonly string _authFilePath;
    private readonly byte[] _entropy;

    public SteamAuthManager(string dataPath)
    {
        var authDir = Path.Combine(dataPath, "Auth");
        Directory.CreateDirectory(authDir);
        _authFilePath = Path.Combine(authDir, "steam_auth.dat");
        _entropy = Encoding.UTF8.GetBytes(Environment.MachineName);
    }

    public void SaveCredentials(SteamCredentials credentials)
    {
        try
        {
            var json = JsonSerializer.Serialize(credentials);
            var data = Encoding.UTF8.GetBytes(json);
            var encrypted = ProtectedData.Protect(data, _entropy, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_authFilePath, encrypted);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save Steam credentials: {ex.Message}", ex);
        }
    }

    public SteamCredentials? LoadCredentials()
    {
        try
        {
            if (!File.Exists(_authFilePath))
                return null;

            var encrypted = File.ReadAllBytes(_authFilePath);
            var decrypted = ProtectedData.Unprotect(encrypted, _entropy, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(decrypted);
            return JsonSerializer.Deserialize<SteamCredentials>(json);
        }
        catch
        {
            return null;
        }
    }

    public void ClearCredentials()
    {
        if (File.Exists(_authFilePath))
            File.Delete(_authFilePath);
    }
}

public class SteamCredentials
{
    public string Username { get; set; } = string.Empty;
    public string GuardToken { get; set; } = string.Empty;
    public bool RememberCredentials { get; set; }
}