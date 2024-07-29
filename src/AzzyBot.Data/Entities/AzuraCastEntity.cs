using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AzzyBot.Data.Entities;

[SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Entity Framework Core is unable to handle Uri")]
public sealed class AzuraCastEntity
{
    /// <summary>
    /// The database id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The base url of the AzuraCast instance.
    /// </summary>
    /// <remarks>
    /// This property is encrypted.
    /// </remarks>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The administrative api key of the AzuraCast instance.
    /// </summary>
    /// <remarks>
    /// This property is encrypted.
    /// </remarks>
    public string AdminApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The state if the station is online or offline.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// The associated <see cref="AzuraCastChecksEntity"/> of the instance.
    /// </summary>
    public AzuraCastChecksEntity Checks { get; set; } = new();

    /// <summary>
    /// The user-defined preferences of the <see cref="AzuraCastEntity"/> object.
    /// </summary>
    public AzuraCastPreferencesEntity Preferences { get; set; } = new();

    /// <summary>
    /// A <see cref="ICollection<>"/> of the associated <see cref="AzuraCastStationEntity"/> database items.
    /// </summary>
    public ICollection<AzuraCastStationEntity> Stations { get; } = [];

    /// <summary>
    /// The database id of the parenting <see cref="GuildEntity"/> database item.
    /// </summary>
    public int GuildId { get; set; }

    /// <summary>
    /// The parenting <see cref="GuildEntity"/> database item.
    /// </summary>
    public GuildEntity Guild { get; set; } = null!;
}
