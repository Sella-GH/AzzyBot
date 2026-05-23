using System;

using AzzyBot.Data.Services;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Data.Logging;

public static partial class LoggerActions
{
    [LoggerMessage(LogLevel.Debug, "Entering DbConcurrency handler")]
    public static partial void DatabaseConcurrencyHandlerEnter(this ILogger<DbActions> logger);

    [LoggerMessage(LogLevel.Debug, "Current entry: {entry}")]
    public static partial void DatabaseConcurrencyEntry(this ILogger<DbActions> logger, string entry);

    [LoggerMessage(LogLevel.Debug, "Property: {property}, ClientValue: {clientValue}, DatabaseValue: {dbValue}")]
    public static partial void DatabaseConcurrencyValues(this ILogger<DbActions> logger, string property, object? clientValue, object? dbValue);

    [LoggerMessage(LogLevel.Debug, "Exiting DbConcurrency handler")]
    public static partial void DatabaseConcurrencyHandlerExit(this ILogger<DbActions> logger);

    [LoggerMessage(LogLevel.Information, "Starting database cleanup of orphaned guilds.")]
    public static partial void DatabaseOrphanedGuildsStart(this ILogger<DbMaintenance> logger);

    [LoggerMessage(LogLevel.Information, "Database cleanup of orphaned guilds completed, {count} guilds were deleted.")]
    public static partial void DatabaseOrphanedGuildsComplete(this ILogger<DbMaintenance> logger, int count);

    [LoggerMessage(LogLevel.Information, "Database concurrency situation resolved.")]
    public static partial void DatabaseConcurrencyResolved(this ILogger<DbActions> logger);

    [LoggerMessage(LogLevel.Warning, "Could not find AzzyBot item")]
    public static partial void DatabaseAzzyBotNotFound(this ILogger<DbActions> logger);

    [LoggerMessage(LogLevel.Warning, "Could not find Guild item for guild {guild}")]
    public static partial void DatabaseGuildNotFound(this ILogger logger, ulong guild);

    [LoggerMessage(LogLevel.Warning, "Could not find Guild preferences for guild {guild}")]
    public static partial void DatabaseGuildPreferencesNotFound(this ILogger<DbActions> logger, ulong guild);

    [LoggerMessage(LogLevel.Warning, "Could not find AzuraCast item for guild {guild}")]
    public static partial void DatabaseAzuraCastNotFound(this ILogger logger, ulong guild);

    [LoggerMessage(LogLevel.Warning, "Could not find AzuraCast checks for guild {guild} in instance {instance}")]
    public static partial void DatabaseAzuraCastChecksNotFound(this ILogger<DbActions> logger, ulong guild, int instance);

    [LoggerMessage(LogLevel.Warning, "Could not find AzuraCast preferences for guild {guildId} in instance {instance}")]
    public static partial void DatabaseAzuraCastPreferencesNotFound(this ILogger<DbActions> logger, ulong guildId, int instance);

    [LoggerMessage(LogLevel.Warning, "Could not find AzuraCast station {station} for guild {guild} in instance {instance}")]
    public static partial void DatabaseAzuraCastStationNotFound(this ILogger logger, ulong guild, int instance, int station);

    [LoggerMessage(LogLevel.Warning, "Could not find AzuraCast station checks for guild {guild} in instance {instance} at station {station}")]
    public static partial void DatabaseAzuraCastStationChecksNotFound(this ILogger<DbActions> logger, ulong guild, int instance, int station);

    [LoggerMessage(LogLevel.Warning, "Could not find AzuraCast station preferences for guild {guild} in instance {instance} at station {station}")]
    public static partial void DatabaseAzuraCastStationPreferencesNotFound(this ILogger logger, ulong guild, int instance, int station);

    [LoggerMessage(LogLevel.Warning, "Could not find MusicStreaming item for guild {guild}")]
    public static partial void DatabaseMusicStreamingNotFound(this ILogger logger, ulong guild);

    [LoggerMessage(LogLevel.Warning, "Database concurrency exception occurred: ")]
    public static partial void DatabaseConcurrencyException(this ILogger<DbActions> logger, Exception ex);
}
