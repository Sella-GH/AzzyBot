using System;
using System.Text.Json.Serialization;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class SongHistory
{
    [JsonPropertyName("played_at")]
    public int PlayedAt { get; set; }

    [JsonPropertyName("playlist")]
    public string Playlist { get; set; } = string.Empty;

    [JsonPropertyName("streamer")]
    public string Streamer { get; set; } = string.Empty;

    [JsonPropertyName("is_request")]
    public bool IsRequest { get; set; }

    [JsonPropertyName("song")]
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
