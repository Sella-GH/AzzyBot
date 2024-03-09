using System;
using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class SongHistory
{
    [JsonProperty("played_at")]
    public int PlayedAt { get; set; }

    [JsonProperty("playlist")]
    public string Playlist { get; set; } = string.Empty;

    [JsonProperty("streamer")]
    public string Streamer { get; set; } = string.Empty;

    [JsonProperty("is_request")]
    public bool IsRequest { get; set; }

    [JsonProperty("song")]
    public SongSimple Song { get; set; } = new();
}

internal sealed class SongExportHistory
{
    public TimeSpan PlayedAt { get; set; }
    public SongSimple Song { get; set; } = new();
    public bool SongRequest { get; set; }
    public string Playlist { get; set; } = string.Empty;
    public string Streamer { get; set; } = string.Empty;
}
