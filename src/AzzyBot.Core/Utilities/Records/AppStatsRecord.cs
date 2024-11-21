using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AzzyBot.Core.Utilities.Records;

/// <summary>
/// Represents the application statistics record.
/// </summary>
public sealed record AppStatsRecord
{
    /// <summary>
    /// The commit hash.
    /// </summary>
    [JsonPropertyName(nameof(Commit))]
    public string Commit { get; init; }

    /// <summary>
    /// The compilation date.
    /// </summary>
    [JsonPropertyName(nameof(CompilationDate))]
    public DateTimeOffset CompilationDate { get; init; }

    /// <summary>
    /// The lines of code in C#.
    /// </summary>
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
