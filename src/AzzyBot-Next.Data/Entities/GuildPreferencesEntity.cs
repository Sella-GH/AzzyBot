using DSharpPlus.Entities;

namespace AzzyBot.Data.Entities;

public sealed class GuildPreferencesEntity
{
    /// <summary>
    /// The database id.
    /// </summary>
    public int Id { get; set; }

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
    /// The database id of the parenting <see cref="GuildEntity"/> database item.
    /// </summary>
    public int GuildId { get; set; }

    /// <summary>
    /// The parenting <see cref="GuildEntity"/> database item.
    /// </summary>
    public GuildEntity Guild { get; set; } = null!;
}
