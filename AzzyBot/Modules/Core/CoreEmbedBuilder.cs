using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzzyBot.Modules.Core.Models;
using AzzyBot.Modules.Core.Structs;
using AzzyBot.Strings.Core;
using DSharpPlus.Entities;

namespace AzzyBot.Modules.Core;

internal static class CoreEmbedBuilder
{
    private const string GitHubReleaseUrl = "https://github.com/Sella-GH/AzzyBot/releases/latest";

    internal static DiscordEmbedBuilder CreateBasicEmbed(string title, string description = "", string discordName = "", string discordAvatarUrl = "", DiscordColor? color = null, string thumbnailUrl = "", string footerText = "", string url = "", Dictionary<string, DiscordEmbedStruct>? fields = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        DiscordEmbedBuilder builder = new()
        {
            Title = title
        };

        if (!string.IsNullOrWhiteSpace(description))
            builder.Description = description;

        if (color is not null)
            builder.Color = color.Value;

        if (!string.IsNullOrWhiteSpace(discordName) && !string.IsNullOrWhiteSpace(discordAvatarUrl))
            builder.Author = new() { IconUrl = discordAvatarUrl, Name = discordName };

        if (!string.IsNullOrWhiteSpace(thumbnailUrl))
            builder.Thumbnail = new() { Url = thumbnailUrl };

        if (!string.IsNullOrWhiteSpace(footerText))
            builder.Footer = new() { Text = footerText };

        if (!string.IsNullOrWhiteSpace(url))
            builder.Url = url;

        if (fields is not null)
        {
            foreach (KeyValuePair<string, DiscordEmbedStruct> field in fields)
            {
                builder.AddField(field.Key, field.Value.Description, field.Value.IsInline);
            }
        }

        return builder;
    }

    internal static DiscordEmbed BuildAzzyHelpEmbed(string userName, string userAvatarUrl, List<AzzyHelpModel> commands)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(commands.Count, nameof(commands));

        string title = CoreStringBuilder.GetEmbedAzzyHelpTitle;
        string description = CoreStringBuilder.GetEmbedAzzyHelpDesc;

        Dictionary<string, DiscordEmbedStruct> fields = [];
        foreach (AzzyHelpModel field in commands)
        {
            fields.Add($"/{field.Name}", new(field.Name, field.Description, true));
        }

