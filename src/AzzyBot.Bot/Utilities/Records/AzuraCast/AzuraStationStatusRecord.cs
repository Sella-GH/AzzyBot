using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

/// <summary>
/// Represents the status of an AzuraCast station.
/// </summary>
public sealed record AzuraStationStatusRecord
{
    /// <summary>
    /// Whether the station backend service is running.
    /// </summary>
    [JsonPropertyName("backend_running")]
    public bool BackendRunning { get; init; }

    /// <summary>
    /// Whether the station frontend service is running.
    /// </summary>
    [JsonPropertyName("frontend_running")]
    public bool FrontendRunning { get; init; }
}
