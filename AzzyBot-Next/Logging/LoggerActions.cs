using AzzyBot.Services;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Logging;

internal static partial class LoggerActions
{
    [LoggerMessage(100, LogLevel.Information, "AzzyBot is ready to accept commands")]
    public static partial void BotReady(this ILogger<DiscordBotServiceHost> logger);

    [LoggerMessage(600, LogLevel.Critical, "The given BotToken is either missing or invalid")]
    public static partial void BotTokenInvalid(this ILogger<DiscordBotServiceHost> logger);
}
