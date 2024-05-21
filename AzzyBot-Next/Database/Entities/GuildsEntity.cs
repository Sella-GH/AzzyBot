using System.Collections.Generic;

namespace AzzyBot.Database.Entities;

public sealed class GuildsEntity
{
    public int Id { get; set; }
    public ulong UniqueId { get; set; }
    public ulong ErrorChannelId { get; set; }
    public bool IsDebugAllowed { get; set; }
    public bool ConfigSet { get; set; }
    public ICollection<AzuraCastEntity> AzuraCastStations { get; } = new List<AzuraCastEntity>();
}
