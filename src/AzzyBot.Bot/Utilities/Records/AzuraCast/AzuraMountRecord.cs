using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

/// <summary>
/// Represents a mount point.
/// </summary>
[SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "It is a string and not an uri.")]
public sealed record AzuraStationMountRecord : AzuraDefaultMountBaseRecord
{
    /// <summary>
    /// Full listening URL specific to this mount
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// The listeners that are currently listening to the station.
    /// </summary>
    [JsonPropertyName("listeners")]
    public required AzuraNowPlayingListenersRecord Listeners { get; init; }

    /// <summary>
    /// If the mount is the default mount for the parent station
    /// </summary>
    [JsonPropertyName("is_default")]
    public required bool IsDefault { get; init; }
}

/// <summary>
/// Represents an HLS mount point.
/// </summary>
public sealed record AzuraHlsMountRecord : AzuraDefaultMountBaseRecord
{
    /// <summary>
    /// Total number of listeners (unique or not) currently tuned in to this mount
    /// </summary>
    [JsonPropertyName("listeners")]
    public required int Listeners { get; init; }

    /// <summary>
    /// Links about the mount point
    /// </summary>
    [JsonPropertyName("links")]
    public required AzuraMountLinksBaseRecord Links { get; init; }
}

/// <summary>
/// Represents a mount point on an AzuraCast station.
/// </summary>
public sealed record AzuraMountRecord : AzuraMountBaseRecord
{
    /// <summary>
    /// Number of unique listeners currently tuned in to this mount
    /// </summary>
    [JsonPropertyName("listeners_unique")]
    public required int ListenersUnique { get; init; }

    /// <summary>
    /// Total number of listeners (unique or not) currently tuned in to this mount
    /// </summary>
    [JsonPropertyName("listeners_total")]
    public required int ListenersTotal { get; init; }

    /// <summary>
    /// Audio encoding format of broadcasted audio (if known)
    /// </summary>
    [JsonPropertyName("autodj_format")]
    public required string AutoDjFormat { get; init; }

    /// <summary>
    /// Bitrate (kbps) of the broadcasted audio (if known)
    /// </summary>
    [JsonPropertyName("autodj_bitrate")]
    public required int AutoDjBitrate { get; init; }

    /// <summary>
    /// Links about the mount point
    /// </summary>
    [JsonPropertyName("links")]
    public required AzuraMountLinksRecord Links { get; init; }
}

/// <summary>
/// Represents a link section of a mount point on an AzuraCast station.
/// </summary>
public record AzuraMountLinksRecord : AzuraMountLinksBaseRecord
{
    /// <summary>
    /// Full URL to the intro of the mount point
    /// </summary>
    [JsonPropertyName("intro")]
    public required string Intro { get; init; }

    /// <summary>
    /// Full listening URL specific to this mount
    /// </summary>
    [JsonPropertyName("listen")]
    public required string Listen { get; init; }
}

/// <summary>
/// Represents a base record for a link section of a mount point.
/// </summary>
public record AzuraMountLinksBaseRecord
{
    /// <summary>
    /// Full URL to the mount point
    /// </summary>
    [JsonPropertyName("self")]
    public required string Self { get; init; }
}

/// <summary>
/// Represents a base record for a mount point.
/// </summary>
public record AzuraDefaultMountBaseRecord : AzuraMountBaseRecord
{
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

/// <summary>
/// Represents a base record for a mount point.
/// </summary>
public record AzuraMountBaseRecord
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
}
