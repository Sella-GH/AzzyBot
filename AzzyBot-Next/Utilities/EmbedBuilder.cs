using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzzyBot.Database.Entities;
using AzzyBot.Utilities.Encryption;
using AzzyBot.Utilities.Records;
using AzzyBot.Utilities.Records.AzuraCast;
using DSharpPlus.Entities;

namespace AzzyBot.Utilities;

public static class EmbedBuilder
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

    public static async Task<DiscordEmbed> BuildAzzyHardwareStatsEmbedAsync(Uri avaUrl)
    {
        const string title = "AzzyBot Hardware Stats";
        const string notLinux = "To display more information you need to have a linux os.";
        string os = AzzyStatsHardware.GetSystemOs;
        string osArch = AzzyStatsHardware.GetSystemOsArch;
        string isDocker = AzzyStatsHardware.CheckIfDocker.ToString();
        long uptime = Converter.ConvertToUnixTime(AzzyStatsHardware.GetSystemUptime);

        Dictionary<string, DiscordEmbedRecord> fields = new()
        {
            ["Operating System"] = new(os, true),
            ["Architecture"] = new(osArch, true),
            ["Is Docker?"] = new(isDocker, true),
            ["System Uptime"] = new($"<t:{uptime}>", false)
        };

        if (!AzzyStatsHardware.CheckIfLinuxOs)
            return CreateBasicEmbed(title, null, DiscordColor.Orange, null, notLinux, null, fields);

        Dictionary<int, double> cpuUsage = await AzzyStatsHardware.GetSystemCpuAsync();
        Dictionary<string, double> cpuTemp = await AzzyStatsHardware.GetSystemCpuTempAsync();
        CpuLoadRecord cpuLoads = await AzzyStatsHardware.GetSystemCpuLoadAsync();
        MemoryUsageRecord memory = await AzzyStatsHardware.GetSystemMemoryUsageAsync();
        DiskUsageRecord disk = AzzyStatsHardware.GetSystemDiskUsage();
        Dictionary<string, NetworkSpeedRecord> networkUsage = await AzzyStatsHardware.GetSystemNetworkUsageAsync();

        if (cpuUsage.Count > 0)
        {
            StringBuilder cpuUsageBuilder = new();
            foreach (KeyValuePair<int, double> kvp in cpuUsage)
            {
                int counter = kvp.Key;

                if (counter == 0)
                {
                    cpuUsageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Total usage: **{kvp.Value}**%");
                    continue;
                }

                cpuUsageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Core {counter}: **{kvp.Value}**%");
            }

            fields.Add("CPU Usage", new(cpuUsageBuilder.ToString(), false));
        }

        if (cpuTemp.Count > 0)
        {
            StringBuilder cpuTempBuilder = new();
            foreach (KeyValuePair<string, double> kvp in cpuTemp)
            {
                cpuTempBuilder.AppendLine(CultureInfo.InvariantCulture, $"{kvp.Key}: **{kvp.Value}** °C");
            }

            fields.Add("Temperatures", new(cpuTempBuilder.ToString(), false));
        }

        if (cpuLoads is not null)
        {
            string cpuLoad = $"1-Min-Load: **{cpuLoads.OneMin}**\n5-Min-Load: **{cpuLoads.FiveMin}**\n15-Min-Load: **{cpuLoads.FifteenMin}**";
            fields.Add("CPU Load", new(cpuLoad, true));
        }

        if (memory is not null)
        {
            string memoryUsage = $"Total: **{memory.Total}** GB\nUsed: **{memory.Used}** GB\nFree: **{Math.Round(memory.Total - memory.Used, 2)}** GB";
            fields.Add("Memory Usage", new(memoryUsage, true));
        }

        if (disk is not null)
        {
            string diskUsage = $"Total: **{disk.TotalSize}** GB\nUsed: **{disk.TotalUsedSpace}** GB\nFree: **{disk.TotalFreeSpace}** GB";
            fields.Add("Disk Usage", new(diskUsage, true));
        }

        if (networkUsage.Count > 0)
        {
            StringBuilder networkUsageBuilder = new();
            foreach (KeyValuePair<string, NetworkSpeedRecord> kvp in networkUsage)
            {
                networkUsageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Interface: **{kvp.Key}**\nReceived: **{kvp.Value.Received}** KB/s\nTransmitted: **{kvp.Value.Transmitted}** KB/s\n");
            }

            fields.Add("Network Usage", new(networkUsageBuilder.ToString(), false));
        }

        return CreateBasicEmbed(title, null, DiscordColor.Orange, avaUrl, null, null, fields);
    }

    public static DiscordEmbed BuildAzzyHelpEmbed(AzzyHelpRecord command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));

        string title = command.Name;
        string description = command.Description;

        Dictionary<string, DiscordEmbedRecord> fields = [];
        foreach (KeyValuePair<string, string> kvp in command.Parameters)
        {
            fields.Add(kvp.Key, new(kvp.Value, false));
        }

        return CreateBasicEmbed(title, description, DiscordColor.Blurple, null, null, null, fields);
    }

    public static DiscordEmbed BuildAzzyHelpEmbed(IReadOnlyList<AzzyHelpRecord> commands)
    {
        ArgumentNullException.ThrowIfNull(commands, nameof(commands));
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

    public static DiscordEmbed BuildAzzyInfoStatsEmbed(Uri avaUrl, string dspVersion, string commit, in DateTime compileDate, int loc)
    {
        const string title = "AzzyBot Informational Stats";
        const string githubUrl = "https://github.com/Sella-GH";
        const string botUrl = $"{githubUrl}/AzzyBot";
        const string commitUrl = $"{botUrl}/commit";
        const string contribUrl = $"{botUrl}/graphs/contributors";
        string[] authors = AzzyStatsSoftware.GetBotAuthors.Split(',');
        string sourceCode = $"{loc} lines";
        string formattedAuthors = $"- [{authors[0].Trim()}]({githubUrl})\n- [{authors[1].Trim()}]({contribUrl})";
        string formattedCommit = $"[{commit}]({commitUrl}/{commit})";

        Dictionary<string, DiscordEmbedRecord> fields = new()
        {
            // Row 1
            ["Name"] = new(AzzyStatsSoftware.GetBotName, true),

            // Row 2
            ["Uptime"] = new($"<t:{Converter.ConvertToUnixTime(AzzyStatsSoftware.GetBotUptime())}>", false),

            // Row 3
            ["Bot Version"] = new(AzzyStatsSoftware.GetBotVersion, true),
            [".NET Version"] = new(AzzyStatsSoftware.GetBotDotNetVersion, true),
            ["D#+ Version"] = new(dspVersion, true),

            // Row 4
            ["Authors"] = new(formattedAuthors, true),
            ["Repository"] = new($"[GitHub]({botUrl})", true),
            ["Environment"] = new(AzzyStatsSoftware.GetBotEnvironment, true),

            // Row 5
            ["Language"] = new("C# 12.0", true),
            ["Source Code"] = new(sourceCode, true),
            ["Memory Usage"] = new($"{AzzyStatsSoftware.GetBotMemoryUsage()} GB", true),

            // Row 6
            ["Compilation Date"] = new($"<t:{Converter.ConvertToUnixTime(compileDate)}>", false),

            // Row 7
            ["AzzyBot GitHub Commit"] = new(formattedCommit, false)
        };

        return CreateBasicEmbed(title, null, DiscordColor.Orange, avaUrl, null, null, fields);
    }

    public static DiscordEmbed BuildAzzyUpdatesAvailableEmbed(string version, in DateTime updateDate, Uri url)
    {
        ArgumentNullException.ThrowIfNull(version, nameof(version));

        const string title = "Azzy Updates Available";
        const string description = "Update now to get the latest bug fixes, features and improvements!";
        string yourVersion = AzzyStatsSoftware.GetBotVersion;

        Dictionary<string, DiscordEmbedRecord> fields = new()
        {
            ["Release Date"] = new($"<t:{Converter.ConvertToUnixTime(updateDate)}>"),
            ["Your Version"] = new(yourVersion),
            ["New Version"] = new(version)
        };

        return CreateBasicEmbed(title, description, DiscordColor.White, null, null, url, fields);
    }

    public static DiscordEmbed BuildAzzyUpdatesChangelogEmbed(string changelog, Uri url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(changelog);

        const string title = "Changelog";
        string description = changelog;

        if (title.Length + description.Length > 6000)
            description = $"The changelog is too big to display it in an Embed, you can view it [here]({url}).";

        return CreateBasicEmbed(title, description, DiscordColor.White);
    }

    public static DiscordEmbed BuildAzzyUpdatesInstructionsEmbed()
    {
        bool isLinux = AzzyStatsHardware.CheckIfLinuxOs;
        bool isWindows = AzzyStatsHardware.CheckIfWindowsOs;
        const string title = "Update Instructions";
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

    public static DiscordEmbed BuildGetSettingsGuildEmbed(string serverName, GuildsEntity guild)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName, nameof(serverName));

        const string title = "Settings Overview";
        string description = $"Here are all settings which are currently set for **{serverName}**";

        Dictionary<string, DiscordEmbedRecord> fields = new()
        {
            ["Server ID"] = new(guild.UniqueId.ToString(CultureInfo.InvariantCulture)),
            ["Error Channel"] = new((guild.ErrorChannelId > 0) ? $"<#{guild.ErrorChannelId}>" : "Not set"),
            ["Configuration Complete"] = new(guild.ConfigSet.ToString()),
            ["AzuraCast Activated"] = new(guild.AzuraCastSet.ToString())
        };

        return CreateBasicEmbed(title, description, DiscordColor.White, null, null, null, fields);
    }

    public static IReadOnlyList<DiscordEmbed> BuildGetSettingsAzuraEmbed(AzuraCastEntity azuraCast)
    {
        ArgumentNullException.ThrowIfNull(azuraCast, nameof(azuraCast));

        const string title = "AzuraCast Settings";
        List<DiscordEmbed> embeds = [];
        Dictionary<string, DiscordEmbedRecord> fields = new()
        {
            ["Base Url"] = new($"||{((!string.IsNullOrWhiteSpace(azuraCast.BaseUrl)) ? Crypto.Decrypt(azuraCast.BaseUrl) : "Not set")}||"),
            ["Outages Channel"] = new((azuraCast.OutagesChannelId > 0) ? $"<#{azuraCast.OutagesChannelId}>" : "Not set")
        };

        embeds.Add(CreateBasicEmbed(title, string.Empty, DiscordColor.White, null, null, null, fields));

        const string stationTitle = "AzuraCast Stations";
        foreach (AzuraCastStationEntity station in azuraCast.Stations)
        {
            fields = new()
            {
                ["Station Name"] = new(Crypto.Decrypt(station.Name)),
                ["Station ID"] = new(station.StationId.ToString(CultureInfo.InvariantCulture)),
                ["Api key"] = new($"||{((!string.IsNullOrWhiteSpace(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : "Not set")}||"),
                ["Music Requests Channel"] = new((station.RequestsChannelId > 0) ? $"<#{station.RequestsChannelId}>" : "Not set"),
                ["Prefer HLS Streaming"] = new(station.PreferHls.ToString()),
                ["Show Playlist In Now Playing"] = new(station.ShowPlaylistInNowPlaying.ToString()),
                ["Automatic Checks"] = new($"- File Changes: {station.Checks.FileChanges}\n- Server Status: {station.Checks.ServerStatus}\n- Updates: {station.Checks.Updates}\n- Updates Changelog: {station.Checks.UpdatesShowChangelog}"),
                ["Mount Points"] = new((station.Mounts.Count > 0) ? string.Join('\n', station.Mounts.Select(x => $"- {Crypto.Decrypt(x.Name)}: {Crypto.Decrypt(x.Mount)}")) : "No Mount Points added")
            };

            embeds.Add(CreateBasicEmbed(stationTitle, string.Empty, DiscordColor.White, null, null, null, fields));
        }

        return embeds;
    }

    public static DiscordEmbed BuildMusicNowPlayingEmbed(NowPlayingDataRecord data, string? playlistName = null)
    {
        ArgumentNullException.ThrowIfNull(data, nameof(data));

        const string title = "Now Playing";
        string? message = null;
        string thumbnailUrl = (!string.IsNullOrWhiteSpace(data.Live.Art)) ? data.Live.Art : data.NowPlaying.Song.Art;

        Dictionary<string, DiscordEmbedRecord> fields = new()
        {
            ["Song"] = new(data.NowPlaying.Song.Title, true),
            ["By"] = new(data.NowPlaying.Song.Artist.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase), true)
        };

        if (!string.IsNullOrWhiteSpace(data.NowPlaying.Song.Album))
            fields.Add("On", new(data.NowPlaying.Song.Album.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase), true));

        if (data.Live.IsLive)
        {
            message = $"Currently served *live* by the one and only **{data.Live.StreamerName}**";
            fields.Add("Streaming live since", new($"<t:{Converter.ConvertFromUnixTime(Convert.ToInt64(data.Live.BroadcastStart, CultureInfo.InvariantCulture))}>"));
        }
        else
        {
            TimeSpan duration = TimeSpan.FromSeconds(data.NowPlaying.Duration);
            TimeSpan elapsed = TimeSpan.FromSeconds(data.NowPlaying.Elapsed);

            string songDuration = duration.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
            string songElapsed = elapsed.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
            string progressBar = AzuraCastMisc.GetProgressBar(14, elapsed.TotalSeconds, duration.TotalSeconds);

            fields.Add("Duration", new($"{progressBar} `[{songElapsed} / {songDuration}]`"));

            if (!string.IsNullOrWhiteSpace(playlistName))
                fields.Add("Playlist", new(playlistName));
        }

        return CreateBasicEmbed(title, message, DiscordColor.Aquamarine, new(thumbnailUrl), null, null, fields);
    }
}
