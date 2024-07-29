using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Settings;

public sealed record AzzyBotStatsRecord
{
    [JsonPropertyName(nameof(Commit))]
    public string Commit { get; init; }

    [JsonPropertyName(nameof(CompilationDate))]
    public DateTime CompilationDate { get; init; }

    [JsonPropertyName(nameof(LocCs))]
    public int LocCs { get; init; }

    [SuppressMessage("Roslynator", "RCS1231:Make parameter ref read-only", Justification = "This is a constructor and does not allow referencing.")]
    public AzzyBotStatsRecord(string commit, DateTime compilationDate, int locCs)
    {
        Commit = commit;
        CompilationDate = compilationDate;
        LocCs = locCs;
    }
}
