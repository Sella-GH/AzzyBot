using System.Text.Json.Serialization;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcStationConfigModel
{
    [JsonPropertyName("enable_requests")]
    public bool Enable_requests { get; set; }
}
