using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraSystemLogRecord
{
    [JsonPropertyName("contents")]
    public required string Content { get; init; }
}
