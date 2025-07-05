using System;
using System.ComponentModel.DataAnnotations;

using DSharpPlus.Entities;

namespace AzzyBot.Data.Entities;

/// <summary>
/// The guild database entity.
/// </summary>
public sealed class GuildEntity
{
    /// <summary>
    /// The database id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The <see cref="DiscordGuild"/> id.
    /// </summary>
    public ulong UniqueId { get; set; }

    /// <summary>
    /// The state if the core config was set.
    /// </summary>
    public bool ConfigSet { get; set; }

    /// <summary>
    /// The state if the guild accepted the legal terms.
    /// </summary>
    public bool LegalsAccepted { get; set; }

    /// <summary>
    /// The last saved <see cref="DateTimeOffset"/> timestamp when the <see cref="DiscordGuild"/> was checked for correct channel permissions.
    /// </summary>
    /// <remarks>
    /// Always use <see cref="DateTimeOffset.UtcNow"/> to set this value.
    /// </remarks>
    public DateTimeOffset LastPermissionCheck { get; set; }

    /// <summary>
    /// The concurrency token for optimistic concurrency checks.
    /// </summary>
    [Timestamp]
    public uint Version { get; set; }

    /// <summary>
    /// The user-defined preferences of the <see cref="GuildEntity"/> object.
    /// </summary>
    public GuildPreferencesEntity Preferences { get; set; } = new();

    /// <summary>
    /// The possible <see cref="AzuraCastEntity"/> database item.
    /// </summary>
    /// <remarks>
    /// This can be null if this <see cref="DiscordGuild"/> does not utilize AzuraCast.
    /// </remarks>
    public AzuraCastEntity? AzuraCast { get; set; }

    /// <summary>
    /// The possible <see cref="MusicStreamingEntity"/> database item.
    /// </summary>
    /// <remarks>
    /// This can be null if this <see cref="DiscordGuild"/> does not utilize the music streaming service.
    /// </remarks>
    public MusicStreamingEntity? MusicStreaming { get; set; }
}
