using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

/// <summary>
/// Represents the data for a media file from AzuraCast.
/// </summary>
public record AzuraFilesRecord
{
    /// <summary>
    /// A unique identifier associated with this record.
    /// </summary>
    [JsonPropertyName("unique_id")]
    public required string UniqueId { get; init; }

    /// <summary>
    /// The media file's 32-character unique song identifier hash
    /// </summary>
    [JsonPropertyName("song_id")]
    public required string SongId { get; init; }

    /// <summary>
    /// The relative path of the media file.
    /// </summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    /// <summary>
    /// The song artist
    /// </summary>
    [JsonPropertyName("artist")]
    public required string Artist { get; init; }

    /// <summary>
    /// The song title
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>
    /// The song album
    /// </summary>
    [JsonPropertyName("album")]
    public string Album { get; init; } = string.Empty;
}

/// <summary>
/// Represents the detailed data for a media file from AzuraCast.
/// </summary>
public sealed record AzuraFilesDetailedRecord : AzuraFilesRecord
{
    /// <summary>
    /// The formatted song duration (in mm:ss format)
    /// </summary>
    [JsonPropertyName("length_text")]
    public required string Length { get; init; }

    /// <summary>
    /// A list of all the playlists the media file is in.
    /// </summary>
    [JsonPropertyName("playlists")]
    public required IReadOnlyList<AzuraFilesPlaylistRecord> Playlists { get; init; }

    /// <summary>
    /// The song genre
    /// </summary>
    [JsonPropertyName("genre")]
    public required string Genre { get; init; }

    /// <summary>
    /// The International Standard Recording Code (ISRC) of the file.
    /// </summary>
    [JsonPropertyName("isrc")]
    public required string Isrc { get; init; }
}

/// <summary>
/// Represents a playlist that a media file is in.
/// </summary>
public sealed record AzuraFilesPlaylistRecord
{
    /// <summary>
    /// The playlist ID
    /// </summary>
    [JsonPropertyName("id")]
    public required int Id { get; init; }
}

/// <summary>
/// Represents the data for a media file upload to AzuraCast.
/// </summary>
public sealed record AzuraFileUploadRecord
{
    /// <summary>
    /// The relative path of the media file.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; init; }

    /// <summary>
    /// The media file's name.
    /// </summary>
    [JsonPropertyName("file")]
    public string File { get; init; }

    public AzuraFileUploadRecord(string path, string file)
    {
        Path = path;
        File = file;
    }
}

/// <summary>
/// Represents custom compliance data for a media file from AzuraCast.
/// </summary>
public sealed record AzuraFileComplianceRecord
{
    /// <summary>
    /// Whether the media file is compliant.
    /// </summary>
    public bool IsCompliant { get; init; }

    /// <summary>
    /// Whether the media file's title is compliant.
    /// </summary>
    public bool TitleCompliance { get; init; }

    /// <summary>
    /// Whether the media file's performer is compliant.
    /// </summary>
    public bool PerformerCompliance { get; init; }

    public AzuraFileComplianceRecord(bool isCompliant, bool titleCompliance, bool performerCompliance)
    {
        IsCompliant = isCompliant;
        TitleCompliance = titleCompliance;
        PerformerCompliance = performerCompliance;
    }
}
