using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcFilesModel
{
    [JsonProperty("unique_id")]
    public string Unique_Id { get; set; } = string.Empty;

    [JsonProperty("album")]
    public string Album { get; set; } = string.Empty;

    [JsonProperty("path")]
    public string Path { get; set; } = string.Empty;

    [JsonProperty("song_id")]
    public string Song_Id { get; set; } = string.Empty;

    [JsonProperty("artist")]
    public string Artist { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
}
