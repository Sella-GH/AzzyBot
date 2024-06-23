﻿using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records.AzuraCast;

public sealed record AzuraFilesRecord
{
    [JsonPropertyName("unique_id")]
    public required string UniqueId { get; init; }

    [JsonPropertyName("album")]
    public string Album { get; init; } = string.Empty;

    [JsonPropertyName("lengh_text")]
    public string Length { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("song_id")]
    public required string SongId { get; init; }

    [JsonPropertyName("artist")]
    public required string Artist { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }
}
