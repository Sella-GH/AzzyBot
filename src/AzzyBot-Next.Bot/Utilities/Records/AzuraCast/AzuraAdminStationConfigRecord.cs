using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraAdminStationConfigRecord
{
    [JsonPropertyName("is_enabled")]
    public required bool IsEnabled { get; set; }

    [JsonPropertyName("backend_config")]
    public required AzuraAdminStationConfigBackendRecord BackendConfig { get; set; }

    [JsonPropertyName("enable_requests")]
    public required bool EnableRequests { get; set; }

    [JsonPropertyName("request_threshold")]
    public required int RequestThreshold { get; set; }
}

public sealed record AzuraAdminStationConfigBackendRecord
{
    [JsonPropertyName("write_playlists_to_liquidsoap")]
    public required bool WritePlaylistsToLiquidsoap { get; set; }
}
