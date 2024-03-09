using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class NowPlayingData
{
    [JsonProperty("now_playing")]
    public NowPlaying Now_Playing { get; set; } = new();

    [JsonProperty("listeners")]
    public Listeners Listeners { get; set; } = new();

    [JsonProperty("live")]
    public Live Live { get; set; } = new();
}

internal sealed class Listeners
{
    [JsonProperty("unique")]
    public int Current { get; set; }
}

internal sealed class Live
{
    [JsonProperty("is_live")]
    public bool Is_live { get; set; }

    [JsonProperty("streamer_name")]
    public string Streamer_Name { get; set; } = string.Empty;

    [JsonProperty("broadcast_start")]
    public int? Broadcast_Start { get; set; }

    [JsonProperty("broadcast_end")]
    public string Art { get; set; } = string.Empty;
}

internal sealed class NowPlaying : SongData
{
    [JsonProperty("elapsed")]
    public int Elapsed { get; set; }
}
