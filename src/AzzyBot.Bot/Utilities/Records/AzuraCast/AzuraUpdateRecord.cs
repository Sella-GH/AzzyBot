using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

/// <summary>
/// Represents the update information for AzuraCast.
/// </summary>
public sealed record AzuraUpdateRecord
{
    /// <summary>
    /// The current release version of AzuraCast.
    /// </summary>
    [JsonPropertyName("current_release")]
    public string? CurrentRelease { get; init; }

    /// <summary>
    /// The latest release version of AzuraCast.
    /// </summary>
    [JsonPropertyName("latest_release")]
    public string? LatestRelease { get; init; }

    /// <summary>
    /// Whether the current release is rolling release and needs updates.
    /// </summary>
    [JsonPropertyName("needs_rolling_update")]
    public bool NeedsRollingUpdate { get; init; }

    /// <summary>
    /// Whether the current release is stable release and needs updates.
    /// </summary>
    [JsonPropertyName("needs_release_update")]
    public bool NeedsReleaseUpdate { get; init; }

    /// <summary>
    /// The number of rolling updates available for the current release.
    /// </summary>
    [JsonPropertyName("rolling_updates_available")]
    public int RollingUpdatesAvailable { get; init; }

    /// <summary>
    /// Whether the current rolling release can be switched to a stable release.
    /// </summary>
    [JsonPropertyName("can_switch_to_stable")]
    public bool CanSwitchToStable { get; init; }
}
