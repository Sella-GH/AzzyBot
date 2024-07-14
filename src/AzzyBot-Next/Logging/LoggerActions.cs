using System;
using System.Net.Http;
using AzzyBot.Database;
using AzzyBot.Services;
using AzzyBot.Services.Modules;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Logging;

public static partial class LoggerActions
{
    [LoggerMessage(0, LogLevel.Debug, "Starting logfile cleaning")]
    public static partial void LogfileCleaning(this ILogger<CoreServiceHost> logger);

    [LoggerMessage(1, LogLevel.Debug, "{number} logfiles were deleted")]
    public static partial void LogfileDeleted(this ILogger<CoreServiceHost> logger, int number);

    [LoggerMessage(10, LogLevel.Debug, "Starting global timer")]
    public static partial void GlobalTimerStart(this ILogger<TimerServiceHost> logger);

    [LoggerMessage(11, LogLevel.Debug, "Global timer ticked")]
    public static partial void GlobalTimerTick(this ILogger<TimerServiceHost> logger);

    [LoggerMessage(12, LogLevel.Debug, "Global timer checking for bot updates")]
    public static partial void GlobalTimerCheckForUpdates(this ILogger<TimerServiceHost> logger);

    [LoggerMessage(13, LogLevel.Debug, "Global timer checking for AzuraCast files changes")]
    public static partial void GlobalTimerCheckForAzuraCastFiles(this ILogger<TimerServiceHost> logger);

    [LoggerMessage(14, LogLevel.Debug, "Global timer checking for AzuraCast updates")]
    public static partial void GlobalTimerCheckForAzuraCastUpdates(this ILogger<TimerServiceHost> logger);

    [LoggerMessage(15, LogLevel.Debug, "Global timer checking for AzuraCast instance status")]
    public static partial void GlobalTimerCheckForAzuraCastStatus(this ILogger<TimerServiceHost> logger);

    [LoggerMessage(16, LogLevel.Debug, "Global timer checking for AzuraCast api permissions")]
    public static partial void GlobalTimerCheckForAzuraCastApi(this ILogger<TimerServiceHost> logger);

    [LoggerMessage(20, LogLevel.Debug, "BackgroundServiceHost started")]
    public static partial void BackgroundServiceHostStart(this ILogger<AzzyBackgroundServiceHost> logger);

    [LoggerMessage(21, LogLevel.Debug, "BackgroundService started")]
    public static partial void BackgroundServiceStart(this ILogger<AzzyBackgroundService> logger);

    [LoggerMessage(22, LogLevel.Debug, "BackgroundServiceHost running")]
    public static partial void BackgroundServiceHostRun(this ILogger<AzzyBackgroundServiceHost> logger);

    [LoggerMessage(23, LogLevel.Debug, "Creating work items for: {item}")]
    public static partial void BackgroundServiceWorkItem(this ILogger logger, string item);

    [LoggerMessage(30, LogLevel.Debug, "Station {dId}-{sId} has different files")]
    public static partial void BackgroundServiceStationFilesChanged(this ILogger<AzuraCastFileService> logger, int dId, int sId);

    [LoggerMessage(31, LogLevel.Debug, "Instance {id} is {status}")]
    public static partial void BackgroundServiceInstanceStatus(this ILogger<AzuraCastPingService> logger, int id, string status);

    [LoggerMessage(90, LogLevel.Debug, "Stopping global timer")]
    public static partial void GlobalTimerStop(this ILogger<TimerServiceHost> logger);

    [LoggerMessage(91, LogLevel.Debug, "BackgroundServiceHost stopped")]
    public static partial void BackgroundServiceHostStop(this ILogger<AzzyBackgroundServiceHost> logger);

    [LoggerMessage(99, LogLevel.Debug, "Operation {ops} canceled by CancellationToken")]
    public static partial void OperationCanceled(this ILogger logger, string ops);

    [LoggerMessage(100, LogLevel.Information, "Starting {name} in version {version} on {os}-{arch}")]
    public static partial void BotStarting(this ILogger<CoreServiceHost> logger, string name, string version, string os, string arch);

    [LoggerMessage(101, LogLevel.Information, "AzzyBot is ready to accept commands")]
    public static partial void BotReady(this ILogger<DiscordBotServiceHost> logger);

    [LoggerMessage(102, LogLevel.Information, "Invite me using the following url: {invite}")]
    public static partial void InviteUrl(this ILogger<DiscordBotServiceHost> logger, string invite);

    [LoggerMessage(103, LogLevel.Information, "Command {command} requested by {user} to execute")]
    public static partial void CommandRequested(this ILogger logger, string command, string user);

