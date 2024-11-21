using System;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

/// <summary>
/// Represents the data for a song that was played on the station.
/// </summary>
public sealed record AzuraStationHistoryItemRecord : AzuraStationQueueItemRecord
{
    /// <summary>
    /// Indicates the current streamer that was connected, if available, or empty string if not.
    /// </summary>
    [JsonPropertyName("streamer")]
    public string Streamer { get; init; } = string.Empty;

    /// <summary>
    /// Number of listeners when the song playback started.
    /// </summary>
    [JsonPropertyName("listeners_start")]
    public required int ListenersStart { get; init; }

    /// <summary>
    /// Number of listeners when song playback ended.
    /// </summary>
    [JsonPropertyName("listeners_end")]
    public required int ListenersEnd { get; init; }

    /// <summary>
    /// The sum total change of listeners between the song's start and ending.
    /// </summary>
    [JsonPropertyName("delta_total")]
    public required int DeltaTotal { get; init; }
}

/// <summary>
/// Represents the custom data for a song that was played on the station to export it.
/// </summary>
public sealed record AzuraStationHistoryExportRecord
{
    /// <summary>
    /// The date the song was played.
    /// </summary>
    public string Date { get; init; } = string.Empty;

    /// <summary>
    /// The time the song was played.
    /// </summary>
    public TimeSpan PlayedAt { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// The basic data for the song that was played.
    /// </summary>
    public AzuraSongBasicDataRecord? Song { get; set; }

    /// <summary>
    /// Indicates whether the song is a listener request.
    /// </summary>
    public bool SongRequest { get; set; }

    /// <summary>
    /// The streamer that was connected when the song was played.
    /// </summary>
    public string Streamer { get; set; } = string.Empty;

    /// <summary>
    /// Indicates the playlist that the song was played from, if available, or empty string if not.
    /// </summary>
    public string Playlist { get; set; } = string.Empty;
}
