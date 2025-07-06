using System.ComponentModel.DataAnnotations;

using DSharpPlus.Entities;

namespace AzzyBot.Data.Entities;

public sealed class MusicStreamingEntity
{
    /// <summary>
    /// The database id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The <see cref="DiscordChannel"/> id of the <see cref="DiscordChannel"/> in which the persistent NowPlaying embed should be sent.
    /// </summary>
    public ulong NowPlayingEmbedChannelId { get; set; }

    /// <summary>
    /// The <see cref="DiscordMessage"/> id of the persistent NowPlaying embed message.
    /// </summary>
    public ulong NowPlayingEmbedMessageId { get; set; }

    /// <summary>
    /// The audio volume of the guild.
    /// </summary>
    public int Volume { get; set; }

    /// <summary>
    /// The concurrency token for optimistic concurrency checks.
    /// </summary>
    [Timestamp]
    public uint Version { get; set; }

    /// <summary>
    /// The database id of the parenting <see cref="GuildEntity"/> database item.
    /// </summary>
    public int GuildId { get; set; }

    /// <summary>
    /// The parenting <see cref="GuildEntity"/> database item.
    /// </summary>
    public GuildEntity Guild { get; set; } = null!;
}
