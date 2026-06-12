using System.Text.Json.Serialization;
using Dameng.SepEx;

namespace AzzyBot.Bot.Models.AzuraCast;

/// <summary>
/// Represents the full data for a song from AzuraCast.
/// </summary>
public sealed record class AzuraSongDataModel : AzuraSongAdvancedDataModel
{
    /// <summary>
    /// The song genre.
    /// </summary>
    [JsonPropertyName("genre")]
    public string Genre { get; init; } = string.Empty;

    /// <summary>
    /// The International Standard Recording Code (ISRC) of the file.
    /// </summary>
    [JsonPropertyName("isrc")]
    public string Isrc { get; init; } = string.Empty;

    /// <summary>
    /// Lyrics to the song.
    /// </summary>
    [JsonPropertyName("lyrics")]
    public string Lyrics { get; init; } = string.Empty;
}

/// <summary>
/// Represents the partial but not full data for a song from AzuraCast.
/// </summary>
public record class AzuraSongAdvancedDataModel : AzuraSongBasicDataModel
{
    /// <summary>
    /// A unique identifier associated with this record.
    /// </summary>
    [JsonPropertyName("unique_id")]
    public string UniqueId { get; set; } = string.Empty;

    /// <summary>
    /// The song's 32-character unique identifier hash
    /// </summary>
    [JsonPropertyName("id")]
    public required string SongId { get; init; } = string.Empty;

    /// <summary>
    /// The song title, usually "Artist - Title"
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; set; } = string.Empty;

    /// <summary>
    /// URL to the album artwork (if available).
    /// </summary>
    [JsonPropertyName("art")]
    public required string Art { get; set; } = string.Empty;
}

/// <summary>
/// Represents the basic data for a song from AzuraCast.
/// </summary>
[GenSepParsable]
public partial record class AzuraSongBasicDataModel
{
    /// <summary>
    /// The song artist.
    /// </summary>
    [JsonPropertyName("artist")]
    public required string Artist { get; set; }

    /// <summary>
    /// The song title.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    /// <summary>
    /// The song album.
    /// </summary>
    [JsonPropertyName("album")]
    public string Album { get; set; } = string.Empty;
}

/// <summary>
/// Represents the basic data for a song from AzuraCast.
/// </summary>
public sealed record class AzuraMediaItemModel
{
    /// <summary>
    /// The song item.
    /// </summary>
    [JsonPropertyName("media")]
    public required AzuraSongBasicDataModel Media { get; init; }
}

/// <summary>
/// Represents partial data for a song from AzuraCast.
/// </summary>
public sealed record class AzuraTrackModel
{
    /// <summary>
    /// A unique identifier associated with this record.
    /// </summary>
    [JsonPropertyName("unique_id")]
    public required string UniqueId { get; init; }

    /// <summary>
    /// The request id of the song.
    /// </summary>
    [JsonPropertyName("song_id")]
    public required string SongId { get; init; }

    /// <summary>
    /// The song artist.
    /// </summary>
    [JsonPropertyName("artist")]
    public required string Artist { get; init; }

    /// <summary>
    /// The song title.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }
}
