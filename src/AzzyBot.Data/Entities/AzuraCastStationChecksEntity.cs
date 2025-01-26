using System;
using System.ComponentModel.DataAnnotations;

namespace AzzyBot.Data.Entities;

/// <summary>
/// Represents a station checks entity of an AzuraCast instance.
/// </summary>
public sealed class AzuraCastStationChecksEntity
{
    /// <summary>
    /// The database id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The state of the check if files have been changed.
    /// </summary>
    /// <remarks>
    /// This also enables file caching.
    /// </remarks>
    public bool FileChanges { get; set; }

    /// <summary>
    /// The <see cref="DateTimeOffset"/> of the last file changes check.
    /// </summary>
    /// <remarks>
    /// Always use <see cref="DateTimeOffset.UtcNow"/> to set this value.
    /// </remarks>
    public DateTimeOffset LastFileChangesCheck { get; set; }

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
