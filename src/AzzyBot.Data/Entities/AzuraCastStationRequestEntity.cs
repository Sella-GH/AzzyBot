using System;

namespace AzzyBot.Data.Entities;

public sealed class AzuraCastStationRequestEntity
{
    /// <summary>
    /// The database id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The unique id of the song.
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
    /// The database id of the parenting <see cref="AzuraCastStationEntity"/> database item.
    /// </summary>
    public int StationId { get; set; }

    /// <summary>
    /// The parenting <see cref="AzuraCastStationEntity"/> database item.
    /// </summary>
    public AzuraCastStationEntity Station { get; set; } = null!;
}
