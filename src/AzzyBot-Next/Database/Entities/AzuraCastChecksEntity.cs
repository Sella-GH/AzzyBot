namespace AzzyBot.Database.Entities;

public sealed class AzuraCastChecksEntity
{
    /// <summary>
    /// The database id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The state of the check if the instance is online.
    /// </summary>
    public bool ServerStatus { get; set; }

    /// <summary>
    /// The state of the check for instance updates.
    /// </summary>
    public bool Updates { get; set; }

    /// <summary>
    /// The state if the changelog should be added to the check for instance updates.
    /// </summary>
    public bool UpdatesShowChangelog { get; set; }

    /// <summary>
    /// The database id of the parenting <see cref="AzuraCastEntity"/> database item.
    /// </summary>
    public int AzuraCastId { get; set; }

    /// <summary>
    /// The parenting <see cref="AzuraCastEntity"/> database item.
    /// </summary>
    public AzuraCastEntity AzuraCast { get; set; } = null!;
}
