﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AzzyBot.Database.Entities;
using AzzyBot.Utilities.Records;
using DSharpPlus.Entities;

namespace AzzyBot.Utilities;

internal static class EmbedBuilder
{
    private static DiscordEmbedBuilder CreateBasicEmbed(string title, string? description = null, DiscordColor? color = null, Uri? thumbnailUrl = null, string? footerText = null, Uri? url = null, Dictionary<string, DiscordEmbedRecord>? fields = null)
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

        if (thumbnailUrl is not null)
            builder.WithThumbnail(thumbnailUrl);

        if (!string.IsNullOrWhiteSpace(footerText))
            builder.WithFooter(footerText);

        if (url is not null)
            builder.WithUrl(url);

        if (fields is not null)
        {
            foreach (KeyValuePair<string, DiscordEmbedRecord> field in fields)
            {
                builder.AddField(field.Key, field.Value.Description, field.Value.IsInline);
            }
        }

        return builder;
    }

    internal static DiscordEmbed BuildAzzyHardwareStatsEmbed(Uri avaUrl, string os, string osArch, string isDocker, long sysUptime, Dictionary<int, double> cpuUsages, CpuLoadRecord cpuLoads, MemoryUsageRecord memory, DiskUsageRecord disk, Dictionary<string, NetworkSpeedRecord> networkUsage)
    {
        const string title = "AzzyBot Hardware Stats";
        const string notLinux = "To display more information you need to have a linux os.";

        Dictionary<string, DiscordEmbedRecord> fields = new()
        {
            ["Operating System"] = new(os, true),
            ["Architecture"] = new(osArch, true),
            ["Is Dockerized"] = new(isDocker, true),
            ["System Uptime"] = new($"<t:{sysUptime}>", false)
        };

        Console.WriteLine($"OS: {os}");
        Console.WriteLine($"Arch: {osArch}");
        Console.WriteLine($"isDocker: {isDocker}");
        Console.WriteLine($"Uptime: {sysUptime}");

        if (!AzzyStatsHardware.CheckIfLinuxOs)
            return CreateBasicEmbed(title, null, DiscordColor.Orange, null, notLinux, null, fields);

        StringBuilder cpuUsage = new();
        foreach (KeyValuePair<int, double> kvp in cpuUsages)
        {
            int counter = kvp.Key;

            if (counter == 0)
            {
                cpuUsage.AppendLine(CultureInfo.InvariantCulture, $"Total usage: **{kvp.Value}**%");
                continue;
            }

            cpuUsage.AppendLine(CultureInfo.InvariantCulture, $"Core {counter}: **{kvp.Value}**%");
        }

        fields.Add("CPU Usage", new(cpuUsage.ToString(), false));
        Console.WriteLine($"CPU Usage: {cpuUsage}");

        string cpuLoad = $"1-Min-Load: **{cpuLoads.OneMin}**\n5-Min-Load: **{cpuLoads.FiveMin}**\n15-Min-Load: **{cpuLoads.FifteenMin}**";
        fields.Add("CPU Load", new(cpuLoad, false));
        Console.WriteLine($"CPU Load: {cpuLoad}");

        string memoryUsage = $"Total: **{memory.Total}** GB\nUsed: **{memory.Used}** GB\nFree: **{memory.Total - memory.Used}** GB";
        fields.Add("Memory Usage", new(memoryUsage, false));
        Console.WriteLine($"Memory Usage: {memoryUsage}");

        string diskUsage = $"Total: **{disk.TotalSize}** GB\nUsed: **{disk.TotalUsedSpace}** GB\nFree: **{disk.TotalFreeSpace}** GB";
        fields.Add("Disk Usage", new(diskUsage, false));
        Console.WriteLine($"Disk Usage: {diskUsage}");

        StringBuilder networkUsageBuilder = new();
        foreach (KeyValuePair<string, NetworkSpeedRecord> kvp in networkUsage)
        {
            networkUsageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Interface: **{kvp.Key}** KB/s\nReceived: **{kvp.Value.Received}** KB/s\nTransmitted: **{kvp.Value.Transmitted}** KB/s");
        }

        fields.Add("Network Usage", new(networkUsageBuilder.ToString(), false));
        Console.WriteLine($"Network Usage: {networkUsageBuilder}");

        return CreateBasicEmbed(title, null, DiscordColor.Orange, avaUrl, null, null, fields);
    }

    internal static DiscordEmbed BuildAzzyHelpEmbed(AzzyHelpRecord command)
    {
        string title = command.Name;
        string description = command.Description;

        Dictionary<string, DiscordEmbedRecord> fields = [];
        foreach (KeyValuePair<string, string> kvp in command.Parameters)
        {
            fields.Add(kvp.Key, new(kvp.Value, false));
        }

        return CreateBasicEmbed(title, description, DiscordColor.Blurple, null, null, null, fields);
    }

    internal static DiscordEmbed BuildAzzyHelpEmbed(List<AzzyHelpRecord> commands)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(commands.Count, nameof(commands));

        const string preTitle = "Command List For";
        string title = $"{preTitle} {commands[0].SubCommand} Module";

        Dictionary<string, DiscordEmbedRecord> fields = [];
        foreach (AzzyHelpRecord command in commands)
        {
            fields.Add(command.Name, new(command.Description));
        }

        return CreateBasicEmbed(title, null, DiscordColor.Blurple, null, null, null, fields);
    }

    internal static DiscordEmbed BuildAzzyUpdatesAvailableEmbed(Version version, in DateTime updateDate, Uri url)
    {
        const string title = "Azzy Updates Available";
        const string description = "Update now to get the latest bug fixes, features and improvements!";
        string yourVersion = AzzyStatsSoftware.GetBotVersion;

        Dictionary<string, DiscordEmbedRecord> fields = new()
        {
            ["Release Date"] = new($"<t:{Converter.ConvertToUnixTime(updateDate)}>"),
            ["Your version"] = new(yourVersion),
            ["New version"] = new(version.ToString())
        };

        return CreateBasicEmbed(title, description, DiscordColor.White, null, null, url, fields);
    }

    internal static DiscordEmbed BuildAzzyUpdatesChangelogEmbed(string changelog, Uri url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(changelog);

        const string title = "Changelog";
        string description = changelog;

        if (title.Length + description.Length > 6000)
            description = $"The changelog is too big to display it in an Embed, you can view it [here]({url}).";

        return CreateBasicEmbed(title, description, DiscordColor.White);
    }

    internal static DiscordEmbed BuildAzzyUpdatesInstructionsEmbed()
    {
        bool isLinux = AzzyStatsHardware.CheckIfLinuxOs;
        bool isWindows = AzzyStatsHardware.CheckIfWindowsOs;
        const string title = "Update instructions";
        string description = "Please follow the instructions inside the [wiki](https://github.com/Sella-GH/AzzyBot/wiki/Docker-Update-Instructions).";

        if (isLinux)
        {
            description = description.Replace("Docker", "Linux", StringComparison.OrdinalIgnoreCase);
        }
        else if (isWindows)
        {
            description = description.Replace("Docker", "Windows", StringComparison.OrdinalIgnoreCase);
        }

        return CreateBasicEmbed(title, description, DiscordColor.White);
    }

    internal static DiscordEmbed BuildGetSettingsGuildEmbed(string serverName, GuildsEntity guild)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName, nameof(serverName));

        const string title = "Settings overview";
        string description = $"Here are all settings which are currently set for {serverName}";

        Dictionary<string, DiscordEmbedRecord> fields = new()
        {
            ["Server ID"] = new(guild.UniqueId.ToString(CultureInfo.InvariantCulture)),
            ["Error channel"] = new((guild.ErrorChannelId > 0) ? $"<#{guild.ErrorChannelId}>" : "Not set"),
            ["Configuration complete"] = new(guild.ConfigSet.ToString())
        };

        return CreateBasicEmbed(title, description, DiscordColor.White, null, null, null, fields);
    }

    internal static DiscordEmbed BuildGetSettingsAzuraEmbed(AzuraCastEntity azuraCast)
    {
        const string title = "AzuraCast settings";

        Dictionary<string, DiscordEmbedRecord> fields = new()
        {
            ["API Key"] = new($"||{((!string.IsNullOrWhiteSpace(azuraCast.ApiKey)) ? azuraCast.ApiKey : "Not set")}||"),
            ["API URL"] = new($"||{((!string.IsNullOrWhiteSpace(azuraCast.ApiUrl)) ? azuraCast.ApiUrl : "Not set")}||"),
            ["Station ID"] = new($"{((azuraCast.StationId > 0) ? azuraCast.StationId : "Not set")}"),
            ["Music Requests Channel"] = new((azuraCast.MusicRequestsChannelId > 0) ? $"<#{azuraCast.MusicRequestsChannelId}>" : "Not set"),
            ["Outages Channel"] = new((azuraCast.OutagesChannelId > 0) ? $"<#{azuraCast.OutagesChannelId}>" : "Not set"),
            ["Show Playlist In Now Playing"] = new(azuraCast.ShowPlaylistInNowPlaying.ToString())
        };

        return CreateBasicEmbed(title, string.Empty, DiscordColor.White, null, null, null, fields);
    }

    internal static DiscordEmbed BuildGetSettingsAzuraChecksEmbed(AzuraCastChecksEntity checks)
    {
        const string title = "AzuraCast Checks settings";

        Dictionary<string, DiscordEmbedRecord> fields = new()
        {
            ["File Changes"] = new(checks.FileChanges.ToString()),
            ["Server Status"] = new(checks.ServerStatus.ToString()),
            ["Updates"] = new(checks.Updates.ToString()),
            ["Updates Changelog"] = new(checks.UpdatesShowChangelog.ToString())
        };

        return CreateBasicEmbed(title, string.Empty, DiscordColor.White, null, null, null, fields);
    }
}
