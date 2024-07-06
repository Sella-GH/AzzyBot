using System.Text.Json.Serialization;

namespace AzzyBot.Modules.Core.Updater;

internal sealed class UpdaterModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}
