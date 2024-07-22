using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

[SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "It is a string and not an uri.")]
public sealed record AzuraStationRecord
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("shortcode")]
    public required string Shortcode { get; init; }

    [JsonPropertyName("mounts")]
    public required IReadOnlyList<AzuraStationMountRecord> Mounts { get; init; }

    [JsonPropertyName("hls_url")]
    public string? HlsUrl { get; init; }
}

[SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "It is a string and not an uri.")]
public sealed record AzuraStationMountRecord
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("bitrate")]
    public required int Bitrate { get; init; }

    [JsonPropertyName("format")]
    public required string Format { get; init; }
}
