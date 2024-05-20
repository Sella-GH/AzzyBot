using System.Diagnostics.CodeAnalysis;

namespace AzzyBot.Database.Entities;

[SuppressMessage("Roslynator", "RCS0036:Remove blank line between single-line declarations of same kind", Justification = "Better clarification on ForeignKeys")]
public sealed class AzuraCastMountsEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Mount { get; set; } = string.Empty;

    public int AzuraCastId { get; set; }
    public AzuraCastEntity AzuraCast { get; set; } = null!;
}
