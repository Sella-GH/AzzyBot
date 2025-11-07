using System.ComponentModel.DataAnnotations;

using DSharpPlus.Entities;

namespace AzzyBot.Data.Entities;

/// <summary>
/// Represents the user-defined preferences of the AzuraCast station.
/// </summary>
public sealed class AzuraCastStationPreferencesEntity
{
    /// <summary>
    /// The database id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The <see cref="DiscordChannel"/> id of the <see cref="DiscordChannel"/> in which users are able to upload files to the station.
    /// </summary>
    public ulong FileUploadChannelId { get; set; }

    /// <summary>
    /// The path where uploaded files are stored on the AzuraCast station.
    /// </summary>
    public string FileUploadPath { get; set; } = string.Empty;

    /// <summary>
    /// The <see cref="DiscordChannel"/> id of the <see cref="DiscordChannel"/> in which the persistent NowPlaying embed should be sent.
    /// </summary>
    public ulong NowPlayingEmbedChannelId { get; set; }

    /// <summary>
    /// The <see cref="DiscordMessage"/> id of the persistent NowPlaying embed message.
    /// </summary>
    public ulong NowPlayingEmbedMessageId { get; set; }

    /// <summary>
    /// The <see cref="DiscordChannel"/> id of the <see cref="DiscordChannel"/> to which not-available music-requests should be sent.
    /// </summary>
    public ulong RequestsChannelId { get; set; }

    /// <summary>
    /// The state if the name of the playlist should be shown in the NowPlaying embed.
    /// </summary>
    public bool ShowPlaylistInNowPlaying { get; set; }

    /// <summary>
    /// The <see cref="DiscordRole"/> id of the <see cref="DiscordRole"/> with administrative permissions on the station.
    /// </summary>
    public ulong StationAdminRoleId { get; set; }

    /// <summary>
    /// The <see cref="DiscordRole"/> id of the <see cref="DiscordRole"/> with djing permissions on the station.
    /// </summary>
    public ulong StationDjRoleId { get; set; }

    /// <summary>
    /// The concurrency token for optimistic concurrency checks.
    /// </summary>
    [Timestamp]
    public uint Version { get; set; }

    /// <summary>
    /// The database id of the parenting <see cref="AzuraCastStationEntity"/> database item.
    /// </summary>
    public int StationId { get; set; }

    /// <summary>
    /// The parenting <see cref="AzuraCastStationEntity"/> database item.
    /// </summary>
    public AzuraCastStationEntity Station { get; set; } = null!;
}
