using System.Text.Json.Serialization;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcListenerModel
{
    [JsonPropertyName("connected_time")]
    public int TimeConnected { get; set; }
}
