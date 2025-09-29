using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

/// <summary>
/// Represents a station on an AzuraCast instance.
/// </summary>
[SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "It is a string and not an uri.")]
public sealed record AzuraStationRecord
{
    /// <summary>
    /// Station ID
    /// </summary>
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    /// <summary>
    /// Station name
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Station "short code", used for URL and folder paths
    /// </summary>
    [JsonPropertyName("shortcode")]
    public required string Shortcode { get; init; }

    /// <summary>
    /// If the station is public (i.e. should be shown in listings of all stations)
    /// </summary>
    [JsonPropertyName("is_public")]
    public required bool IsPublic { get; init; }

    /// <summary>
    /// A list of all the station's mounts
    /// </summary>
    [JsonPropertyName("mounts")]
    public required IReadOnlyList<AzuraStationMountRecord> Mounts { get; init; }

    /// <summary>
    /// If the station has HLS streaming enabled.
    /// </summary>
    [JsonPropertyName("hls_enabled")]
    public required bool HlsEnabled { get; set; }

    /// <summary>
    /// If the HLS stream should be the default one for the station.
    /// </summary>
    [JsonPropertyName("hls_is_default")]
    public required bool HlsIsDefault { get; set; }

    /// <summary>
    /// The full URL to listen to the HLS stream for the station.
    /// </summary>
    [JsonPropertyName("hls_url")]
    public string? HlsUrl { get; init; }
}
