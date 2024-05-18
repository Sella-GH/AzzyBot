using System;
using System.Text.Json.Serialization;

namespace AzzyBot.Settings;

internal sealed record AzzyBotStatsRecord
{
    [JsonPropertyName(nameof(Commit))]
    public required string Commit { get; init; }

    [JsonPropertyName(nameof(CompilationDate))]
    public required DateTime CompilationDate { get; init; }

    [JsonPropertyName(nameof(LocCs))]
    public required int LocCs { get; init; }
}
