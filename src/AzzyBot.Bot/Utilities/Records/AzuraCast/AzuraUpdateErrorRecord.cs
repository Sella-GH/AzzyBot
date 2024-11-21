using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

/// <summary>
/// Represents an error from an AzuraCast update.
/// </summary>
public sealed record AzuraUpdateErrorRecord
{
    /// <summary>
    /// The error message.
    /// </summary>
    [JsonPropertyName("formatted_message")]
    public required string FormattedMessage { get; init; }
}
