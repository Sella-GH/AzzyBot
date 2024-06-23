using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records.AzuraCast;

public sealed record AzuraAdminStationConfigRecord
{
    [JsonPropertyName("enable_requests")]
    public required bool EnableRequests { get; set; }

    [JsonPropertyName("is_enabled")]
    public required bool IsEnabled { get; set; }
}
