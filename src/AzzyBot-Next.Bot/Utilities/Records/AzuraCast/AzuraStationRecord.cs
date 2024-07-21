using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraStationRecord
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("shortcode")]
    public required string Shortcode { get; init; }

    [JsonPropertyName("mounts")]
    public required IReadOnlyList<AzuraStationMountRecord> Mounts { get; init; }

    [JsonPropertyName("hls_url")]
    public Uri? HlsUrl { get; init; }
}

public sealed record AzuraStationMountRecord
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("url")]
    public required Uri Url { get; init; }
}
