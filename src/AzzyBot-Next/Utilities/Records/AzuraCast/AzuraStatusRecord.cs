using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records.AzuraCast;

public record AzuraStatusRecord
{
    [JsonPropertyName("online")]
    public required bool Online { get; init; }
}
