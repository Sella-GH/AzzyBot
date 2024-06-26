using System.Text.Json.Serialization;

namespace AzzyBot;

public sealed record AzuraTrackRecord
{
    [JsonPropertyName("unique_id")]
    public required string UniqueId { get; init; }

    [JsonPropertyName("song_id")]
    public required string SongId { get; init; }
}
