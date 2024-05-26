using System.Diagnostics.CodeAnalysis;

namespace AzzyBot.Database.Entities;

[SuppressMessage("Roslynator", "RCS0036:Remove blank line between single-line declarations of same kind", Justification = "Better clarification on keys")]
public sealed class AzuraCastMountEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Mount { get; set; } = string.Empty;

    public int StationId { get; set; }
    public AzuraCastStationEntity Station { get; set; } = null!;
}
