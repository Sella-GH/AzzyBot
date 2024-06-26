using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AzzyBot.Modules.Core.Settings;
using DSharpPlus.Entities;

namespace AzzyBot.Modules.Core.Updater;

internal static class Updates
{
    private static DateTime LastNotificationTime;
    private static Version LastOnlineVersion = new(0, 0, 0);
    private static int UpdateNotifyCounter;

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

        UpdaterModel updaterModel = JsonSerializer.Deserialize<UpdaterModel>(body) ?? throw new InvalidOperationException("UpdaterModel is null");

        Version updateVersion = new(updaterModel.Name);
        if (updateVersion == localVersion)
            return;

        if (!DateTime.TryParse(updaterModel.CreatedAt, out DateTime releaseDate))
            releaseDate = DateTime.Now;

        await SendUpdateMessageAsync(updateVersion, releaseDate, updaterModel.Body);
    }

    private static async Task SendUpdateMessageAsync(Version updateVersion, DateTime releaseDate, string changelog)
    {
        if (LastOnlineVersion != updateVersion)
        {
            LastNotificationTime = DateTime.MinValue;
            UpdateNotifyCounter = 0;
        }

        if (!CoreMisc.CheckUpdateNotification(UpdateNotifyCounter, LastNotificationTime))
            return;

        LastNotificationTime = DateTime.Now;
        LastOnlineVersion = updateVersion;
        UpdateNotifyCounter++;

        List<DiscordEmbed?> embeds = [CoreEmbedBuilder.BuildUpdatesAvailableEmbed(updateVersion, releaseDate)];

        if (CoreSettings.UpdaterDisplayChangelog)
            embeds.Add(CoreEmbedBuilder.BuildUpdatesAvailableChangelogEmbed(changelog));

        if (CoreSettings.UpdaterDisplayInstructions)
            embeds.Add(CoreEmbedBuilder.BuildUpdatesInstructionEmbed());

        IReadOnlyDictionary<ulong, DiscordGuild> guilds = AzzyBot.GetDiscordClientGuilds;
        ulong channelId = CoreSettings.ErrorChannelId;

        if (guilds.Count is 1)
        {
            bool channelSet = false;

            DiscordGuild? wantedGuild = null;
            foreach (KeyValuePair<ulong, DiscordGuild> guild in guilds)
            {
                wantedGuild = guild.Value;
            }

            // First check if the proposed channel exists in the guild
            if (wantedGuild is not null && CoreDiscordChecks.CheckIfChannelExists(wantedGuild, CoreSettings.UpdaterMessageChannelId))
            {
                channelId = CoreSettings.UpdaterMessageChannelId;
                channelSet = true;
            }

            // If not then check if the guild updates one exists
            if (!channelSet && wantedGuild is not null && CoreDiscordChecks.CheckIfChannelExists(wantedGuild, wantedGuild.PublicUpdatesChannel))
                channelId = wantedGuild.PublicUpdatesChannel.Id;
        }

        await AzzyBot.SendMessageAsync(channelId, string.Empty, embeds);
    }
}
