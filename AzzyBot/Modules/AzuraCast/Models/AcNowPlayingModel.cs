using System.Text.Json.Serialization;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class NowPlayingData
{
    [JsonPropertyName("now_playing")]
    public NowPlaying Now_Playing { get; set; } = new();

    [JsonPropertyName("listeners")]
    public Listeners Listeners { get; set; } = new();

    [JsonPropertyName("live")]
    public Live Live { get; set; } = new();
}

internal sealed class Listeners
{
    [JsonPropertyName("unique")]
    public int Current { get; set; }
}

internal sealed class Live
{
    [JsonPropertyName("is_live")]
    public bool Is_live { get; set; }

    [JsonPropertyName("streamer_name")]
    public string Streamer_Name { get; set; } = string.Empty;

    [JsonPropertyName("broadcast_start")]
    public int? Broadcast_Start { get; set; }

    [JsonPropertyName("broadcast_end")]
    public string Art { get; set; } = string.Empty;
}

internal sealed class NowPlaying : SongData
{
    [JsonPropertyName("elapsed")]
    public int Elapsed { get; set; }
}
