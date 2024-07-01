using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AzzyBot.Database.Entities;

[SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Entity Framework Core is unable to handle Uri")]
[SuppressMessage("Roslynator", "RCS0036:Remove blank line between single-line declarations of same kind", Justification = "Better clarification on keys")]
public sealed class AzuraCastEntity
{
    public int Id { get; set; }

    public string BaseUrl { get; set; } = string.Empty;
    public string AdminApiKey { get; set; } = string.Empty;
    public string InstanceOwner { get; set; } = string.Empty;
    public ulong InstanceAdminGroup { get; set; }
    public ulong NotificationChannelId { get; set; }
    public ulong OutagesChannelId { get; set; }
    public bool IsOnline { get; set; }
    public AzuraCastChecksEntity Checks { get; set; } = new();
    public ICollection<AzuraCastStationEntity> Stations { get; } = new List<AzuraCastStationEntity>();

    public int? GuildId { get; set; }
    public GuildsEntity? Guild { get; set; }
}
