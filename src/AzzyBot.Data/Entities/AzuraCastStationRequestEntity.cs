using System;
using System.ComponentModel.DataAnnotations;

namespace AzzyBot.Data.Entities;

/// <summary>
/// Represents a request for a song on a station.
/// </summary>
public sealed class AzuraCastStationRequestEntity
{
    /// <summary>
    /// The database id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The 32 character unique id of the song.
    /// </summary>
    public string SongId { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when the request was scheduled.
    /// </summary>
    /// <remarks>
    /// Always use <see cref="DateTimeOffset.UtcNow"/> to set this value.
    /// </remarks>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Whether this request was made internally to the AutoDj.
    /// </summary>
    public bool IsInternal { get; set; }

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
