using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records.AzuraCast;

public sealed record NowPlayingDataRecord
{
    [JsonPropertyName("now_playing")]
    public NowPlayingRecord NowPlaying { get; init; } = new();

    [JsonPropertyName("listeners")]
    public NowPlayingListenersRecord Listeners { get; init; } = new();

    [JsonPropertyName("live")]
    public NowPlayingLiveRecord Live { get; init; } = new();

    [JsonPropertyName("is_online")]
    public bool IsOnline { get; init; }
}

public sealed record NowPlayingListenersRecord
{
    [JsonPropertyName("unique")]
    public int Current { get; init; }
}

public sealed record NowPlayingLiveRecord
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

public sealed record NowPlayingRecord : SongDataRecord
{
    [JsonPropertyName("elapsed")]
    public int Elapsed { get; init; }
}
