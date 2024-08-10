using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraRequestRecord
{
    [JsonPropertyName("request_id")]
    public required string RequestId { get; set; }

    [JsonPropertyName("song")]
    public required AzuraSongDataRecord Song { get; set; }
}

public sealed record AzuraRequestQueueItemRecord
{
    [JsonPropertyName("timestamp")]
    public required int Timestamp { get; init; }

    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("track")]
    public required AzuraTrackRecord Track { get; init; }
}
