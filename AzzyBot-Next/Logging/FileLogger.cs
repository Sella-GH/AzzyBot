using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Logging;

public sealed class FileLogger(string filePath) : ILogger, IDisposable
{
    private readonly string _filePath = filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
    public IDisposable? BeginScope<TState>(TState? state) => null;
#pragma warning restore CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.

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
            File.AppendAllText(_filePath, logMessage);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
