﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public record AzuraFilesRecord
{
    [JsonPropertyName("unique_id")]
    public required string UniqueId { get; init; }

    [JsonPropertyName("song_id")]
    public required string SongId { get; init; }

    [JsonPropertyName("art")]
    public required string Art { get; init; }

    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("artist")]
    public required string Artist { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("album")]
    public string Album { get; init; } = string.Empty;
}

public sealed record AzuraFilesDetailedRecord : AzuraFilesRecord
{
    [JsonPropertyName("length_text")]
    public required string Length { get; init; }

    [JsonPropertyName("playlists")]
    public required IReadOnlyList<AzuraFilesPlaylistRecord> Playlists { get; init; }

    [JsonPropertyName("genre")]
    public required string Genre { get; init; }

    [JsonPropertyName("isrc")]
    public required string Isrc { get; init; }
}

public sealed record AzuraFilesPlaylistRecord
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }
}
public sealed record AzuraFileUploadRecord
{
    [JsonPropertyName("path")]
    public string Path { get; init; }

    [JsonPropertyName("file")]
    public string File { get; init; }

    public AzuraFileUploadRecord(string path, string file)
    {
        Path = path;
        File = file;
    }
}

public sealed record AzuraFileComplianceRecord
{
    public bool IsCompliant { get; init; }
    public bool TitleCompliance { get; init; }
    public bool PerformerCompliance { get; init; }

    public AzuraFileComplianceRecord(bool isCompliant, bool titleCompliance, bool performerCompliance)
    {
        IsCompliant = isCompliant;
        TitleCompliance = titleCompliance;
        PerformerCompliance = performerCompliance;
    }
}
