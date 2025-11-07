using System;
using System.ComponentModel.DataAnnotations;

namespace AzzyBot.Data.Entities;

/// <summary>
/// The base entity for AzzyBot.
/// </summary>
/// <remarks>
/// This entity contains information used by the bot.
/// </remarks>
public sealed class AzzyBotEntity
{
    /// <summary>
    /// The database id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The last saved <see cref="DateTimeOffset"/> timestamp when the database was cleaned up.
    /// </summary>
    /// <remarks>
    /// Always use <see cref="DateTimeOffset.UtcNow"/> to set this value.
    /// </remarks>
    public DateTimeOffset LastDatabaseCleanup { get; set; } = DateTimeOffset.MinValue;

    /// <summary>
    /// The last saved <see cref="DateTimeOffset"/> timestamp when the bot checked for inactive guilds.
    /// </summary>
    /// <remarks>
    /// Always use <see cref="DateTimeOffset.UtcNow"/> to set this value.
    /// </remarks>
    public DateTimeOffset LastGuildReminderCheck { get; set; } = DateTimeOffset.MinValue;

    /// <summary>
    /// The last saved <see cref="DateTimeOffset"/> timestamp when the bot checked for updates.
    /// </summary>
    /// <remarks>
    /// Always use <see cref="DateTimeOffset.UtcNow"/> to set this value.
    /// </remarks>
    public DateTimeOffset LastUpdateCheck { get; set; } = DateTimeOffset.MinValue;

    /// <summary>
    /// The concurrency token for optimistic concurrency checks.
    /// </summary>
    [Timestamp]
    public uint Version { get; set; }
}
