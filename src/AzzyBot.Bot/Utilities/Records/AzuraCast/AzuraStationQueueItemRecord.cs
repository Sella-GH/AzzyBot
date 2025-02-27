using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

/// <summary>
/// Represents a queue item from AzuraCast.
/// </summary>
public record AzuraStationQueueItemRecord
{
    /// <summary>
    /// UNIX timestamp when playback is expected to start.
    /// </summary>
    [JsonPropertyName("played_at")]
    public required int PlayedAt { get; init; }

    /// <summary>
    /// Duration of the song in seconds
    /// </summary>
    [JsonPropertyName("duration")]
    public required double Duration { get; init; }

    /// <summary>
    /// Indicates the playlist that the song was played from, if available, or empty string if not.
    /// </summary>
    [JsonPropertyName("playlist")]
    public string Playlist { get; init; } = string.Empty;

    /// <summary>
    /// Indicates whether the song is a listener request.
    /// </summary>
    [JsonPropertyName("is_request")]
    public required bool IsRequest { get; init; }

    /// <summary>
    /// The full data for the song that was played.
    /// </summary>
    [JsonPropertyName("song")]
    public required AzuraSongDataRecord Song { get; init; }
}

/// <summary>
/// Represents a detailed queue item from AzuraCast.
/// </summary>
public sealed record AzuraStationQueueItemDetailedRecord : AzuraStationQueueItemRecord
{
    /// <summary>
    /// UNIX timestamp when the AutoDJ is expected to queue the song for playback.
    /// </summary>
    [JsonPropertyName("cued_at")]
    public required int CuedAt { get; init; }
}
