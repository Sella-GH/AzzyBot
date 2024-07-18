using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraInstanceServiceRecord
{
    [JsonPropertyName("running")]
    public required bool Running { get; init; }
}
