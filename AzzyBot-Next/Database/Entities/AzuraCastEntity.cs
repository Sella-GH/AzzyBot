using System.Diagnostics.CodeAnalysis;

namespace AzzyBot.Database.Entities;

[SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Entity Framework Core is unable to handle Uri")]
[SuppressMessage("Roslynator", "RCS0036:Remove blank line between single-line declarations of same kind", Justification = "Better clarification on ForeignKeys")]
public sealed class AzuraCastEntity
{
    public int Id { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;

    public int StationId { get; set; }
    public ulong MusicRequestsChannelId { get; set; }
    public ulong OutagesChannelId { get; set; }
    public bool ShowPlaylistInNowPlaying { get; set; }

    public AzuraCastChecksEntity? AutomaticChecks { get; set; }

    public int GuildId { get; set; }
    public GuildsEntity Guild { get; set; } = null!;
}
