using System;

namespace AzzyBot.Data.Entities;

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
    /// The last saved <see cref="DateTime"/> timestamp after a song was skipped.
    /// </summary>
    /// <remarks>
    /// Always use <see cref="DateTime.UtcNow"/> to set this value.
    /// </remarks>
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