    [LoggerMessage(104, LogLevel.Information, "Commands error is: {ex}")]
    public static partial void CommandsErrorType(this ILogger<DiscordBotServiceHost> logger, string ex);

    [LoggerMessage(105, LogLevel.Information, "Starting Database Reencryption")]
    public static partial void DatabaseReencryptionStart(this ILogger<CoreServiceHost> logger);

    [LoggerMessage(106, LogLevel.Information, "Database Reencryption completed")]
    public static partial void DatabaseReencryptionComplete(this ILogger<CoreServiceHost> logger);

    [LoggerMessage(110, LogLevel.Information, "AzzyBot joined the following Guild: {guild}")]
    public static partial void GuildCreated(this ILogger<DiscordBotServiceHost> logger, string guild);

    [LoggerMessage(111, LogLevel.Information, "AzzyBot was removed from the following Guild: {guild}")]
    public static partial void GuildDeleted(this ILogger<DiscordBotServiceHost> logger, string guild);

    [LoggerMessage(112, LogLevel.Information, "The following guild is unavailable due to an outage: {guild}")]
    public static partial void GuildUnavailable(this ILogger<DiscordBotServiceHost> logger, string guild);

    [LoggerMessage(198, LogLevel.Information, "An update for Azzy is available! Please update now to version: {version} to get the latest fixes and improvements.")]
    public static partial void UpdateAvailable(this ILogger<UpdaterService> logger, string version);

    [LoggerMessage(199, LogLevel.Information, "Stopping AzzyBot")]
    public static partial void BotStopping(this ILogger<CoreServiceHost> logger);

    [LoggerMessage(200, LogLevel.Warning, "AzzyBot is not connected to Discord!")]
    public static partial void BotNotConnected(this ILogger logger);

    [LoggerMessage(201, LogLevel.Warning, "Commands error occured!")]
    public static partial void CommandsError(this ILogger<DiscordBotServiceHost> logger);

    [LoggerMessage(202, LogLevel.Warning, "Could not fetch channel for id {id}")]
    public static partial void ChannelNotFound(this ILogger<DiscordBotService> logger, ulong id);

    [LoggerMessage(210, LogLevel.Warning, "Could not find database item {item} for guild {guild}")]
    public static partial void DatabaseItemNotFound(this ILogger logger, string item, ulong guild);

    [LoggerMessage(220, LogLevel.Warning, "Could not find discord item {item} for guild {guild}")]
    public static partial void DiscordItemNotFound(this ILogger logger, string item, ulong guild);

    [LoggerMessage(290, LogLevel.Warning, "Latest online version of the bot is empty")]
    public static partial void OnlineVersionEmpty(this ILogger<UpdaterService> logger);

    [LoggerMessage(291, LogLevel.Warning, "Body of online version could not be deserialized")]
    public static partial void OnlineVersionUnserializable(this ILogger<UpdaterService> logger);

    [LoggerMessage(300, LogLevel.Error, "An error occured while logging the exception to discord: {ex}")]
    public static partial void UnableToLogException(this ILogger logger, string ex);

    [LoggerMessage(301, LogLevel.Error, "An error occured while sending a message to discord: {ex}")]
    public static partial void UnableToSendMessage(this ILogger logger, string ex);

    [LoggerMessage(302, LogLevel.Error, "The provided uri is invalid: {uri}")]
    public static partial void WebInvalidUri(this ILogger logger, Uri uri);

    [LoggerMessage(303, LogLevel.Error, "The {type} request to {uri} failed with error: {ex}")]
    public static partial void WebRequestFailed(this ILogger logger, HttpMethod type, string ex, Uri uri);

    [LoggerMessage(310, LogLevel.Error, "An error occured while executing the background task: {ex}")]
    public static partial void BackgroundTaskError(this ILogger logger, string ex);

    [LoggerMessage(320, LogLevel.Error, "Database transaction failed with error: ")]
    public static partial void DatabaseTransactionFailed(this ILogger logger, Exception ex);

    [LoggerMessage(400, LogLevel.Critical, "The given settings can't be parsed, are they filled out?")]
    public static partial void UnableToParseSettings(this ILogger<DiscordBotServiceHost> logger);

    [LoggerMessage(401, LogLevel.Critical, "The given BotToken is either missing or invalid")]
    public static partial void BotTokenInvalid(this ILogger<DiscordBotServiceHost> logger);

    [LoggerMessage(402, LogLevel.Critical, "An exception occured: ")]
    public static partial void ExceptionOccured(this ILogger<DiscordBotService> logger, Exception ex);
}
