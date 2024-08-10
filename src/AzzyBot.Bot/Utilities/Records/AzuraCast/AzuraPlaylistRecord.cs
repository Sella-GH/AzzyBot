using System;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraPlaylistRecord
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; init; }

    [JsonPropertyName("include_in_requests")]
    public bool IncludeInRequests { get; init; }

    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("short_name")]
    public string ShortName { get; init; } = string.Empty;

    [JsonPropertyName("links")]
    public AzuraPlaylistLinkRecord Links { get; init; } = new AzuraPlaylistLinkRecord();
}

public sealed record AzuraPlaylistLinkRecord
{
    [JsonPropertyName("export")]
    public AzuraPlaylistLinkExportRecord Export { get; init; } = new AzuraPlaylistLinkExportRecord();
}

public sealed record AzuraPlaylistLinkExportRecord
{
    [JsonPropertyName("pls")]
    public Uri PLS { get; init; } = new Uri("https://github.com");

    [JsonPropertyName("m3u")]
    public Uri M3U { get; init; } = new Uri("https://github.com");
}

public sealed record AzuraPlaylistStateRecord
{
    public string PlaylistName { get; init; }
    public bool PlaylistState { get; init; }

    public AzuraPlaylistStateRecord(string playlistName, bool playlistState)
    {
        PlaylistName = playlistName;
        PlaylistState = playlistState;
    }
}
