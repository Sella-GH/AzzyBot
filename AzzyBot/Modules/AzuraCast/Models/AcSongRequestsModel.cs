using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcSongRequestsModel
{
    [JsonProperty("request_id")]
    public string Request_Id { get; set; } = string.Empty;

    [JsonProperty("song")]
    public SongDetailed Song { get; set; } = new();
}
