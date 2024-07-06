using System.Text.Json.Serialization;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcSongRequestsQueueModel
{
    [JsonPropertyName("timestamp")]
    public int Timestamp { get; set; }

    [JsonPropertyName("track")]
    public Track Track { get; set; } = new();
}

internal sealed class Track
{
    [JsonPropertyName("song_id")]
    public string Song_Id { get; set; } = string.Empty;
}
