using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraUpdateRecord
{
    [JsonPropertyName("current_release")]
    public required string CurrentRelease { get; init; }

    [JsonPropertyName("latest_release")]
    public required string LatestRelease { get; init; }

    [JsonPropertyName("needs_rolling_update")]
    public required bool NeedsRollingUpdate { get; init; }

    [JsonPropertyName("rolling_updates_list")]
    public IReadOnlyList<string> RollingUpdatesList { get; init; } = [];

    [JsonPropertyName("needs_release_update")]
    public required bool NeedsReleaseUpdate { get; init; }

    [JsonPropertyName("can_switch_to_stable")]
    public required bool CanSwitchToStable { get; init; }
}
