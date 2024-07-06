using System.Text.Json.Serialization;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcPlaylistItemModel
{
    [JsonPropertyName("media")]
    public SongSimple Media { get; set; } = new();
}
