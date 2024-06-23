using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records.AzuraCast;

[SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "This is a JSON valued string")]
public sealed record AzuraRequestRecord
{
    [JsonPropertyName("request_id")]
    public required string RequestId { get; init; }

    [JsonPropertyName("request_url")]
    public required string RequestUrl { get; init; }

    [JsonPropertyName("song")]
    public required AzuraSongDetailedRecord Song { get; init; }
}
