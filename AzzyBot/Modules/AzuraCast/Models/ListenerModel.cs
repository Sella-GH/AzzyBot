using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class ListenerModel
{
    [JsonProperty("connected_time")]
    public int TimeConnected { get; set; }
}
