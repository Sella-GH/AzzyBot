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
    internal static DiscordEmbedBuilder CreateBasicEmbed(string title, string description = "", string discordName = "", string discordAvatarUrl = "", DiscordColor? color = null, string thumbnailUrl = "", string footerText = "", Dictionary<string, DiscordEmbedStruct>? fields = null)
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

        return CreateBasicEmbed(title, description, userName, userAvatarUrl, DiscordColor.Blurple, string.Empty, string.Empty, fields);
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

        return CreateBasicEmbed(title, description, userName, userAvatarUrl, DiscordColor.Blurple, string.Empty, string.Empty, fields);
    }

    internal static async Task<DiscordEmbed> BuildAzzyStatsEmbedAsync(string userName, string userAvatarUrl, int ping)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        Dictionary<int, double> cpuUsage = await CoreAzzyStatsLinux.GetCpuUsageAsync();
        long uptime = await CoreAzzyStatsLinux.GetSystemUptimeAsync();
        string coreUsage = string.Empty;
        string allCoreUsage = string.Empty;
        MemoryUsageStruct memory = await CoreAzzyStatsLinux.GetMemoryUsageAsync();
        double processMem = CoreAzzyStatsGeneral.GetBotMemoryUsage();
        string diskUsage = CoreAzzyStatsGeneral.GetDiskUsage();
        Dictionary<string, NetworkSpeedStruct> networkUsage = await CoreAzzyStatsLinux.GetNetworkUsageAsync();

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

        string title = CoreStringBuilder.GetEmbedAzzyStatsTitle;
        CpuLoadStruct cpuLoad = await CoreAzzyStatsLinux.GetCpuLoadAsync();
        Dictionary<string, DiscordEmbedStruct> fields = CoreStringBuilder.GetEmbedAzzyStatsFields(uptime, ping, coreUsage, cpuLoad.OneMin, cpuLoad.FiveMin, cpuLoad.FifteenMin, memory.Used, processMem, memory.Total, diskUsage);

        foreach (KeyValuePair<string, NetworkSpeedStruct> kvp in networkUsage)
        {
            fields.Add(CoreStringBuilder.GetEmbedAzzyStatsNetworkUsageTitle(kvp.Key), new(kvp.Key, CoreStringBuilder.GetEmbedAzzyStatsNetworkUsageDesc(kvp.Value.Received, kvp.Value.Transmitted), true));
        }

        return CreateBasicEmbed(title, string.Empty, userName, userAvatarUrl, DiscordColor.Red, userAvatarUrl, string.Empty, fields);
    }

    internal static DiscordEmbed BuildAzzyStatsNotAvailableEmbed(string userName, string userAvatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = CoreStringBuilder.GetEmbedAzzyStatsNotAvailableTitle;
        string description = CoreStringBuilder.GetEmbedAzzyStatsNotAvailableDesc;

        return CreateBasicEmbed(title, description, userName, userAvatarUrl, DiscordColor.IndianRed);
    }

    internal static async Task<DiscordEmbed> BuildInfoAzzyEmbedAsync(string userName, string userAvatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAvatarUrl, nameof(userAvatarUrl));

        string title = CoreStringBuilder.GetEmbedAzzyInfoTitle;
        string botName = CoreAzzyStatsGeneral.GetBotName;
        string botUptime = CoreAzzyStatsGeneral.GetBotUptime;
        string botVersion = CoreAzzyStatsGeneral.GetBotVersion;
        string dotnetVersion = CoreAzzyStatsGeneral.GetDotNetVersion;
        string libVersion = CoreAzzyStatsGeneral.GetDSharpNetVersion.Split('+')[0];
        string commit = await CoreAzzyStatsGeneral.GetBotCommitAsync();
        string compilationDate = await CoreAzzyStatsGeneral.GetBotCompileDateAsync();
        string botEnvironment = CoreAzzyStatsGeneral.GetBotEnvironment;
        string activatedModules = CoreAzzyStatsGeneral.GetActivatedModules();

        Dictionary<string, DiscordEmbedStruct> fields = new()
        {
            [CoreStringBuilder.EmbedAzzyInfoBotName] = new(nameof(botName), botName, false),
            [CoreStringBuilder.EmbedAzzyInfoBotUptime] = new(nameof(botUptime), botUptime, false),
            [CoreStringBuilder.EmbedAzzyInfoBotVersion] = new(nameof(botVersion), botVersion, true),
            [CoreStringBuilder.EmbedAzzyInfoNetVersion] = new(nameof(dotnetVersion), dotnetVersion, true),
            [CoreStringBuilder.EmbedAzzyInfoDspVersion] = new(nameof(libVersion), libVersion, true),
            [CoreStringBuilder.EmbedAzzyInfoGitHubCommit] = new(nameof(commit), commit, false),
            [CoreStringBuilder.EmbedAzzyInfoCompDate] = new(nameof(compilationDate), compilationDate, false),
            [CoreStringBuilder.EmbedAzzyInfoEnv] = new(nameof(botEnvironment), botEnvironment, false),
            [CoreStringBuilder.EmbedAzzyInfoModules] = new(nameof(activatedModules), activatedModules, false)
        };

        return CreateBasicEmbed(title, string.Empty, userName, userAvatarUrl, DiscordColor.Red, userAvatarUrl, string.Empty, fields);
    }
}
