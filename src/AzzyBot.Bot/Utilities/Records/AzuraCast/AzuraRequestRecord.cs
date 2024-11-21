using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

/// <summary>
/// Represents the data for a song request.
/// </summary>
public sealed record AzuraRequestRecord
{
    /// <summary>
    /// Requestable ID unique identifier
    /// </summary>
    [JsonPropertyName("request_id")]
    public required string RequestId { get; set; }

    /// <summary>
    /// The song that is attached to the identifier.
    /// </summary>
    [JsonPropertyName("song")]
    public required AzuraSongDataRecord Song { get; set; }
}

/// <summary>
/// Represents a song request inside the queue.
/// </summary>
public sealed record AzuraRequestQueueItemRecord
{
    /// <summary>
    /// The timestamp of when the song was requested.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public required int Timestamp { get; init; }

    /// <summary>
    /// The id of the request in the queue.
    /// </summary>
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    /// <summary>
    /// A portion of data connected to the song requested.
    /// </summary>
    [JsonPropertyName("track")]
    public required AzuraTrackRecord Track { get; init; }
}
