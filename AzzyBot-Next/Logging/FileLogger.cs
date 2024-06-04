using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Logging;

public sealed class FileLogger(string name, Func<FileLoggerConfiguration> getConfig) : ILogger
{
    private readonly string _name = name;
    private readonly object _lock = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => default!;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel != LogLevel.None && !string.IsNullOrWhiteSpace(getConfig().Directory);

    private static string GetLogFilePath(string directory)
        => Path.Combine(directory, $"{DateTime.Now:yyyy-MM-dd}.log");

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter, nameof(formatter));

        if (!IsEnabled(logLevel))
            return;

        lock (_lock)
        {
            FileLoggerConfiguration config = getConfig();
            string message = formatter(state, exception);
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {logLevel}: {_name}[{eventId.Id}] {message}{Environment.NewLine}";

            File.AppendAllText(GetLogFilePath(config.Directory), logMessage);
        }
    }
}
