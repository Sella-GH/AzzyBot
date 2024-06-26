using System.Text.Json.Serialization;

namespace AzzyBot;

public sealed record AzuraStationHistoryItemRecord : AzuraStationQueueItemRecord
{
    [JsonPropertyName("listeners_start")]
    public required int ListenersStart { get; init; }

    [JsonPropertyName("listeners_end")]
    public required int ListenersEnd { get; init; }

    [JsonPropertyName("delta_total")]
    public required int DeltaTotal { get; init; }
}
