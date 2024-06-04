using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Logging;

public sealed class FileLogger(string path) : ILogger, IDisposable
{
    private readonly string _path = path;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Dispose() => Dispose(true);

    public void Dispose(bool disposing)
    {
        if (disposing)
            _semaphore.Dispose();
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter, nameof(formatter));

        if (!IsEnabled(logLevel))
            return;

        _semaphore.Wait();

        string message = formatter(state, exception);
        string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {message}{Environment.NewLine}";

        if (string.IsNullOrWhiteSpace(logMessage))
        {
            _semaphore.Release();
            return;
        }

        try
        {
            File.AppendAllText(_path, logMessage);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
