using System.Diagnostics.CodeAnalysis;

namespace AzzyBot.Database.Models;

[SuppressMessage("Roslynator", "RCS0036:Remove blank line between single-line declarations of same kind", Justification = "Better clarification on ForeignKeys")]
internal sealed class GuildsEntity
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }

    public AzuraCastEntity? AzuraCast { get; set; }
}
