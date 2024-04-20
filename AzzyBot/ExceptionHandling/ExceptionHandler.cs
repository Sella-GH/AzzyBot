using System;
using DSharpPlus;
using Microsoft.Extensions.Logging;

namespace AzzyBot.ExceptionHandling;

/// <summary>
/// Handles exceptions and logs them.
/// </summary>
internal static class ExceptionHandler
{
    private static readonly Action<ILogger, string, Exception?> LogDebug = LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1000, "Debug"), "{Message}");
    private static readonly Action<ILogger, string, Exception?> LogInfo = LoggerMessage.Define<string>(LogLevel.Information, new EventId(1001, "Info"), "{Message}");
    private static readonly Action<ILogger, string, Exception?> LogWarn = LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1002, "Warning"), "{Message}");
    private static readonly Action<ILogger, string, Exception?> LogErr = LoggerMessage.Define<string>(LogLevel.Error, new EventId(1003, "Error"), "{Message}");
    private static readonly Action<ILogger, string, Exception?> LogCrit = LoggerMessage.Define<string>(LogLevel.Critical, new EventId(1004, "Crit"), "{Message}");

    /// <summary>
    /// Logs a message.
    /// </summary>
    /// <param name="level">The log level of the message.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="jsonMessage">The JSON message if the error happened during a web request.</param>
    /// <exception cref="ArgumentOutOfRangeException">Throws when the log level is not in the <seealso cref="LogLevel"/> enum.</exception>
    internal static bool LogMessage(LogLevel level, string message, string jsonMessage = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message, nameof(message));

        ILogger<BaseDiscordClient> client = AzzyBot.GetDiscordClientLogger;

        switch (level)
        {
            case LogLevel.Debug:
                LogDebug(client, message, null);
                break;

            case LogLevel.Information:
                LogInfo(client, message, null);
                break;

            case LogLevel.Warning:
                LogWarn(client, message, null);
                break;

            case LogLevel.Error:
                LogErr(client, message, null);
                break;

            case LogLevel.Critical:
                LogCrit(client, message, null);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(level), level.ToString(), "Value is not defined");
        }

        if (!string.IsNullOrWhiteSpace(jsonMessage))
            LogErr(client, jsonMessage, null);

        return true;
    }
}
