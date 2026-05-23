using System;
using System.Net.Http;

using AzzyBot.Bot.Commands;
using AzzyBot.Bot.Commands.Checks;
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Services.CronJobs;
using AzzyBot.Bot.Services.DiscordEvents;
using AzzyBot.Bot.Services.Modules;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Logging;

public static partial class LoggerActions
{
#if DEBUG || DOCKER_DEBUG
    [LoggerMessage(LogLevel.Debug, "Cluster logging test interval {i}")]
    public static partial void ClusterLoggingTest(this ILogger<DebugCommands.DebugGroup> logger, int i);
#endif

    [LoggerMessage(LogLevel.Debug, "Starting logfile cleanup")]
    public static partial void LogfileCleanupStart(this ILogger<LogfileCleaningJob> logger);

    [LoggerMessage(LogLevel.Debug, "Logfile cleanup completed, {count} logfiles deleted")]
    public static partial void LogfileCleanupComplete(this ILogger<LogfileCleaningJob> logger, int count);

    [LoggerMessage(LogLevel.Debug, "Notified guild {guildId} in channel {channelId} about deletion")]
    public static partial void BackgroundServiceGuildDeletionNotified(this ILogger<CoreService> logger, ulong guildId, ulong channelId);

    [LoggerMessage(LogLevel.Debug, "Notified owner of guild {guildId} about deletion")]
    public static partial void BackgroundServiceGuildDeletionNotifiedOwner(this ILogger<CoreService> logger, ulong guildId);

    [LoggerMessage(LogLevel.Debug, "Notified guild {guildId} in channel {channelId} about unused guild")]
    public static partial void BackgroundServiceGuildUnusedNotified(this ILogger<CoreService> logger, ulong guildId, ulong channelId);

    [LoggerMessage(LogLevel.Debug, "Notified owner of guild {guildId} about unused guild")]
    public static partial void BackgroundServiceGuildUnusedNotifiedOwner(this ILogger<CoreService> logger, ulong guildId);

    [LoggerMessage(LogLevel.Debug, "Creating work items for: {item}")]
    public static partial void BackgroundServiceWorkItem(this ILogger<AzuraRequestJob> logger, string item);

    [LoggerMessage(LogLevel.Debug, "Station {gId}-{iId}-{dId}-{sId} has different files")]
    public static partial void BackgroundServiceStationFilesChanged(this ILogger<AzuraCastFileService> logger, int gId, int iId, int dId, int sId);

    [LoggerMessage(LogLevel.Debug, "Instance {gId}-{iId} is {status}")]
    public static partial void BackgroundServiceInstanceStatus(this ILogger<AzuraCastPingService> logger, int gId, int iId, string status);

    [LoggerMessage(LogLevel.Debug, "Song request {rId} from {gId}-{iId}-{dId}-{sId} is waiting {time} seconds")]
    public static partial void BackgroundServiceSongRequestWaiting(this ILogger<AzuraRequestJob> logger, string rId, int gId, int iId, int dId, int sId, int time);

    [LoggerMessage(LogLevel.Debug, "Song request {rId} from {gId}-{iId}-{dId}-{sId} requeued")]
    public static partial void BackgroundServiceSongRequestRequeued(this ILogger<AzuraRequestJob> logger, string rId, int gId, int iId, int dId, int sId);

    [LoggerMessage(LogLevel.Debug, "Song request {rId} from {gId}-{iId}-{dId}-{sId} finished")]
    public static partial void BackgroundServiceSongRequestFinished(this ILogger<AzuraRequestJob> logger, string rId, int gId, int iId, int dId, int sId);

    [LoggerMessage(LogLevel.Debug, "AzuraCastDiscordPermission is {perm}")]
    public static partial void AzuraCastDiscordPermission(this ILogger<AzuraCastDiscordPermCheck> logger, string perm);

    [LoggerMessage(LogLevel.Debug, "User {user} is not connected to a voice channel")]
    public static partial void UserNotConnected(this ILogger<MusicStreamingService> logger, string user);

    [LoggerMessage(LogLevel.Debug, "Setting channelId to 0 because user is not connected to a voice channel")]
    public static partial void UserNotConnectedSetChannelId(this ILogger<MusicStreamingService> logger);

    [LoggerMessage(LogLevel.Debug, "Expected failure of a {type} request to {uri} with exception name {ex}")]
    public static partial void WebRequestExpectedFailure(this ILogger logger, HttpMethod type, Uri uri, string ex);

