using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Core.Logging;

public static partial class LoggerActions
{
    [LoggerMessage(0, LogLevel.Debug, "Starting logfile cleaning")]
    public static partial void LogfileCleaning(this ILogger logger);

    [LoggerMessage(1, LogLevel.Debug, "{number} logfiles were deleted")]
    public static partial void LogfileDeleted(this ILogger logger, int number);

    [LoggerMessage(10, LogLevel.Debug, "Starting global timer")]
    public static partial void GlobalTimerStart(this ILogger logger);

    [LoggerMessage(11, LogLevel.Debug, "Global timer ticked")]
    public static partial void GlobalTimerTick(this ILogger logger);

    [LoggerMessage(12, LogLevel.Debug, "Global timer checking for bot updates")]
    public static partial void GlobalTimerCheckForUpdates(this ILogger logger);

    [LoggerMessage(13, LogLevel.Debug, "Global timer checking {counter} guilds for channel permissions")]
    public static partial void GlobalTimerCheckForChannelPermissions(this ILogger logger, int counter);

    [LoggerMessage(14, LogLevel.Debug, "Global timer checking {counter} guilds for AzuraCast files changes")]
    public static partial void GlobalTimerCheckForAzuraCastFiles(this ILogger logger, int counter);

    [LoggerMessage(15, LogLevel.Debug, "Global timer checking {counter} guilds for AzuraCast updates")]
    public static partial void GlobalTimerCheckForAzuraCastUpdates(this ILogger logger, int counter);

    [LoggerMessage(16, LogLevel.Debug, "Global timer checking {counter} guilds for AzuraCast instance status")]
    public static partial void GlobalTimerCheckForAzuraCastStatus(this ILogger logger, int counter);

    [LoggerMessage(17, LogLevel.Debug, "Global timer checking {counter} guilds for AzuraCast api permissions")]
    public static partial void GlobalTimerCheckForAzuraCastApi(this ILogger logger, int counter);

    [LoggerMessage(20, LogLevel.Debug, "BackgroundServiceHost started")]
    public static partial void BackgroundServiceHostStart(this ILogger logger);

    [LoggerMessage(21, LogLevel.Debug, "BackgroundService started")]
    public static partial void BackgroundServiceStart(this ILogger logger);

    [LoggerMessage(22, LogLevel.Debug, "BackgroundServiceHost running")]
    public static partial void BackgroundServiceHostRun(this ILogger logger);

    [LoggerMessage(23, LogLevel.Debug, "Creating work items for: {item}")]
    public static partial void BackgroundServiceWorkItem(this ILogger logger, string item);

    [LoggerMessage(30, LogLevel.Debug, "Station {gId}-{iId}-{dId}-{sId} has different files")]
    public static partial void BackgroundServiceStationFilesChanged(this ILogger logger, int gId, int iId, int dId, int sId);

    [LoggerMessage(31, LogLevel.Debug, "Instance {gId}-{iId} is {status}")]
    public static partial void BackgroundServiceInstanceStatus(this ILogger logger, int gId, int iId, string status);

    [LoggerMessage(32, LogLevel.Debug, "Song request {rId} from {gId}-{iId}-{dId}-{sId} is waiting {time} seconds")]
    public static partial void BackgroundServiceSongRequestWaiting(this ILogger logger, string rId, int gId, int iId, int dId, int sId, int time);

    [LoggerMessage(33, LogLevel.Debug, "Song request {rId} from {gId}-{iId}-{dId}-{sId} requeued")]
    public static partial void BackgroundServiceSongRequestRequed(this ILogger logger, string rId, int gId, int iId, int dId, int sId);

    [LoggerMessage(34, LogLevel.Debug, "Song request {rId} from {gId}-{iId}-{dId}-{sId} finished")]
    public static partial void BackgroundServiceSongRequestFinished(this ILogger logger, string rId, int gId, int iId, int dId, int sId);

    [LoggerMessage(40, LogLevel.Debug, "AzuraCastDiscordPermission is {perm}")]
    public static partial void AzuraCastDiscordPermission(this ILogger logger, string perm);

    [LoggerMessage(41, LogLevel.Debug, "User {user} is not connected to a voice channel")]
    public static partial void UserNotConnected(this ILogger logger, string user);

    [LoggerMessage(42, LogLevel.Debug, "Setting channelId to 0 because user is not connected to a voice channel")]
    public static partial void UserNotConnectedSetChannelId(this ILogger logger);

    [LoggerMessage(50, LogLevel.Debug, "Expected failure of a {type} request to {uri} with exception name {ex}")]
    public static partial void WebRequestExpectedFailure(this ILogger logger, HttpMethod type, Uri uri, string ex);

    [LoggerMessage(90, LogLevel.Debug, "Stopping global timer")]
    public static partial void GlobalTimerStop(this ILogger logger);

    [LoggerMessage(91, LogLevel.Debug, "BackgroundServiceHost stopped")]
    public static partial void BackgroundServiceHostStop(this ILogger logger);

    [LoggerMessage(99, LogLevel.Debug, "Operation {ops} canceled by CancellationToken")]
    public static partial void OperationCanceled(this ILogger logger, string ops);

    [LoggerMessage(100, LogLevel.Information, "Starting {name} in version {version} on {os}-{arch}")]
    public static partial void BotStarting(this ILogger logger, string name, string version, string os, string arch);

    [LoggerMessage(101, LogLevel.Information, "AzzyBot is ready to accept commands")]
    public static partial void BotReady(this ILogger logger);

    [LoggerMessage(102, LogLevel.Information, "Invite me using the following url: {invite}")]
    public static partial void InviteUrl(this ILogger logger, string invite);

    [LoggerMessage(103, LogLevel.Information, "Command {command} requested by {user} to execute")]
    public static partial void CommandRequested(this ILogger logger, string command, string user);

