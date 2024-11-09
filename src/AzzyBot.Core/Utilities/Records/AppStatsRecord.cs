using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AzzyBot.Core.Utilities.Records;

public sealed record AppStatsRecord
{
    [JsonPropertyName(nameof(Commit))]
    public string Commit { get; init; }

    [JsonPropertyName(nameof(CompilationDate))]
    public DateTimeOffset CompilationDate { get; init; }

    [JsonPropertyName(nameof(LocCs))]
    public int LocCs { get; init; }

    [SuppressMessage("Roslynator", "RCS1231:Make parameter ref read-only", Justification = "This is a constructor and does not allow referencing.")]
    public AppStatsRecord(string commit, DateTimeOffset compilationDate, int locCs)
    {
        Commit = commit;
        CompilationDate = compilationDate;
        LocCs = locCs;
    }
}
