using System.Text.Json.Serialization;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcSongRequestsModel
{
    [JsonPropertyName("request_id")]
    public string Request_Id { get; set; } = string.Empty;

    [JsonPropertyName("song")]
    public SongDetailed Song { get; set; } = new();
}
