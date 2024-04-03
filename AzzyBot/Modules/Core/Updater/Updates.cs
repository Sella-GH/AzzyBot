using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AzzyBot.Modules.Core.Updater;

internal static class Updates
{
    internal static async Task CheckForUpdatesAsync()
    {
        const string gitHubUrl = "https://api.github.com/repos/Sella-GH/AzzyBot/releases/latest";
        Version localVersion = new(CoreAzzyStatsGeneral.GetBotVersion);

        Dictionary<string, string> headers = new()
        {
            ["User-Agent"] = CoreAzzyStatsGeneral.GetBotName
        };
        string body = await CoreWebRequests.GetWebAsync(gitHubUrl, headers);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("GitHub release version body is empty");

        UpdaterModel? updaterModel = JsonConvert.DeserializeObject<UpdaterModel>(body) ?? throw new InvalidOperationException("UpdaterModel is null");

        Version updateVersion = new(updaterModel.name);
        if (updateVersion == localVersion)
            return;

        if (!DateTime.TryParse(updaterModel.createdAt, out DateTime releaseDate))
            releaseDate = DateTime.Now;

        await AzzyBot.SendMessageAsync(CoreSettings.ErrorChannelId, string.Empty, [CoreEmbedBuilder.BuildUpdatesAvailableEmbed(updateVersion, releaseDate), CoreEmbedBuilder.BuildUpdatesAvailableChangelogEmbed(updaterModel.body)]);
    }
}
