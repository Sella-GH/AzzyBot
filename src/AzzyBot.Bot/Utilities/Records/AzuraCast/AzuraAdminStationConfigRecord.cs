using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

/// <summary>
/// Represents the configuration for a station.
/// </summary>
public sealed record AzuraAdminStationConfigRecord
{
    /// <summary>
    /// The full display name of the station.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// If set to 'false', prevents the station from broadcasting but leaves it in the database.
    /// </summary>
    [JsonPropertyName("is_enabled")]
    public required bool IsEnabled { get; set; }

    /// <summary>
    /// An object containing station-specific backend configuration
    /// </summary>
    [JsonPropertyName("backend_config")]
    public required AzuraAdminStationConfigBackendRecord BackendConfig { get; set; }

    /// <summary>
    /// Whether listeners can request songs to play on this station.
    /// </summary>
    [JsonPropertyName("enable_requests")]
    public required bool EnableRequests { get; set; }

    /// <summary>
    /// This specifies the minimum time (in minutes) between a song playing on the radio and being available to request again. Set to 0 for no threshold.
    /// </summary>
    [JsonPropertyName("request_threshold")]
    public required int RequestThreshold { get; set; }
}

/// <summary>
/// Represents the backend configuration for a station.
/// </summary>
public sealed record AzuraAdminStationConfigBackendRecord
{
    /// <summary>
    /// By default, all playlists are written to Liquidsoap as a backup in case the normal AutoDJ fails. Disable to only write essential playlists to Liquidsoap.
    /// </summary>
    [JsonPropertyName("write_playlists_to_liquidsoap")]
    public required bool WritePlaylistsToLiquidsoap { get; set; }
}
