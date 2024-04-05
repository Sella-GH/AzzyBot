using Newtonsoft.Json;

namespace AzzyBot.Modules.Core.Updater;

internal sealed class UpdaterModel
{
    [JsonProperty(nameof(name))]
    public string name { get; set; } = string.Empty;

    [JsonProperty("created_at")]
    public string createdAt { get; set; } = string.Empty;

    [JsonProperty(nameof(body))]
    public string body { get; set; } = string.Empty;
}
