using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcListenerModel
{
    [JsonProperty("connected_time")]
    public int TimeConnected { get; set; }
}
