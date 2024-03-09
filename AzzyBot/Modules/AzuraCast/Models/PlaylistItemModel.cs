using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class PlaylistItemModel
{
    [JsonProperty("media")]
    public SongSimple Media { get; set; } = new();
}