    [LoggerMessage(104, LogLevel.Information, "Commands error is: {ex}")]
    public static partial void CommandsErrorType(this ILogger logger, string ex);

    [LoggerMessage(105, LogLevel.Information, "Starting Database Reencryption")]
    public static partial void DatabaseReencryptionStart(this ILogger logger);

    [LoggerMessage(106, LogLevel.Information, "Database Reencryption completed")]
    public static partial void DatabaseReencryptionComplete(this ILogger logger);

    [LoggerMessage(110, LogLevel.Information, "AzzyBot joined the following Guild: {guild}")]
    public static partial void GuildCreated(this ILogger logger, string guild);

    [LoggerMessage(111, LogLevel.Information, "AzzyBot was removed from the following Guild: {guild}")]
    public static partial void GuildDeleted(this ILogger logger, string guild);

    [LoggerMessage(112, LogLevel.Information, "The following guild is unavailable due to an outage: {guild}")]
    public static partial void GuildUnavailable(this ILogger logger, string guild);

    [LoggerMessage(198, LogLevel.Information, "An update for Azzy is available! Please update now to version: {version} to get the latest fixes and improvements.")]
    public static partial void UpdateAvailable(this ILogger logger, string version);

    [LoggerMessage(199, LogLevel.Information, "Stopping AzzyBot")]
    public static partial void BotStopping(this ILogger logger);

    [LoggerMessage(200, LogLevel.Warning, "AzzyBot is not connected to Discord!")]
    public static partial void BotNotConnected(this ILogger logger);

    [LoggerMessage(201, LogLevel.Warning, "Commands error occured!")]
    public static partial void CommandsError(this ILogger logger);

    [LoggerMessage(202, LogLevel.Warning, "Could not fetch channel for id {id}")]
    public static partial void ChannelNotFound(this ILogger logger, ulong id);

    [LoggerMessage(210, LogLevel.Warning, "Could not find Guild item for guild {guild}")]
    public static partial void DatabaseGuildNotFound(this ILogger logger, ulong guild);

    [LoggerMessage(211, LogLevel.Warning, "Could not find Guild preferences for guild {guild}")]
    public static partial void DatabaseGuildPreferencesNotFound(this ILogger logger, ulong guild);

    [LoggerMessage(212, LogLevel.Warning, "Could not find AzuraCast item for guild {guild}")]
    public static partial void DatabaseAzuraCastNotFound(this ILogger logger, ulong guild);

    [LoggerMessage(213, LogLevel.Warning, "Could not find AzuraCast checks for guild {guild} in instance {instance}")]
    public static partial void DatabaseAzuraCastChecksNotFound(this ILogger logger, ulong guild, int instance);

    [LoggerMessage(214, LogLevel.Warning, "Could not find AzuraCast preferences for guild {guildId} in instance {instance}")]
    public static partial void DatabaseAzuraCastPreferencesNotFound(this ILogger logger, ulong guildId, int instance);

    [LoggerMessage(215, LogLevel.Warning, "Could not find AzuraCast station {station} for guild {guild} in instance {instance}")]
    public static partial void DatabaseAzuraCastStationNotFound(this ILogger logger, ulong guild, int instance, int station);

    [LoggerMessage(216, LogLevel.Warning, "Could not find AzuraCast station checks for guild {guild} in instance {instance} at station {station}")]
    public static partial void DatabaseAzuraCastStationChecksNotFound(this ILogger logger, ulong guild, int instance, int station);

    [LoggerMessage(217, LogLevel.Warning, "Could not find AzuraCast station preferences for guild {guild} in instance {instance} at station {station}")]
    public static partial void DatabaseAzuraCastStationPreferencesNotFound(this ILogger logger, ulong guild, int instance, int station);

    [LoggerMessage(220, LogLevel.Warning, "Could not find discord item {item} for guild {guild}")]
    public static partial void DiscordItemNotFound(this ILogger logger, string item, ulong guild);

    [LoggerMessage(221, LogLevel.Warning, "Could not find referenced command {command}")]
    public static partial void CommandNotFound(this ILogger logger, string command);

    [LoggerMessage(230, LogLevel.Warning, "Bot is ratelimited on uri: {uri} retrying in {time} seconds")]
    public static partial void BotRatelimited(this ILogger logger, Uri uri, int time);

    [LoggerMessage(290, LogLevel.Warning, "Latest online version of the bot is empty")]
    public static partial void OnlineVersionEmpty(this ILogger logger);

    [LoggerMessage(291, LogLevel.Warning, "Body of online version could not be deserialized")]
    public static partial void OnlineVersionUnserializable(this ILogger logger);

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

    [LoggerMessage(400, LogLevel.Critical, "An exception occured: ")]
    public static partial void ExceptionOccured(this ILogger logger, Exception ex);

    [LoggerMessage(401, LogLevel.Critical, "A gateway error occured: ")]
    public static partial void GatewayException(this ILogger logger, Exception ex);

    [LoggerMessage(402, LogLevel.Critical, "I'm not inside the server with the id {id} - please invite me to my hometown or I won't start!")]
    public static partial void NotInHomeGuild(this ILogger logger, ulong id);

    [LoggerMessage(403, LogLevel.Critical, "You removed me from my hometown server with the id {id}! I'm going to shutdown now.")]
    public static partial void RemovedFromHomeGuild(this ILogger logger, ulong id);

    [LoggerMessage(410, LogLevel.Critical, "The given settings can't be parsed, are they filled out?")]
    public static partial void UnableToParseSettings(this ILogger logger);

    [LoggerMessage(411, LogLevel.Critical, "The given BotToken is either missing or invalid")]
    public static partial void BotTokenInvalid(this ILogger logger);
}
