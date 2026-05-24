using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Models.AzuraCast;

/// <summary>
/// Represents the status of the AzuraCast instance.
/// </summary>
public record class AzuraStatusModel
{
    /// <summary>
    /// Whether the service is online or not (should always be true)
    /// </summary>
    [JsonPropertyName("online")]
    public required bool Online { get; init; }
}
