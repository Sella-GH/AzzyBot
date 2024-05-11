namespace AzzyBot.Database.Models;

internal class GuildsEntity
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }

    public int AzuraCastId { get; set; }
    public virtual AzuraCastEntity AzuraCast { get; set; } = new();
}
