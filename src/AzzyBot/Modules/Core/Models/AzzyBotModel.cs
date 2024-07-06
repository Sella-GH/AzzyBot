using System.Text.Json.Serialization;

namespace AzzyBot.Modules.Core.Models;

internal sealed class AzzyBotModel
{
    [JsonPropertyName(nameof(CompileDate))]
    public string CompileDate { get; set; } = string.Empty;

    [JsonPropertyName(nameof(Commit))]
    public string Commit { get; set; } = string.Empty;

    [JsonPropertyName(nameof(LoC_CS))]
    public string LoC_CS { get; set; } = string.Empty;

    [JsonPropertyName(nameof(LoC_JSON))]
    public string LoC_JSON { get; set; } = string.Empty;
}