        return CreateBasicEmbed(title, description, userName, userAvatarUrl, DiscordColor.Blurple, string.Empty, string.Empty, string.Empty, fields);
    }

    internal static DiscordEmbed BuildAzzyHelpCommandEmbed(string userName, string userAvatarUrl, AzzyHelpModel model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));
        ArgumentNullException.ThrowIfNull(model, nameof(model));

        string title = $"/{model.Name}";
        string description = model.Description;
        description += CoreStringBuilder.GetEmbedAzzyHelpOptionDesc;

        Dictionary<string, DiscordEmbedStruct> fields = [];
        foreach ((string name, string desc) in model.Parameters)
        {
            fields.Add(name, new(name, desc, false));
        }

        return CreateBasicEmbed(title, description, userName, userAvatarUrl, DiscordColor.Blurple, string.Empty, string.Empty, string.Empty, fields);
    }

    internal static async Task<DiscordEmbed> BuildAzzyStatsEmbedAsync(string userName, string userAvatarUrl, int ping)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = CoreStringBuilder.GetEmbedAzzyStatsTitle;
        string footer = string.Empty;
        long uptime = CoreAzzyStatsGeneral.GetSystemUptime(); // is incorrect on windows systems without administative privileges
        string coreUsage = string.Empty;
        double processMem = CoreAzzyStatsGeneral.GetBotMemoryUsage();
        string diskUsage = CoreAzzyStatsGeneral.GetDiskUsage();
        CpuLoadStruct cpuLoad = new();
        MemoryUsageStruct memory = new();

        if (CoreMisc.CheckIfLinuxOs())
        {
            uptime = await CoreAzzyStatsLinux.GetSystemUptimeAsync();
            string allCoreUsage = string.Empty;
            Dictionary<int, double> cpuUsage = await CoreAzzyStatsLinux.GetCpuUsageAsync();
            memory = await CoreAzzyStatsLinux.GetMemoryUsageAsync();

            foreach (KeyValuePair<int, double> kvp in cpuUsage)
            {
                int counter = kvp.Key;

                if (counter == 0)
                {
                    allCoreUsage = CoreStringBuilder.GetEmbedAzzyStatsCpuUsageAll(kvp.Value);
                }
                else
                {
                    int core = counter - 1;
                    coreUsage += CoreStringBuilder.GetEmbedAzzyStatsCpuUsageCore(core, kvp.Value);
                }
            }

            coreUsage += allCoreUsage;

            cpuLoad = await CoreAzzyStatsLinux.GetCpuLoadAsync();
        }

        Dictionary<string, DiscordEmbedStruct> fields = CoreStringBuilder.GetEmbedAzzyStatsFields(uptime, ping, coreUsage, cpuLoad.OneMin, cpuLoad.FiveMin, cpuLoad.FifteenMin, memory.Used, processMem, memory.Total, diskUsage);

        if (CoreMisc.CheckIfLinuxOs())
        {
            Dictionary<string, NetworkSpeedStruct> networkUsage = await CoreAzzyStatsLinux.GetNetworkUsageAsync();
            foreach (KeyValuePair<string, NetworkSpeedStruct> kvp in networkUsage)
            {
                fields.Add(CoreStringBuilder.GetEmbedAzzyStatsNetworkUsageTitle(kvp.Key), new(kvp.Key, CoreStringBuilder.GetEmbedAzzyStatsNetworkUsageDesc(kvp.Value.Received, kvp.Value.Transmitted), true));
            }
        }
        else
        {
            footer = CoreStringBuilder.GetEmbedAzzyStatsMoreStats;
        }

        return CreateBasicEmbed(title, string.Empty, userName, userAvatarUrl, DiscordColor.Red, userAvatarUrl, footer, string.Empty, fields);
    }

    internal static async Task<DiscordEmbed> BuildInfoAzzyEmbedAsync(string userName, string userAvatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = CoreStringBuilder.GetEmbedAzzyInfoTitle;
        string botName = CoreAzzyStatsGeneral.GetBotName;
        string botUptime = CoreAzzyStatsGeneral.GetBotUptime();
        string botVersion = CoreAzzyStatsGeneral.GetBotVersion;
        string dotnetVersion = CoreAzzyStatsGeneral.GetDotNetVersion;
        string libVersion = CoreAzzyStatsGeneral.GetDSharpNetVersion.Split('+')[0];
        string commit = await CoreAzzyStatsGeneral.GetBotCommitAsync();
        string compilationDate = await CoreAzzyStatsGeneral.GetBotCompileDateAsync();
        string botEnvironment = CoreAzzyStatsGeneral.GetBotEnvironment;
        string activatedModules = CoreAzzyStatsGeneral.GetActivatedModules();

        string formattedCommit = commit;
        if (commit is not "Commit not found")
            formattedCommit = $"[{commit}](https://github.com/Sella-GH/AzzyBot/commit/{commit})";

        Dictionary<string, DiscordEmbedStruct> fields = new()
        {
            [CoreStringBuilder.EmbedAzzyInfoBotName] = new(nameof(botName), botName, false),
            [CoreStringBuilder.EmbedAzzyInfoBotUptime] = new(nameof(botUptime), botUptime, false),
            [CoreStringBuilder.EmbedAzzyInfoBotVersion] = new(nameof(botVersion), botVersion, true),
            [CoreStringBuilder.EmbedAzzyInfoNetVersion] = new(nameof(dotnetVersion), dotnetVersion, true),
            [CoreStringBuilder.EmbedAzzyInfoDspVersion] = new(nameof(libVersion), libVersion, true),
            [CoreStringBuilder.EmbedAzzyInfoGitHubCommit] = new(nameof(formattedCommit), formattedCommit, false),
            [CoreStringBuilder.EmbedAzzyInfoCompDate] = new(nameof(compilationDate), compilationDate, false),
            [CoreStringBuilder.EmbedAzzyInfoEnv] = new(nameof(botEnvironment), botEnvironment, false),
            [CoreStringBuilder.EmbedAzzyInfoModules] = new(nameof(activatedModules), activatedModules, false)
        };

        return CreateBasicEmbed(title, string.Empty, userName, userAvatarUrl, DiscordColor.Red, userAvatarUrl, string.Empty, string.Empty, fields);
    }

    internal static DiscordEmbed BuildUpdatesAvailableEmbed(Version version, in DateTime updateDate)
    {
        string title = CoreStringBuilder.GetEmbedUpdatesAvailableTitle;
        string body = CoreStringBuilder.GetEmbedUpdatesAvailableDesc;

        Dictionary<string, DiscordEmbedStruct> fields = new()
        {
            [CoreStringBuilder.GetEmbedUpdatesAvailableReleaseDate] = new(new(CoreStringBuilder.GetEmbedUpdatesAvailableReleaseDate), $"<t:{CoreMisc.ConvertToUnixTime(updateDate)}>", false),
            [CoreStringBuilder.GetEmbedUpdatesAvailableYourVersion] = new(new(CoreStringBuilder.GetEmbedUpdatesAvailableYourVersion), CoreAzzyStatsGeneral.GetBotVersion, false),
            [CoreStringBuilder.GetEmbedUpdatesAvailableUpdatedVersion] = new(new(CoreStringBuilder.GetEmbedUpdatesAvailableUpdatedVersion), version.ToString(), false)
        };

        return CreateBasicEmbed(title, body, AzzyBot.GetDiscordClientUserName, AzzyBot.GetDiscordClientAvatarUrl, DiscordColor.White, string.Empty, string.Empty, GitHubReleaseUrl, fields);
    }

    internal static DiscordEmbed BuildUpdatesAvailableChangelogEmbed(string changelog)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(changelog, nameof(changelog));

        string title = CoreStringBuilder.GetEmbedUpdatesAvailableChangelogTitle;
        string body = changelog;

        if (title.Length + body.Length > 6000)
            body = CoreStringBuilder.GetEmbedUpdatesAvailableChangelogTooBig(GitHubReleaseUrl);

        return CreateBasicEmbed(title, body, string.Empty, string.Empty, DiscordColor.White);
    }

    internal static DiscordEmbed BuildUpdatesInstructionEmbed()
    {
        bool isDocker = CoreAzzyStatsGeneral.GetBotName == "AzzyBot-Docker";
        bool isLinux = CoreMisc.CheckIfLinuxOs();
        const string title = "Update Instructions";
        string description;

        if (isDocker)
        {
            description = "To update the bot please go into the command line of the machine running the docker container and execute the following commands:\n- `docker compose down`\n- `docker compose pull`\n- `docker compose up -d`";
        }
        else if (isLinux)
        {
            description = "Please follow the instructions inside the [wiki](https://github.com/Sella-GH/AzzyBot/wiki/Linux-Update-Instructions).";
        }
        else
        {
            description = "Please follow the instructions inside the [wiki](https://github.com/Sella-GH/AzzyBot/wiki/Windows-Update-Instructions).";
        }

        return CreateBasicEmbed(title, description, string.Empty, string.Empty, DiscordColor.White);
    }
}
