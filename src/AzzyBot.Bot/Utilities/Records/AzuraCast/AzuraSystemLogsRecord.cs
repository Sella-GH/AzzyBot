using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraSystemLogsRecord
{
    [JsonPropertyName("logs")]
    public required IReadOnlyList<AzuraSystemLogEntryRecord> Logs { get; init; }
}

public sealed record AzuraSystemLogEntryRecord
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("key")]
    public required string Key { get; init; }
}
