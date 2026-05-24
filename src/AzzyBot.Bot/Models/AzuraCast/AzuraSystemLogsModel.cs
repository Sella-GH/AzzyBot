using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Models.AzuraCast;

/// <summary>
/// Represents the system logs available.
/// </summary>
public sealed record class AzuraSystemLogsModel
{
    /// <summary>
    /// The list of logs available.
    /// </summary>
    [JsonPropertyName("logs")]
    public required IReadOnlyList<AzuraSystemLogEntryModel> Logs { get; init; }
}

/// <summary>
/// Represents a system log entry.
/// </summary>
public sealed record class AzuraSystemLogEntryModel
{
    /// <summary>
    /// The name of the log.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// The internal name (key) of the log.
    /// </summary>
    [JsonPropertyName("key")]
    public required string Key { get; init; }
}

/// <summary>
/// Represents a system log.
/// </summary>
public sealed record class AzuraSystemLogModel
{
    /// <summary>
    /// The content of the log.
    /// </summary>
    [JsonPropertyName("contents")]
    public required string Content { get; init; }
}
