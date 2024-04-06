using Newtonsoft.Json;

namespace AzzyBot.Modules.Core.Models;

internal sealed class AzzyBotModel
{
    [JsonProperty(nameof(CompileDate))]
    public string CompileDate { get; set; } = string.Empty;

    [JsonProperty(nameof(Commit))]
    public string Commit { get; set; } = string.Empty;

    [JsonProperty(nameof(LoC_CS))]
    public string LoC_CS { get; set; } = string.Empty;

    [JsonProperty(nameof(LoC_JSON))]
    public string LoC_JSON { get; set; } = string.Empty;
}
