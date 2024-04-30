using System.Text.Json.Serialization;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcStationModel
{
    [JsonPropertyName("id")]
    internal int Id { get; set; }

    [JsonPropertyName("listen_url")]
    internal string ListenUrl { get; set; } = string.Empty;
}
