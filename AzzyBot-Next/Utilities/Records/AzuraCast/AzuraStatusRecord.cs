using System.Text.Json.Serialization;

namespace AzzyBot;

public record AzuraStatusRecord
{
    [JsonPropertyName("online")]
    public required string Online { get; init; }
}
