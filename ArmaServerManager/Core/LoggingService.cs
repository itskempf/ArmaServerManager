using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace ArmaServerManager.Core;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
}

public class LoggingService : ILogger
{
    private readonly string _logFilePath;
    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    public LoggingService()
    {
        var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Logs");
        Directory.CreateDirectory(logsDir);
        _logFilePath = Path.Combine(logsDir, "manager.log");
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = logLevel,
            Message = message,
            Exception = exception?.ToString()
        };

        LogEntries.Add(logEntry);
        _ = WriteToFileAsync(logEntry);
    }

    public void LogInformation(string message, params object[] args) => 
        Log(LogLevel.Information, new EventId(), message, null, (s, e) => string.Format(s, args));
    
    public void LogWarning(string message, params object[] args) => 
        Log(LogLevel.Warning, new EventId(), message, null, (s, e) => string.Format(s, args));
    
    public void LogWarning(Exception? exception, string message, params object[] args) => 
        Log(LogLevel.Warning, new EventId(), message, exception, (s, e) => string.Format(s, args));
    
    public void LogError(string message, params object[] args) => 
        Log(LogLevel.Error, new EventId(), message, null, (s, e) => string.Format(s, args));
    
    public void LogError(Exception? exception, string message, params object[] args) => 
        Log(LogLevel.Error, new EventId(), message, exception, (s, e) => string.Format(s, args));
    
    private async Task WriteToFileAsync(LogEntry entry)
    {
        try
        {
            var logLine = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] {entry.Message}";
            if (!string.IsNullOrEmpty(entry.Exception))
                logLine += $"\n{entry.Exception}";
            
            await File.AppendAllTextAsync(_logFilePath, logLine + Environment.NewLine).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Log write failed: {ex.Message}");
        }
    }
    
    public async Task<string> ReadLogFileAsync()
    {
        try
        {
            if (File.Exists(_logFilePath))
                return await File.ReadAllTextAsync(_logFilePath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Log read failed: {ex.Message}");
        }
        return string.Empty;
    }
}