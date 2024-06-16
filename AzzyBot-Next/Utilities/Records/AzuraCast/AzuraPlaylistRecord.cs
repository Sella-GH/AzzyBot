using System;
using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records.AzuraCast;

public sealed record AzuraPlaylistRecord
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; init; }

    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("short_name")]
    public string ShortName { get; init; } = string.Empty;

    [JsonPropertyName("links")]
    public AzuraPlaylistLinkRecord Links { get; init; } = new AzuraPlaylistLinkRecord();
}

public sealed record AzuraPlaylistLinkRecord
{
    [JsonPropertyName("self")]
    public Uri Self { get; init; } = new Uri(string.Empty);

    [JsonPropertyName("toggle")]
    public Uri Toggle { get; init; } = new Uri(string.Empty);

    [JsonPropertyName("export")]
    public AzuraPlaylistLinkExportRecord Export { get; init; } = new AzuraPlaylistLinkExportRecord();
}

public sealed record AzuraPlaylistLinkExportRecord
{
    [JsonPropertyName("pls")]
    public Uri PLS { get; init; } = new Uri(string.Empty);

    [JsonPropertyName("m3u")]
    public Uri M3U { get; init; } = new Uri(string.Empty);
}
