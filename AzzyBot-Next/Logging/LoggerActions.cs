using AzzyBot.Services;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Logging;

internal static partial class LoggerActions
{
    [LoggerMessage(100, LogLevel.Information, "AzzyBot is ready to accept commands")]
    public static partial void BotReady(this ILogger<DiscordBotService> logger);

    [LoggerMessage(101, LogLevel.Information, "https://discord.com/api/oauth2/authorize?client_id={id}&permissions=268438528&scope=applications.commands%20bot")]
    public static partial void InviteUrl(this ILogger<DiscordBotService> logger, ulong id);

    [LoggerMessage(600, LogLevel.Critical, "The given settings can't be parsed, are they filled out?")]
    public static partial void UnableToParseSettings(this ILogger<DiscordBotService> logger);

    [LoggerMessage(610, LogLevel.Critical, "The given BotToken is either missing or invalid")]
    public static partial void BotTokenInvalid(this ILogger<DiscordBotService> logger);
}
