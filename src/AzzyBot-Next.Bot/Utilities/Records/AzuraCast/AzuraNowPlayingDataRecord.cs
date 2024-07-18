using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraNowPlayingDataRecord
{
    [JsonPropertyName("now_playing")]
    public required AzuraNowPlayingRecord NowPlaying { get; init; }

    [JsonPropertyName("listeners")]
    public required AzuraNowPlayingListenersRecord Listeners { get; init; }

    [JsonPropertyName("live")]
    public required AzuraNowPlayingLiveRecord Live { get; init; }

    [JsonPropertyName("is_online")]
    public required bool IsOnline { get; init; }
}

public sealed record AzuraNowPlayingListenersRecord
{
    [JsonPropertyName("unique")]
    public int Current { get; init; }
}

public sealed record AzuraNowPlayingLiveRecord
{
    [JsonPropertyName("is_live")]
    public required bool IsLive { get; init; }

    [JsonPropertyName("streamer_name")]
    public string StreamerName { get; init; } = string.Empty;

    [JsonPropertyName("broadcast_start")]
    public int? BroadcastStart { get; init; }

    [JsonPropertyName("broadcast_end")]
    public string? Art { get; init; }
}

public sealed record AzuraNowPlayingRecord : AzuraStationQueueItemRecord
{
    [JsonPropertyName("elapsed")]
    public required int Elapsed { get; init; }
}
