using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records.AzuraCast;

public sealed record AzuraRequestRecord
{
    [JsonPropertyName("request_id")]
    public required string RequestId { get; set; }

    [JsonPropertyName("song")]
    public required AzuraSongDataRecord Song { get; set; }
}
