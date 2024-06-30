using System;
using System.Text.Json.Serialization;
using AzzyBot.Utilities.Records.AzuraCast;

namespace AzzyBot;

public sealed record AzuraStationHistoryItemRecord : AzuraStationQueueItemRecord
{
    [JsonPropertyName("streamer")]
    public string Streamer { get; init; } = string.Empty;

    [JsonPropertyName("listeners_start")]
    public required int ListenersStart { get; init; }

    [JsonPropertyName("listeners_end")]
    public required int ListenersEnd { get; init; }

    [JsonPropertyName("delta_total")]
    public required int DeltaTotal { get; init; }
}

public sealed record AzuraStationHistoryExportRecord
{
    public TimeSpan PlayedAt { get; set; } = TimeSpan.Zero;
    public AzuraSongBasicDataRecord? Song { get; set; }
    public bool SongRequest { get; set; }
    public string Streamer { get; set; } = string.Empty;
    public string Playlist { get; set; } = string.Empty;
}
