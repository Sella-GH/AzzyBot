using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Core.Logging;

public sealed class FileLogger : ILogger
{
    private readonly string _name;
    private readonly Func<FileLoggerConfiguration> _getConfig;
    private static StreamWriter? LogStream;
    private static readonly object Lock = new();

    public FileLogger(string name, Func<FileLoggerConfiguration> getConfig)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(getConfig, nameof(getConfig));

        _name = name;
        _getConfig = getConfig;
    }

    private static void InitializeLogWriter(Func<FileLoggerConfiguration> getConfig)
    {
        if (LogStream is not null)
            return;

        lock (Lock)
        {
            FileLoggerConfiguration config = getConfig();
            string logFilePath = GetLogFilePath(config.Directory);
            LogStream = new StreamWriter(logFilePath, true, Encoding.UTF8)
            {
                AutoFlush = true,
                NewLine = Environment.NewLine
            };
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => default!;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel != LogLevel.None && !string.IsNullOrWhiteSpace(_getConfig().Directory);

    private static string GetLogFilePath(string directory)
        => Path.Combine(directory, $"{DateTime.Now:yyyy-MM-dd}.log");

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter, nameof(formatter));

        if (!IsEnabled(logLevel))
            return;

        InitializeLogWriter(_getConfig);

        lock (Lock)
        {
            string message = formatter(state, exception);
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {logLevel}: {_name}[{eventId.Id}] {message}{exception}";

            LogStream?.WriteLine(logMessage);
        }
    }
}
