using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

/// <summary>
/// Represents a listener record from an AzuraCast station.
/// </summary>
public sealed class AzuraStationListenerRecord
{
    /// <summary>
    /// The listener's IP address
    /// </summary>
    [JsonPropertyName("ip")]
    public string Ip { get; init; } = string.Empty;

    /// <summary>
    /// The listener's HTTP User-Agent
    /// </summary>
    [JsonPropertyName("user_agent")]
    public string UserAgent { get; init; } = string.Empty;

    /// <summary>
    /// The display name of the mount point.
    /// </summary>
    [JsonPropertyName("mount_name")]
    public string MountName { get; init; } = string.Empty;
}
