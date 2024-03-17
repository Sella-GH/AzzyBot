using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal class SongData
{
    [JsonProperty("duration")]
    public int Duration { get; set; }

    [JsonProperty("playlist")]
    public string Playlist { get; set; } = string.Empty;

    [JsonProperty("song")]
    public SongDetailed Song { get; set; } = new();
}

internal sealed class SongDetailed : SongSimple
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("art")]
    public string Art { get; set; } = string.Empty;
}

internal class SongSimple
{
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    [JsonProperty("artist")]
    public string Artist { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("album")]
    public string Album { get; set; } = string.Empty;
}
