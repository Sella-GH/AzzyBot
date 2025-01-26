using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AzzyBot.Data.Entities;

/// <summary>
/// Represents a station of an AzuraCast instance.
/// </summary>
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
    /// The api key of the station.
    /// </summary>
    /// <remarks>
    /// Can be empty if no key was provided. Then the administrative key of the parenting <see cref="AzuraCastEntity"/> database item is used.
    /// This property is encrypted.
    /// </remarks>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The associated <see cref="AzuraCastStationChecksEntity"/> database item of the station.
    /// </summary>
    public AzuraCastStationChecksEntity Checks { get; set; } = new();

    /// <summary>
    /// The user-defined preferences of the <see cref="AzuraCastStationEntity"/> object.
    /// </summary>
    public AzuraCastStationPreferencesEntity Preferences { get; set; } = new();

    /// <summary>
    /// A list of the associated <see cref="AzuraCastStationRequestEntity"/> database items.
    /// </summary>
    public ICollection<AzuraCastStationRequestEntity> Requests { get; } = [];

    /// <summary>
    /// The last saved <see cref="DateTimeOffset"/> timestamp after a song was skipped.
    /// </summary>
    /// <remarks>
    /// Always use <see cref="DateTimeOffset.UtcNow"/> to set this value.
    /// </remarks>
    public DateTimeOffset LastSkipTime { get; set; }

    /// <summary>
    /// The last saved <see cref="DateTimeOffset"/> timestamp after a song was requested.
    /// </summary>
    /// <remarks>
    /// Always use <see cref="DateTimeOffset.UtcNow"/> to set this value.
    /// </remarks>
    public DateTimeOffset LastRequestTime { get; set; }

    /// <summary>
    /// The concurrency token for optimistic concurrency checks.
    /// </summary>
    [Timestamp]
    public uint Version { get; set; }

    /// <summary>
    /// The database id of the parenting <see cref="AzuraCastEntity"/> database item.
    /// </summary>
    public int AzuraCastId { get; set; }

    /// <summary>
    /// The parenting <see cref="AzuraCastEntity"/> database item.
    /// </summary>
    public AzuraCastEntity AzuraCast { get; set; } = null!;
}
