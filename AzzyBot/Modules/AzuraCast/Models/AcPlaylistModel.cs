using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcPlaylistModel
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("is_enabled")]
    public bool Is_enabled { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("short_name")]
    public string Short_name { get; set; } = string.Empty;
}
