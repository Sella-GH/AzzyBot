using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraUpdateErrorRecord
{
    [JsonPropertyName("formatted_message")]
    public required string FormattedMessage { get; init; }
}
