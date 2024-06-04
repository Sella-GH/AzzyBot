using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Logging;

public sealed class FileLoggerProvider(string filePath) : ILoggerProvider
{
    private readonly string _filePath = filePath;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new FileLogger(_filePath));
    public void Dispose() => Dispose(true);

    public void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        foreach (KeyValuePair<string, FileLogger> logger in _loggers)
        {
            logger.Value.Dispose();
        }

        _loggers.Clear();
    }
}
