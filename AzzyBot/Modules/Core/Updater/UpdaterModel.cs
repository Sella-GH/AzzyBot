using Newtonsoft.Json;

namespace AzzyBot.Modules.Core.Updater;

internal sealed class UpdaterModel
{
    [JsonProperty(nameof(Updater))]
    public Updater Updater { get; set; } = new();
}

internal sealed class Updater
{
    [JsonProperty(nameof(ApiUrl))]
    public string ApiUrl { get; set; } = string.Empty;

    [JsonProperty(nameof(Permissions))]
    public string Permissions { get; set; } = string.Empty;

    [JsonProperty(nameof(PersonalAccessToken))]
    public string PersonalAccessToken { get; set; } = string.Empty;
}