    [LoggerMessage(LogLevel.Information, "Starting {name} in version {version} on {os}-{arch} using .NET {dotnet}")]
    public static partial void BotStarting(this ILogger<CoreServiceHost> logger, string name, string version, string os, string arch, string dotnet);

    [LoggerMessage(LogLevel.Information, "Invite me using the following url: {invite}")]
    public static partial void InviteUrl(this ILogger<DiscordBotServiceHost> logger, string invite);

    [LoggerMessage(LogLevel.Information, "Command {command} requested by {user} to execute")]
    public static partial void CommandRequested(this ILogger logger, string command, string user);

    [LoggerMessage(LogLevel.Information, "Commands error is: ")]
    public static partial void CommandsErrorType(this ILogger<DiscordCommandsErrorHandler> logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "Starting Database Reencryption")]
    public static partial void DatabaseReencryptionStart(this ILogger<CoreServiceHost> logger);

    [LoggerMessage(LogLevel.Information, "Database Reencryption completed")]
    public static partial void DatabaseReencryptionComplete(this ILogger<CoreServiceHost> logger);

    [LoggerMessage(LogLevel.Information, "Starting database cleanup of unused guilds.")]
    public static partial void DatabaseUnusedGuildsStart(this ILogger<AzzyBotInactiveGuildJob> logger);

    [LoggerMessage(LogLevel.Information, "Database cleanup of {unused} unused guilds completed, {notified} guilds were notified, {deleted} were deleted.")]
    public static partial void DatabaseUnusedGuildsComplete(this ILogger<AzzyBotInactiveGuildJob> logger, int unused, int notified, int deleted);

    [LoggerMessage(LogLevel.Information, "AzzyBot joined the following Guild: {guild}")]
    public static partial void GuildCreated(this ILogger<DiscordGuildsHandler> logger, string guild);

    [LoggerMessage(LogLevel.Information, "AzzyBot was removed from the following Guild: {guild}")]
    public static partial void GuildDeleted(this ILogger<DiscordGuildsHandler> logger, string guild);

    [LoggerMessage(LogLevel.Information, "The following guild is unavailable due to an outage: {guild}")]
    public static partial void GuildUnavailable(this ILogger<DiscordGuildsHandler> logger, string guild);

    [LoggerMessage(LogLevel.Information, "Sent the bot-wide info message to {count} guilds")]
    public static partial void BotWideMessageSent(this ILogger<AdminCommands.AdminGroup> logger, int count);

    [LoggerMessage(LogLevel.Information, "Bot status activity type set to {activityType} with doing '{doing}'")]
    public static partial void BotStatusActivitySet(this ILogger<DiscordBotService> logger, string activityType, string doing);

    [LoggerMessage(LogLevel.Information, "Bot status stream URL set to {url}")]
    public static partial void BotStatusStreamUrlSet(this ILogger<DiscordBotService> logger, string url);

    [LoggerMessage(LogLevel.Information, "Bot status user status set to {userStatus}")]
    public static partial void BotStatusUserStatusSet(this ILogger<DiscordBotService> logger, string userStatus);

    [LoggerMessage(LogLevel.Information, "An update for Azzy is available! Please update now to version: {version} to get the latest fixes and improvements.")]
    public static partial void UpdateAvailable(this ILogger<UpdaterService> logger, string version);

    [LoggerMessage(LogLevel.Information, "Stopping AzzyBot")]
    public static partial void BotStopping(this ILogger<CoreServiceHost> logger);

    [LoggerMessage(LogLevel.Warning, "AzzyBot is not connected to Discord!")]
    public static partial void BotNotConnected(this ILogger<DiscordBotService> logger);

    [LoggerMessage(LogLevel.Warning, "Commands error occurred!")]
    public static partial void CommandsError(this ILogger<DiscordCommandsErrorHandler> logger);

    [LoggerMessage(LogLevel.Warning, "Could not fetch channel for id {id}")]
    public static partial void ChannelNotFound(this ILogger<DiscordBotService> logger, ulong id);

    [LoggerMessage(LogLevel.Warning, "Could not fetch message for id {id} in channel {cid} in guild {gid}")]
    public static partial void MessageNotFound(this ILogger logger, ulong id, ulong cid, ulong gid);

    [LoggerMessage(LogLevel.Warning, "Too many Embeds for SendMessageAsync action. Expected only 10 but got {embeds}. Only the first 10 are added and the others discarded!")]
    public static partial void TooManyEmbeds(this ILogger<DiscordBotService> logger, int embeds);

    [LoggerMessage(LogLevel.Warning, "Could not find discord item {item} for guild {guild}")]
    public static partial void DiscordItemNotFound(this ILogger logger, string item, ulong guild);

    [LoggerMessage(LogLevel.Warning, "Could not find referenced command {command}")]
    public static partial void CommandNotFound(this ILogger<AzuraCastDiscordPermCheck> logger, string command);

    [LoggerMessage(LogLevel.Warning, "Bot status activity type {type} is not defined, falling back to ListeningTo")]
    public static partial void BotStatusActivityTypeNotDefined(this ILogger<DiscordBotService> logger, int type);

    [LoggerMessage(LogLevel.Warning, "Bot status activity type Streaming requires a URL, falling back to Playing")]
    public static partial void BotStatusStreamingRequiresUrl(this ILogger<DiscordBotService> logger);

    [LoggerMessage(LogLevel.Warning, "Bot status user status {status} is not defined, falling back to Online")]
    public static partial void BotStatusUserStatusNotDefined(this ILogger<DiscordBotService> logger, int status);

    [LoggerMessage(LogLevel.Warning, "Bot status activity type Streaming requires a Twitch or YouTube URL, falling back to Playing")]
    public static partial void BotStatusStreamingInvalidUrl(this ILogger<DiscordBotService> logger);

    [LoggerMessage(LogLevel.Warning, "Unable to send message to default channel in guild {guildId} to notify about creation.")]
    public static partial void UnableToNotifyGuildCreated(this ILogger<DiscordGuildsHandler> logger, ulong guildId);

    [LoggerMessage(LogLevel.Warning, "Bot is ratelimited on uri: {uri} retrying in {time} seconds")]
    public static partial void BotRatelimited(this ILogger<WebRequestService> logger, Uri uri, int time);

    [LoggerMessage(LogLevel.Warning, "Unable to notify admins or owner of guild {guildName} ({guildId}) about leaving")]
    public static partial void UnableToNotifyUnusedGuildDeleted(this ILogger<CoreService> logger, string guildName, ulong guildId);

    [LoggerMessage(LogLevel.Warning, "Unable to notify admins or owner of guild {guildName} ({guildId}) about being unused")]
    public static partial void UnableToNotifyUnusedGuildUnused(this ILogger<CoreService> logger, string guildName, ulong guildId);

    [LoggerMessage(LogLevel.Warning, "Could not find local file for station {station} (db: {dId}) in instance {instance} in guild {guild}")]
    public static partial void LocalFileNotFound(this ILogger<AzuraCastApiService> logger, int station, int dId, int instance, int guild);

    [LoggerMessage(LogLevel.Warning, "Could not fetch local file content for station {station} (db: {dId}) in instance {instance} in guild {guild}")]
    public static partial void LocalFileContentNotFound(this ILogger<AzuraCastApiService> logger, int station, int dId, int instance, int guild);

    [LoggerMessage(LogLevel.Warning, "Latest online version of the bot is empty")]
    public static partial void OnlineVersionEmpty(this ILogger<UpdaterService> logger);

    [LoggerMessage(LogLevel.Warning, "Body of online version could not be deserialized")]
    public static partial void OnlineVersionUnserializable(this ILogger<UpdaterService> logger);

    [LoggerMessage(LogLevel.Error, "An error occurred while logging the exception to discord: {ex}")]
    public static partial void UnableToLogException(this ILogger<DiscordBotService> logger, string ex);

    [LoggerMessage(LogLevel.Error, "An error occurred while sending a message to discord: {ex}")]
    public static partial void UnableToSendMessage(this ILogger<DiscordBotService> logger, string ex);

    [LoggerMessage(LogLevel.Error, "The provided uri is invalid: {uri}")]
    public static partial void WebInvalidUri(this ILogger<WebRequestService> logger, Uri uri);

    [LoggerMessage(LogLevel.Error, "The {type} request to {uri} failed with error: {ex}")]
    public static partial void WebRequestFailed(this ILogger<WebRequestService> logger, HttpMethod type, string ex, Uri uri);

    [LoggerMessage(LogLevel.Critical, "An exception occurred: ")]
    public static partial void ExceptionOccurred(this ILogger<DiscordBotService> logger, Exception ex);

    [LoggerMessage(LogLevel.Critical, "I'm not inside the server with the id {id} - please invite me to my hometown or I won't start!")]
    public static partial void NotInHomeGuild(this ILogger<DiscordGuildsHandler> logger, ulong id);

    [LoggerMessage(LogLevel.Critical, "You removed me from my hometown server with the id {id}! I'm going to shutdown now.")]
    public static partial void RemovedFromHomeGuild(this ILogger<DiscordGuildsHandler> logger, ulong id);
}
