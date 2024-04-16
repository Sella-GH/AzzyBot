using System.Collections.Generic;
using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcUpdateModel
{
    [JsonProperty("currentRelease")]
    public string CurrentRelease { get; set; } = string.Empty;

    [JsonProperty("latestRelease")]
    public string LatestRelease { get; set; } = string.Empty;

    [JsonProperty("needs_rolling_update")]
    public bool NeedsRollingUpdate { get; set; }

    [JsonProperty("rolling_updates_available")]
    public int RollingUpdatesAvailable { get; set; }

    [JsonProperty("rolling_updates_list")]
    public List<string> RollingUpdatesList { get; set; } = [];

    [JsonProperty("needs_release_update")]
    public bool NeedsReleaseUpdate { get; set; }

    [JsonProperty("can_switch_to_stable")]
    public bool CanSwitchToStable { get; set; }
}
