using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records.AzuraCast;

public sealed record AzuraStationStatusRecord
{
    [JsonPropertyName("backend_running")]
    public bool BackendRunning { get; init; }

    [JsonPropertyName("frontend_running")]
    public bool FrontendRunning { get; init; }
}
