using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcUpdateModel
{
    [JsonPropertyName("currentRelease")]
    public string CurrentRelease { get; set; } = string.Empty;

    [JsonPropertyName("latestRelease")]
    public string LatestRelease { get; set; } = string.Empty;

    [JsonPropertyName("needs_rolling_update")]
    public bool NeedsRollingUpdate { get; set; }

    [JsonPropertyName("rolling_updates_available")]
    public int RollingUpdatesAvailable { get; set; }

    [JsonPropertyName("rolling_updates_list")]
    public List<string> RollingUpdatesList { get; set; } = [];

    [JsonPropertyName("needs_release_update")]
    public bool NeedsReleaseUpdate { get; set; }

    [JsonPropertyName("can_switch_to_stable")]
    public bool CanSwitchToStable { get; set; }
}
