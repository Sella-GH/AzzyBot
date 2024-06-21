using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records.AzuraCast;

public sealed record AzuraNowPlayingDataRecord
{
    [JsonPropertyName("now_playing")]
    public AzuraNowPlayingRecord NowPlaying { get; init; } = new();

    [JsonPropertyName("listeners")]
    public AzuraNowPlayingListenersRecord Listeners { get; init; } = new();

    [JsonPropertyName("live")]
    public AzuraNowPlayingLiveRecord Live { get; init; } = new();

    [JsonPropertyName("is_online")]
    public bool IsOnline { get; init; }
}

public sealed record AzuraNowPlayingListenersRecord
{
    [JsonPropertyName("unique")]
    public int Current { get; init; }
}

public sealed record AzuraNowPlayingLiveRecord
{
    [JsonPropertyName("is_live")]
    public bool IsLive { get; init; }

    [JsonPropertyName("streamer_name")]
    public string StreamerName { get; init; } = string.Empty;

    [JsonPropertyName("broadcast_start")]
    public int? BroadcastStart { get; init; }

    [JsonPropertyName("broadcast_end")]
    public string Art { get; init; } = string.Empty;
}

public sealed record AzuraNowPlayingRecord : AzuraSongDataRecord
{
    [JsonPropertyName("elapsed")]
    public int Elapsed { get; init; }
}
