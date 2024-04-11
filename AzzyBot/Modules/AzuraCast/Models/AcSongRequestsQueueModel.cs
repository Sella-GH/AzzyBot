using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcSongRequestsQueueModel
{
    [JsonProperty("timestamp")]
    public int Timestamp { get; set; }

    [JsonProperty("track")]
    public Track Track { get; set; } = new();
}

internal sealed class Track
{
    [JsonProperty("song_id")]
    public string Song_Id { get; set; } = string.Empty;
}
