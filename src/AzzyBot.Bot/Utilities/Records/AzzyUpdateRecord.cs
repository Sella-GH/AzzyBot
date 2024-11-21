using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records;

/// <summary>
/// Represents an update from the AzzyBot GitHub repository.
/// </summary>
public sealed record AzzyUpdateRecord
{
    /// <summary>
    /// The name of the update.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// When the update was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public required string CreatedAt { get; init; }

    /// <summary>
    /// The body of the update.
    /// </summary>
    [JsonPropertyName("body")]
    public required string Body { get; init; }
}
