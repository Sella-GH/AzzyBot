using System;
using System.Collections.Generic;
using DSharpPlus.Entities;

namespace AzzyBot.Database.Entities;

public sealed class AzuraCastStationEntity
{
    /// <summary>
    /// The database id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The AzuraCast internal station id.
    /// </summary>
    public int StationId { get; set; }

    /// <summary>
    /// The name of the AzuraCast station.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The api key of the station.
    /// </summary>
    /// <remarks>
    /// Can be empty if no key was provided. Then the administrative key of the parenting <see cref="AzuraCastEntity"/> database item is used.
    /// </remarks>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The <see cref="DiscordRole"/> id of the <see cref="DiscordRole"/> with administrative permissions on the station.
    /// </summary>
    public ulong StationAdminRoleId { get; set; }

    /// <summary>
    /// The <see cref="DiscordRole"/> id of the <see cref="DiscordRole"/> with djing permissions on the station.
    /// </summary>
    public ulong StationDjRoleId { get; set; }

    /// <summary>
    /// The associated <see cref="AzuraCastStationChecksEntity"/> database item of the station.
    /// </summary>
    public AzuraCastStationChecksEntity Checks { get; set; } = new();

    /// <summary>
    /// A <see cref="ICollection<>"/> of associated <see cref="AzuraCastStationMountEntity"/> database items.
    /// </summary>
    public ICollection<AzuraCastStationMountEntity> Mounts { get; } = [];

    /// <summary>
    /// The <see cref="DiscordChannel"/> id of the <see cref="DiscordChannel"/> to which not-available music-requests should be sent.
    /// </summary>
    public ulong RequestsChannelId { get; set; }

    /// <summary>
    /// The state if HLS streams should be prefered when listening to this station.
    /// </summary>
    public bool PreferHls { get; set; }

    /// <summary>
    /// The state if the name of the playlist should be shown in the NowPlaying embed.
    /// </summary>
    public bool ShowPlaylistInNowPlaying { get; set; }

    /// <summary>
    /// The last saved <see cref="DateTime"/> timestamp after a song was skipped.
    /// </summary>
    public DateTime LastSkipTime { get; set; }

    /// <summary>
    /// The database id of the parenting <see cref="AzuraCastEntity"/> database item.
    /// </summary>
    public int AzuraCastId { get; set; }

    /// <summary>
    /// The parenting <see cref="AzuraCastEntity"/> database item.
    /// </summary>
    public AzuraCastEntity AzuraCast { get; set; } = null!;
}
