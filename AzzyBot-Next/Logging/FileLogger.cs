using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Logging;

public sealed class FileLogger(string directory) : ILogger, IDisposable
{
    private readonly string _directory = directory;
    private readonly object _lock = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;
    public bool IsEnabled(LogLevel logLevel) => true;
    private string GetLogFilePath() => Path.Combine(_directory, $"{DateTime.Now:yyyy-MM-dd}.log");

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter, nameof(formatter));

        if (!IsEnabled(logLevel) || string.IsNullOrWhiteSpace(formatter(state, exception)))
            return;

        lock (_lock)
        {
            string message = formatter(state, exception);
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {message}{Environment.NewLine}";

            File.AppendAllText(GetLogFilePath(), logMessage);
        }
    }

    public void Dispose() { }
}
