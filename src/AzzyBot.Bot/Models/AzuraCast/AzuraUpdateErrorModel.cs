using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Models.AzuraCast;

/// <summary>
/// Represents an error from an AzuraCast update.
/// </summary>
public sealed record class AzuraUpdateErrorModel
{
    /// <summary>
    /// The error message.
    /// </summary>
    [JsonPropertyName("formatted_message")]
    public required string FormattedMessage { get; init; }
}
