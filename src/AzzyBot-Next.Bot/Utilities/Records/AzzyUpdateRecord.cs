using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records;

public sealed record AzzyUpdateRecord
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("created_at")]
    public required string CreatedAt { get; init; }

    [JsonPropertyName("body")]
    public required string Body { get; init; }
}
