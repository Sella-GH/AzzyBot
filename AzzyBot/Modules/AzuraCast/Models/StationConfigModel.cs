using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class StationConfigModel
{
    [JsonProperty("enable_requests")]
    public bool Enable_requests { get; set; }
}
