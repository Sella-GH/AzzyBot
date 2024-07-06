using System.Text.Json.Serialization;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcStationModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("listen_url")]
    public string ListenUrl { get; set; } = string.Empty;
}
