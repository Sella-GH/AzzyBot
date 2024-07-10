using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records.AzuraCast;

public sealed record AzuraInstanceServiceRecord
{
    public required IReadOnlyList<AzuraInstanceSingleServiceRecord> Services { get; init; }
}

public record AzuraInstanceSingleServiceRecord
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("running")]
    public required bool Running { get; init; }
}
