using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcPlaylistItemModel
{
    [JsonProperty("media")]
    public SongSimple Media { get; set; } = new();
}
