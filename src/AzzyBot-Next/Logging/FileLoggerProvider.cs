using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzzyBot.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly IDisposable? _onChangeToken;
    private FileLoggerConfiguration _configuration;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public FileLoggerProvider(IOptionsMonitor<FileLoggerConfiguration> config)
    {
        ArgumentNullException.ThrowIfNull(config, nameof(config));

        _configuration = config.CurrentValue;
        _onChangeToken = config.OnChange(updatedConfig => _configuration = updatedConfig);
    }

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, name => new FileLogger(name, GetCurrentConfig));

    private FileLoggerConfiguration GetCurrentConfig()
        => _configuration;

    public void Dispose()
    {
        _loggers.Clear();
        _onChangeToken?.Dispose();
    }
}
