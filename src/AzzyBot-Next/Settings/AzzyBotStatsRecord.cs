using System;
using System.Text.Json.Serialization;

namespace AzzyBot.Settings;

public sealed record AzzyBotStatsRecord
{
    [JsonPropertyName(nameof(Commit))]
    public string Commit { get; init; }

    [JsonPropertyName(nameof(CompilationDate))]
    public DateTime CompilationDate { get; init; }

    [JsonPropertyName(nameof(LocCs))]
    public int LocCs { get; init; }

    public AzzyBotStatsRecord(string commit, in DateTime compilationDate, int locCs)
    {
        Commit = commit;
        CompilationDate = compilationDate;
        LocCs = locCs;
    }
}
