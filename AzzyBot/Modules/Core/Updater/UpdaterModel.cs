using Newtonsoft.Json;

namespace AzzyBot.Modules.Core.Updater;

internal sealed class UpdaterModel
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonProperty("body")]
    public string Body { get; set; } = string.Empty;
}
