using System.Text.Json.Serialization;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcPlaylistModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("is_enabled")]
    public bool Is_enabled { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("short_name")]
    public string Short_name { get; set; } = string.Empty;
}
