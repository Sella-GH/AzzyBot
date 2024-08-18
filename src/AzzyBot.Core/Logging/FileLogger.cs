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
    private static string? CurrentLogFilePath;
    private static DateTime LastFileCreationTime;

    public FileLogger(string name, Func<FileLoggerConfiguration> getConfig)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(getConfig, nameof(getConfig));

        _name = name;
        _getConfig = getConfig;
    }

    private static void InitializeLogWriter(Func<FileLoggerConfiguration> getConfig)
    {
        FileLoggerConfiguration config = getConfig();
        bool rotateFileSize = ShouldRotateBecauseOfFileSize(config);
        bool rotateTime = ShouldRotateBecauseOfTime(config);
        if (LogStream is not null && !rotateFileSize && !rotateTime)
            return;

        lock (Lock)
        {
            if (LogStream is not null)
            {
                LogStream.Dispose();
                LogStream = null;
            }

            CurrentLogFilePath = GetLogFilePath(config.Directory, rotateFileSize);
            LastFileCreationTime = DateTime.Now;

            LogStream = new StreamWriter(CurrentLogFilePath, true, Encoding.UTF8)
            {
                AutoFlush = true,
                NewLine = Environment.NewLine
            };
        }
    }

    private static bool ShouldRotateBecauseOfFileSize(FileLoggerConfiguration config)
        => !string.IsNullOrWhiteSpace(CurrentLogFilePath) && new FileInfo(CurrentLogFilePath).Length >= config.MaxFileSize;

    private static bool ShouldRotateBecauseOfTime(FileLoggerConfiguration config)
        => DateTime.Now - LastFileCreationTime >= config.MaxTimeSpan;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => default!;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel is not LogLevel.None && !string.IsNullOrWhiteSpace(_getConfig().Directory);

    private static string GetLogFilePath(string directory, bool fileSize = false)
    {
        string path;
        DateTime date = DateTime.Now.Date;
        if (!fileSize)
        {
            path = $"{date:yyyy-MM-dd}.log";
        }
        else
        {
            string[] files = Directory.GetFiles(directory, $"{date:yyyy-MM-dd}");
            path = $"{date:yyyy-MM-dd}_{files.Length + 1}.log";
        }

        return Path.Combine(directory, path);
    }

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
