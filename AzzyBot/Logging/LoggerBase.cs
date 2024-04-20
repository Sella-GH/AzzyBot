using System;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Logging;

internal static class LoggerBase
{
    private static ILoggerFactory? Factory;
    private static ILogger<AzzyBot>? Logger;

    internal static ILoggerFactory GetLoggerFactory => Factory ?? throw new InvalidOperationException("LoggerFactory is null!");
    internal static ILogger<AzzyBot> GetLogger => Logger ?? throw new InvalidOperationException("Logger is null!");

    internal static readonly Action<ILogger, string, Exception?> LogTrace = LoggerMessage.Define<string>(LogLevel.Trace, new(0, nameof(LogLevel.Trace)), "{Message}");
    internal static readonly Action<ILogger, string, Exception?> LogDebug = LoggerMessage.Define<string>(LogLevel.Debug, new(1, nameof(LogLevel.Debug)), "{Message}");
    internal static readonly Action<ILogger, string, Exception?> LogInfo = LoggerMessage.Define<string>(LogLevel.Information, new(2, nameof(LogLevel.Information)), "{Message}");
    internal static readonly Action<ILogger, string, Exception?> LogWarn = LoggerMessage.Define<string>(LogLevel.Warning, new(3, nameof(LogLevel.Warning)), "{Message}");
    internal static readonly Action<ILogger, string, Exception?> LogError = LoggerMessage.Define<string>(LogLevel.Error, new(4, nameof(LogLevel.Error)), "{Message}");
    internal static readonly Action<ILogger, string, Exception?> LogCrit = LoggerMessage.Define<string>(LogLevel.Critical, new(5, nameof(LogLevel.Critical)), "{Message}");

    internal static void CreateLogger(string botName)
    {
        LogLevel level = LogLevel.Information;

        if (botName is "AzzyBot-Dev")
            level = LogLevel.Debug;

        Factory = LoggerFactory.Create(builder => builder.AddConsole().AddFilter(string.Empty, level));
        Logger = Factory.CreateLogger<AzzyBot>();

        LogDebug(Logger, "Logger created!", null);
    }

    internal static void DisposeLogger()
    {
        if (Logger is null)
            return;

        LogDebug(Logger, "Logger disposing!", null);

        Factory?.Dispose();
        Factory = null;
        Logger = null;
    }
}
