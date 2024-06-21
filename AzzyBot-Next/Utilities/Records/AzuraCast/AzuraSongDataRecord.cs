using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records.AzuraCast;

public record AzuraSongDataRecord
{
    [JsonPropertyName("duration")]
    public int Duration { get; init; }

    [JsonPropertyName("playlist")]
    public string Playlist { get; init; } = string.Empty;

    [JsonPropertyName("song")]
    public AzuraSongDetailedRecord Song { get; init; } = new();
}

public sealed record AzuraSongDetailedRecord : AzuraSongSimpleRecord
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("art")]
    public string Art { get; init; } = string.Empty;
}

public record AzuraSongSimpleRecord
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("artist")]
    public string Artist { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("album")]
    public string Album { get; init; } = string.Empty;
}
