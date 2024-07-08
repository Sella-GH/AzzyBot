using System.Text.Json.Serialization;

namespace AzzyBot;

public record AzuraStatusRecord
{
    [JsonPropertyName("online")]
    public required bool Online { get; init; }
}
