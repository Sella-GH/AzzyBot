using DSharpPlus.Entities;

namespace AzzyBot.Data.Entities;

public sealed class GuildEntity
{
    /// <summary>
    /// The database id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The <see cref="DiscordGuild"/> id.
    /// </summary>
    public ulong UniqueId { get; set; }

    /// <summary>
    /// The <see cref="DiscordRole"/> id of the administrative <see cref="DiscordRole"/> of the <see cref="DiscordGuild"/>.
    /// </summary>
    public ulong AdminRoleId { get; set; }

    /// <summary>
    /// The <see cref="DiscordChannel"/> id of the adminstrative <see cref="DiscordChannel"/>.
    /// </summary>
    public ulong AdminNotifyChannelId { get; set; }

    /// <summary>
    /// The <see cref="DiscordChannel"/> id of the <see cref="DiscordChannel"/> where errors are posted.
    /// </summary>
    public ulong ErrorChannelId { get; set; }

    /// <summary>
    /// The state if the core config was set.
    /// </summary>
    public bool ConfigSet { get; set; }

    /// <summary>
    /// The possible <see cref="AzuraCastEntity"/> database item.
    /// </summary>
    /// <remarks>
    /// This can be null if this <see cref="DiscordGuild"/> does not utilitize AzuraCast.
    /// </remarks>
    public AzuraCastEntity? AzuraCast { get; set; }
}
