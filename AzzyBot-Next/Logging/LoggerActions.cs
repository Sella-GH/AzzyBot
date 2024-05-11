﻿using System;
using System.Net.Http;
using AzzyBot.Services;
using AzzyBot.Services.Modules;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Logging;

internal static partial class LoggerActions
{
    [LoggerMessage(0, LogLevel.Debug, "Starting global timer")]
    public static partial void GlobalTimerStart(this ILogger<TimerServiceHost> logger);

    [LoggerMessage(1, LogLevel.Debug, "Global timer ticked")]
    public static partial void GlobalTimerTicked(this ILogger<TimerServiceHost> logger);

    [LoggerMessage(2, LogLevel.Debug, "Global timer checking for bot updates")]
    public static partial void GlobalTimerCheckForUpdates(this ILogger<TimerServiceHost> logger);

    [LoggerMessage(9, LogLevel.Debug, "Stopping global timer")]
    public static partial void GlobalTimerStop(this ILogger<TimerServiceHost> logger);

    [LoggerMessage(100, LogLevel.Information, "Starting {name} in version {version} on {os}-{arch}")]
    public static partial void BotStarting(this ILogger<CoreServiceHost> logger, string name, string version, string os, string arch);

    [LoggerMessage(101, LogLevel.Information, "AzzyBot is ready to accept commands")]
    public static partial void BotReady(this ILogger<DiscordBotServiceHost> logger);

    [LoggerMessage(102, LogLevel.Information, "Invite me using the following url: https://discord.com/api/oauth2/authorize?client_id={id}&permissions=268438528&scope=applications.commands%20bot")]
    public static partial void InviteUrl(this ILogger<DiscordBotServiceHost> logger, ulong id);

    [LoggerMessage(103, LogLevel.Information, "Command {command} requested by {user} to execute")]
    public static partial void CommandRequested(this ILogger logger, string command, string user);

    [LoggerMessage(197, LogLevel.Information, "An update for Azzy is available! Please update now to version: {version} to get the latest fixes and improvements.")]
    public static partial void UpdateAvailable(this ILogger<UpdaterService> logger, Version version);

    [LoggerMessage(198, LogLevel.Information, "Press any key to stop the bot...")]
    public static partial void PressAnyKeyToStop(this ILogger logger);

    [LoggerMessage(199, LogLevel.Information, "Stopping AzzyBot")]
    public static partial void BotStopping(this ILogger<CoreServiceHost> logger);

    [LoggerMessage(200, LogLevel.Warning, "Commands error occured!")]
    public static partial void CommandsError(this ILogger<DiscordBotServiceHost> logger);

    [LoggerMessage(300, LogLevel.Error, "{setting} has to be filled out!")]
    public static partial void SettingNotFilled(this ILogger logger, string setting);

    [LoggerMessage(301, LogLevel.Error, "An error happend while logging the exception to discord: {ex}")]
    public static partial void UnableToLogException(this ILogger logger, string ex);

    [LoggerMessage(302, LogLevel.Error, "An error happend while sending a message to discord: {ex}")]
    public static partial void UnableToSendMessage(this ILogger logger, string ex);

    [LoggerMessage(303, LogLevel.Error, "The provided uri is invalid: {uri}")]
    public static partial void WebInvalidUri(this ILogger logger, Uri uri);

    [LoggerMessage(304, LogLevel.Error, "The {type} request failed with error: {ex}")]
    public static partial void WebRequestFailed(this ILogger logger, HttpMethod type, string ex);

    [LoggerMessage(390, LogLevel.Warning, "Latest online version of the bot is empty")]
    public static partial void OnlineVersionEmpty(this ILogger<UpdaterService> logger);

    [LoggerMessage(391, LogLevel.Warning, "Body of online version could not be deserialized")]
    public static partial void OnlineVersionUnserializable(this ILogger<UpdaterService> logger);

    [LoggerMessage(400, LogLevel.Critical, "The given settings can't be parsed, are they filled out?")]
    public static partial void UnableToParseSettings(this ILogger<DiscordBotServiceHost> logger);

    [LoggerMessage(401, LogLevel.Critical, "The given BotToken is either missing or invalid")]
    public static partial void BotTokenInvalid(this ILogger<DiscordBotServiceHost> logger);

    [LoggerMessage(402, LogLevel.Critical, "An exception occured: ")]
    public static partial void ExceptionOccured(this ILogger<DiscordBotService> logger, Exception ex);
}
