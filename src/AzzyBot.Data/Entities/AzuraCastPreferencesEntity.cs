using DSharpPlus.Entities;

namespace AzzyBot.Data.Entities;

public sealed class AzuraCastPreferencesEntity
{
    /// <summary>
    /// The database id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The <see cref="DiscordRole"/> id of the administrative <see cref="DiscordRole"/> of the AzuraCast instance.
    /// </summary>
    public ulong InstanceAdminRoleId { get; set; }

    /// <summary>
    /// The <see cref="DiscordChannel"/> id of the <see cref="DiscordChannel"/> to which notifications should be posted.
    /// </summary>
    public ulong NotificationChannelId { get; set; }

    /// <summary>
    /// The <see cref="DiscordChannel"/> id of the <see cref="DiscordChannel"/> to which instance outages should be posted.
    /// </summary>
    public ulong OutagesChannelId { get; set; }

    /// <summary>
    /// The database id of the parenting <see cref="AzuraCastEntity"/> database item.
    /// </summary>
    public int AzuraCastId { get; set; }

    /// <summary>
    /// The parenting <see cref="AzuraCastEntity"/> database item.
    /// </summary>
    public AzuraCastEntity AzuraCast { get; set; } = null!;
}
