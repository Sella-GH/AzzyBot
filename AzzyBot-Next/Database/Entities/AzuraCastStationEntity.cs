using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AzzyBot.Database.Entities;

[SuppressMessage("Roslynator", "RCS0036:Remove blank line between single-line declarations of same kind", Justification = "Better clarification on keys")]
public sealed class AzuraCastStationEntity
{
    public int Id { get; set; }

    public int StationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public AzuraCastChecksEntity Checks { get; set; } = new();
    public ICollection<AzuraCastMountEntity> Mounts { get; } = new List<AzuraCastMountEntity>();
    public ulong RequestsChannelId { get; set; }
    public ulong OutagesChannelId { get; set; }
    public bool PreferHls { get; set; }
    public bool ShowPlaylistInNowPlaying { get; set; }

    public int AzuraCastId { get; set; }
    public AzuraCastEntity AzuraCast { get; set; } = null!;
}
