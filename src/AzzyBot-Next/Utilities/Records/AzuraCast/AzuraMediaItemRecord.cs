using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records.AzuraCast;

public sealed record AzuraMediaItemRecord
{
    [JsonPropertyName("media")]
    public required AzuraSongBasicDataRecord Media { get; init; }
}
