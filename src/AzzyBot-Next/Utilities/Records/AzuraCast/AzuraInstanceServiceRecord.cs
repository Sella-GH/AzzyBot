using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records.AzuraCast;

public sealed record AzuraInstanceServiceRecord
{
    [JsonPropertyName("running")]
    public required bool Running { get; init; }
}
