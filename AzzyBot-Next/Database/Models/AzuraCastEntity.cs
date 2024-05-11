namespace AzzyBot.Database.Models;

internal class AzuraCastEntity
{
    public int Id { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public int StationId { get; set; }
    public ulong MusicRequestsChannelId { get; set; }
    public ulong OutagesChannelId { get; set; }
    public bool ShowPlaylistInNowPlaying { get; set; }

    public int AzuraCastChecksId { get; set; }
    public virtual AzuraCastChecksEntity AutomaticChecks { get; set; } = new();

    public int GuildId { get; set; }
    public virtual GuildsEntity Guild { get; set; } = new();
}
