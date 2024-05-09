using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records;

internal sealed record UpdateRecord
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("created_at")]
    public required string CreatedAt { get; init; }

    [JsonPropertyName("body")]
    public required string Body { get; init; }
}
