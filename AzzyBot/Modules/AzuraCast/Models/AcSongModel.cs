using System.Text.Json.Serialization;

namespace AzzyBot.Modules.AzuraCast.Models;

internal class SongData
{
    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("playlist")]
    public string Playlist { get; set; } = string.Empty;

    [JsonPropertyName("song")]
    public SongDetailed Song { get; set; } = new();
}

internal sealed class SongDetailed : SongSimple
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("art")]
    public string Art { get; set; } = string.Empty;
}

internal class SongSimple
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("artist")]
    public string Artist { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("album")]
    public string Album { get; set; } = string.Empty;
}
