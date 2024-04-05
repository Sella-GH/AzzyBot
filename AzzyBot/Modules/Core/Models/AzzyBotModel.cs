using Newtonsoft.Json;

namespace AzzyBot.Modules.Core.Models;

internal sealed class AzzyBotModel
{
    [JsonProperty(nameof(CompileDate))]
    public string CompileDate { get; set; } = string.Empty;

    [JsonProperty(nameof(Commit))]
    public string Commit { get; set; } = string.Empty;

    [JsonProperty(nameof(LoC))]
    public string LoC { get; set; } = string.Empty;
}
