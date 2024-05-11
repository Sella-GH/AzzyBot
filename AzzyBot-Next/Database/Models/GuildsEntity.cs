namespace AzzyBot.Database.Models;

internal class GuildsEntity
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }

    public AzuraCastEntity? AzuraCast { get; set; }
}
