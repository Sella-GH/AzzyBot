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
    /// A list of all the station's mounts
    /// </summary>
    [JsonPropertyName("mounts")]
    public required IReadOnlyList<AzuraStationMountRecord> Mounts { get; init; }

    /// <summary>
    /// The full URL to listen to the HLS stream for the station.
    /// </summary>
    [JsonPropertyName("hls_url")]
    public string? HlsUrl { get; init; }
}

/// <summary>
/// Represents a mount point on an AzuraCast station.
/// </summary>
[SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "It is a string and not an uri.")]
public sealed record AzuraStationMountRecord
{
    /// <summary>
    /// Mount/Remote ID number.
    /// </summary>
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    /// <summary>
    /// Mount point name/URL
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Full listening URL specific to this mount
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// Bitrate (kbps) of the broadcasted audio (if known)
    /// </summary>
    [JsonPropertyName("bitrate")]
    public required int Bitrate { get; init; }

    /// <summary>
    /// Audio encoding format of broadcasted audio (if known)
    /// </summary>
    [JsonPropertyName("format")]
    public required string Format { get; init; }
}
