using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ArmaServerManager.Core;

public static class Utilities
{
    public static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        if (uptime.TotalHours >= 1)
            return $"{(int)uptime.TotalHours}h {uptime.Minutes}m";
        return $"{uptime.Minutes}m {uptime.Seconds}s";
    }

    public static bool IsValidWorkshopId(string workshopId)
    {
        return !string.IsNullOrWhiteSpace(workshopId) && 
               Regex.IsMatch(workshopId, @"^\d+$") && 
               workshopId.Length <= 12;
    }

    public static bool IsValidPort(int port)
    {
        return port >= 1024 && port <= 65535;
    }

    public static bool IsValidServerName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && 
               name.Length >= 3 && 
               name.Length <= 64 &&
               Regex.IsMatch(name, @"^[a-zA-Z0-9\s\-_]+$");
    }

    public static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }

    public static string GetRelativePath(string fromPath, string toPath)
    {
        if (string.IsNullOrEmpty(fromPath)) return toPath;
        if (string.IsNullOrEmpty(toPath)) return string.Empty;

        Uri fromUri = new Uri(AppendDirectorySeparatorChar(fromPath));
        Uri toUri = new Uri(AppendDirectorySeparatorChar(toPath));

        if (fromUri.Scheme != toUri.Scheme) return toPath;

        Uri relativeUri = fromUri.MakeRelativeUri(toUri);
        string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        return relativePath;
    }

    private static string AppendDirectorySeparatorChar(string path)
    {
        if (!Path.HasExtension(path) && !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            return path + Path.DirectorySeparatorChar;
        return path;
    }

    public static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path))
            return 0;

        try
        {
            var dirInfo = new DirectoryInfo(path);
            return dirInfo.GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        }
        catch
        {
            return 0;
        }
    }

    public static bool IsProcessRunning(int processId)
    {
        try
        {
            var process = System.Diagnostics.Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }
}
