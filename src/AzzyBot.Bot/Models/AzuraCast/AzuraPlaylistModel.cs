using System;
using System.Text.Json.Serialization;

using AzzyBot.Bot.Resources;

namespace AzzyBot.Bot.Models.AzuraCast;

/// <summary>
/// Represents a playlist from AzuraCast.
/// </summary>
public sealed record class AzuraPlaylistModel
{
    /// <summary>
    /// The name of the playlist.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Whether the playlist is enabled.
    /// </summary>
    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Whether the playlist content is included in requests.
    /// </summary>
    [JsonPropertyName("include_in_requests")]
    public bool IncludeInRequests { get; init; }

    /// <summary>
    /// The ID of the playlist.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>
    /// The short name of the playlist.
    /// </summary>
    [JsonPropertyName("short_name")]
    public string ShortName { get; init; } = string.Empty;

    /// <summary>
    /// A collection of links for the playlist.
    /// </summary>
    [JsonPropertyName("links")]
    public AzuraPlaylistLinkModel Links { get; init; } = new();
}

/// <summary>
/// Represents the links for a playlist.
/// </summary>
public sealed record class AzuraPlaylistLinkModel
{
    /// <summary>
    /// The export links for the playlist.
    /// </summary>
    [JsonPropertyName("export")]
    public AzuraPlaylistLinkExportModel Export { get; init; } = new();
}

/// <summary>
/// Represents the export links for a playlist.
/// </summary>
public sealed record class AzuraPlaylistLinkExportModel
{
    /// <summary>
    /// The PLS export link for the playlist.
    /// </summary>
    [JsonPropertyName("pls")]
    public Uri PLS { get; init; } = new(UriStrings.GitHubUri);

    /// <summary>
    /// The M3U export link for the playlist.
    /// </summary>
    [JsonPropertyName("m3u")]
    public Uri M3U { get; init; } = new(UriStrings.GitHubUri);
}

/// <summary>
/// Represents the (custom made) state of a playlist.
/// </summary>
public sealed record class AzuraPlaylistStateModel
{
    /// <summary>
    /// The name of the playlist.
    /// </summary>
    public string PlaylistName { get; init; }

    /// <summary>
    /// Whether the playlist is enabled or not.
    /// </summary>
    public bool PlaylistState { get; init; }

    public AzuraPlaylistStateModel(string playlistName, bool playlistState)
    {
        PlaylistName = playlistName;
        PlaylistState = playlistState;
    }
}
