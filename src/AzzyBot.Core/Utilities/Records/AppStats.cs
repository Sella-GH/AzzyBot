using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzzyBot.Core.Utilities.Records;

/// <summary>
/// Represents the application statistics record.
/// </summary>
public sealed record AppStats
{
    /// <summary>
    /// The commit hash.
    /// </summary>
    [Length(0, 40, ErrorMessage = "CommitHash must be exactly 40 characters long."), JsonPropertyName(nameof(Commit))]
    public string Commit { get; set; } = "Unknown";

    /// <summary>
    /// The compilation date.
    /// </summary>
    [JsonPropertyName(nameof(CompilationDate))]
    public DateTimeOffset CompilationDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The lines of code in C#.
    /// </summary>
    [JsonPropertyName(nameof(LocCs))]
    public int LocCs { get; set; }
}
