namespace AzzyBot.Data.Entities;

public sealed class AzuraCastStationMountEntity
{
    /// <summary>
    /// The database id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of the mount point.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The actual stub of the mount point.
    /// </summary>
    public string Mount { get; set; } = string.Empty;

    /// <summary>
    /// The database id of the parenting <see cref="AzuraCastStationEntity"/> database item.
    /// </summary>
    public int StationId { get; set; }

    /// <summary>
    /// The parenting <see cref="AzuraCastStationEntity"/> database item.
    /// </summary>
    public AzuraCastStationEntity Station { get; set; } = null!;
}
