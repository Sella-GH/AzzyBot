using System;
using System.ComponentModel.DataAnnotations;

namespace AzzyBot.Data.Entities;

/// <summary>
/// Represents the checks for an AzuraCast instance.
/// </summary>
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
    /// The counter how often the update notification was sent already.
    /// </summary>
    /// <remarks>
    /// This gets reset after no updates were found.
    /// </remarks>
    public int UpdateNotificationCounter { get; set; }

    /// <summary>
    /// The <see cref="DateTimeOffset"/> of the last server status check.
    /// </summary>
    /// <remarks>
    /// Always use <see cref="DateTimeOffset.UtcNow"/> to set this value.
    /// </remarks>
    public DateTimeOffset LastServerStatusCheck { get; set; }

    /// <summary>
    /// The <see cref="DateTimeOffset"/> of the last update check.
    /// </summary>
    /// <remarks>
    /// Always use <see cref="DateTimeOffset.UtcNow"/> to set this value.
    /// </remarks>
    public DateTimeOffset LastUpdateCheck { get; set; }

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
