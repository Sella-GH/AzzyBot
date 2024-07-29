using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraSongDataRecord : AzuraSongAdvancedDataRecord
{
    [JsonPropertyName("genre")]
    public string Genre { get; init; } = string.Empty;

    [JsonPropertyName("isrc")]
    public string Isrc { get; init; } = string.Empty;

    [JsonPropertyName("lyrics")]
    public string Lyrics { get; init; } = string.Empty;
}

public record AzuraSongAdvancedDataRecord : AzuraSongBasicDataRecord
{
    [JsonPropertyName("unique_id")]
    public string UniqueId { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public required string SongId { get; init; } = string.Empty;

    [JsonPropertyName("text")]
    public required string Text { get; set; } = string.Empty;

    [JsonPropertyName("art")]
    public required string Art { get; init; } = string.Empty;
}

public record AzuraSongBasicDataRecord
{
    [JsonPropertyName("artist")]
    public required string Artist { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public required string Title { get; set; } = string.Empty;

    [JsonPropertyName("album")]
    public string Album { get; set; } = string.Empty;
}
