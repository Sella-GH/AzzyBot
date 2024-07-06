using System.Text.Json.Serialization;
using AzzyBot.Utilities.Records.AzuraCast;

namespace AzzyBot;

public record AzuraStationQueueItemRecord
{
    [JsonPropertyName("played_at")]
    public required int PlayedAt { get; init; }

    [JsonPropertyName("duration")]
    public required int Duration { get; init; }

    [JsonPropertyName("playlist")]
    public string Playlist { get; init; } = string.Empty;

    [JsonPropertyName("is_request")]
    public required bool IsRequest { get; init; }

    [JsonPropertyName("song")]
    public required AzuraSongDataRecord Song { get; init; }
}

public sealed record AzuraStationQueueItemDetailedRecord : AzuraStationQueueItemRecord
{
    [JsonPropertyName("cued_at")]
    public required int CuedAt { get; init; }
}
