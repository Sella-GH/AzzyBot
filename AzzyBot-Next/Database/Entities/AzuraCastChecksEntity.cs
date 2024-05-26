using System.Diagnostics.CodeAnalysis;

namespace AzzyBot.Database.Entities;

[SuppressMessage("Roslynator", "RCS0036:Remove blank line between single-line declarations of same kind", Justification = "Better clarification on keys")]
public sealed class AzuraCastChecksEntity
{
    public int Id { get; set; }

    public bool FileChanges { get; set; }
    public bool ServerStatus { get; set; }
    public bool Updates { get; set; }
    public bool UpdatesShowChangelog { get; set; }

    public int StationId { get; set; }
    public AzuraCastStationEntity Station { get; set; } = null!;
}
