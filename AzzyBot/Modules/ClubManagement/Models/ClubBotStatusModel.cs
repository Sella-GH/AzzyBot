using System.Collections.Generic;
using Newtonsoft.Json;

namespace AzzyBot.Modules.ClubManagement.Models;

internal sealed class BotStatus : ClubBotStatus
{
    [JsonProperty(nameof(ClubBotStatusList))]
    public List<ClubBotStatus> ClubBotStatusList { get; set; } = [];
}

internal class ClubBotStatus
{
    [JsonProperty(nameof(BotStatus))]
    public int BotStatus { get; set; }

    [JsonProperty(nameof(BotActivity))]
    public int BotActivity { get; set; }

    [JsonProperty(nameof(BotDoing))]
    public string BotDoing { get; set; } = string.Empty;

    [JsonProperty(nameof(BotStreamUrl))]
    public string BotStreamUrl { get; set; } = string.Empty;
}
