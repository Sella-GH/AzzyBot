using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Models.AzuraCast;

/// <summary>
/// Represents the data for the currently playing song on the station.
/// </summary>
public sealed record class AzuraNowPlayingDataModel
{
    /// <summary>
    /// The station that is currently playing music.
    /// </summary>
    [JsonPropertyName("station")]
    public required AzuraStationModel Station { get; init; }

    /// <summary>
    /// The current song that is playing.
    /// </summary>
    [JsonPropertyName("now_playing")]
    public required AzuraNowPlayingModel NowPlaying { get; init; }

    /// <summary>
    /// The listeners that are currently listening to the station.
    /// </summary>
    [JsonPropertyName("listeners")]
    public required AzuraNowPlayingListenersModel Listeners { get; init; }

    /// <summary>
    /// The live information for the station.
    /// </summary>
    [JsonPropertyName("live")]
    public required AzuraNowPlayingLiveModel Live { get; init; }

    /// <summary>
    /// Whether the stream is currently online.
    /// </summary>
    [JsonPropertyName("is_online")]
    public required bool IsOnline { get; init; }
}

/// <summary>
/// Represents the listeners that are currently listening to the station.
/// </summary>
public sealed record class AzuraNowPlayingListenersModel
{
    /// <summary>
    /// Total non-unique current listeners
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; init; }

    /// <summary>
    /// Total unique current listeners
    /// </summary>
    [JsonPropertyName("unique")]
    public int Unique { get; init; }
}

/// <summary>
/// Represents the live information for the station.
/// </summary>
public sealed record class AzuraNowPlayingLiveModel
{
    /// <summary>
    /// Whether the stream is known to currently have a live DJ.
    /// </summary>
    [JsonPropertyName("is_live")]
    public required bool IsLive { get; init; }

    /// <summary>
    /// The current active streamer/DJ, if one is available.
    /// </summary>
    [JsonPropertyName("streamer_name")]
    public string StreamerName { get; init; } = string.Empty;

    /// <summary>
    /// The start timestamp of the current broadcast, if one is available.
    /// </summary>
    [JsonPropertyName("broadcast_start")]
    public long? BroadcastStart { get; init; }

    /// <summary>
    /// URL to the streamer artwork (if available).
    /// </summary>
    [JsonPropertyName("art")]
    public string? Art { get; init; }
}

/// <summary>
/// Represents the currently playing song on the station.
/// </summary>
public sealed record class AzuraNowPlayingModel : AzuraStationQueueItemModel
{
    /// <summary>
    /// Elapsed time of the song's playback since it started.
    /// </summary>
    [JsonPropertyName("elapsed")]
    public required int Elapsed { get; init; }
}
