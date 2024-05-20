using System.Diagnostics.CodeAnalysis;

namespace AzzyBot.Database.Entities;

[SuppressMessage("Roslynator", "RCS0036:Remove blank line between single-line declarations of same kind", Justification = "Better clarification on ForeignKeys")]
public sealed class GuildsEntity
{
    public int Id { get; set; }
    public ulong UniqueId { get; set; }
    public ulong ErrorChannelId { get; set; }
    public bool IsDebugAllowed { get; set; }
    public bool ConfigSet { get; set; }

    public AzuraCastEntity? AzuraCast { get; set; }
}
