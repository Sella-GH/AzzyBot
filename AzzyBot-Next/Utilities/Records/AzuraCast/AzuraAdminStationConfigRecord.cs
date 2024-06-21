using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records.AzuraCast;

public sealed record AzuraAdminStationConfigRecord
{
    [JsonPropertyName("is_enabled")]
    public required bool IsEnabled { get; set; }
}
