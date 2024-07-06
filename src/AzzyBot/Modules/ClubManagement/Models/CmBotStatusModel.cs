using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzzyBot.Modules.ClubManagement.Models;

internal sealed class BotStatus : ClubBotStatus
{
    [JsonPropertyName(nameof(ClubBotStatusList))]
    public List<ClubBotStatus> ClubBotStatusList { get; set; } = [];
}

internal class ClubBotStatus
{
    [JsonPropertyName(nameof(BotStatus))]
    public int BotStatus { get; set; }

    [JsonPropertyName(nameof(BotActivity))]
    public int BotActivity { get; set; }

    [JsonPropertyName(nameof(BotDoing))]
    public string BotDoing { get; set; } = string.Empty;

    [JsonPropertyName(nameof(BotStreamUrl))]
    public string BotStreamUrl { get; set; } = string.Empty;
}
