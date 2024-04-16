using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcStationConfigModel
{
    [JsonProperty("enable_requests")]
    public bool Enable_requests { get; set; }
}
