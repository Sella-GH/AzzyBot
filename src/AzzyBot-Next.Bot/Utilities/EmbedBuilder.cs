using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzzyBot.Bot.Utilities.Records;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Core.Utilities.Enums;
using AzzyBot.Core.Utilities.Records;
using AzzyBot.Data.Entities;
using DSharpPlus.Entities;

namespace AzzyBot.Bot.Utilities;

public static class EmbedBuilder
{
    private static readonly Uri AzuraCastPic = new("https://raw.githubusercontent.com/AzuraCast/AzuraCast/main/resources/icon.png");
    private static readonly Uri AzuraCastRollingUrl = new("https://github.com/AzuraCast/AzuraCast/commits/main");
    private static readonly Uri AzuraCastStableUrl = new("https://github.com/AzuraCast/AzuraCast/blob/main/CHANGELOG.md");

    private static DiscordEmbedBuilder CreateBasicEmbed(string title, string? description = null, DiscordColor? color = null, Uri? thumbnailUrl = null, string? footerText = null, Uri? url = null, Dictionary<string, AzzyDiscordEmbedRecord>? fields = null)
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
            foreach (KeyValuePair<string, AzzyDiscordEmbedRecord> field in fields)
            {
                builder.AddField(field.Key, field.Value.Description, field.Value.IsInline);
            }
        }

        return builder;
    }

    public static DiscordEmbed BuildAzuraCastFileChangesEmbed(string stationName, int added, int removed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stationName, nameof(stationName));

        const string title = "File Changes";
        string addedFiles = "**No** files were added.";
        string removedFiles = "**No** files were removed.";

        if (added == 1)
        {
            addedFiles = "**1** file was added.";
        }
        else if (added > 1)
        {
            addedFiles = $"**{added}** files were added.";
        }

        if (removed == 1)
        {
            removedFiles = "**1** file was removed.";
        }
        else if (removed > 1)
        {
            removedFiles = $"**{removed}** files were removed.";
        }

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new()
        {
            ["Station"] = new(stationName),
            ["Added"] = new(addedFiles),
            ["Removed"] = new(removedFiles)
        };

        return CreateBasicEmbed(title, null, DiscordColor.Orange, null, null, null, fields);
    }

    public static DiscordEmbed BuildAzuraCastHardwareStatsEmbed(AzuraHardwareStatsRecord stats)
    {
        ArgumentNullException.ThrowIfNull(stats, nameof(stats));

        const string title = "AzuraCast Hardware Stats";
        StringBuilder cpuUsage = new();
        StringBuilder cpuLoads = new();
        StringBuilder memoryUsage = new();
        StringBuilder diskUsage = new();
        StringBuilder networkUsage = new();

        Dictionary<string, AzzyDiscordEmbedRecord> fields = [];
        fields.Add("Ping", new($"{stats.Ping} ms"));

        cpuUsage.AppendLine(CultureInfo.InvariantCulture, $"IO-Wait: **{stats.Cpu.Total.IoWait}**%");
        cpuUsage.AppendLine(CultureInfo.InvariantCulture, $"Stolen: **{stats.Cpu.Total.Steal}**%");
        cpuUsage.AppendLine(CultureInfo.InvariantCulture, $"Total: **{stats.Cpu.Total.Usage}**%");

        for (int i = 0; i < stats.Cpu.Cores.Count; i++)
        {
            cpuUsage.AppendLine(CultureInfo.InvariantCulture, $"Core {i + 1}: **{stats.Cpu.Cores[i].Usage}**%");
        }

        fields.Add("CPU Usage", new(cpuUsage.ToString()));

        cpuLoads.AppendLine(CultureInfo.InvariantCulture, $"1-Min: **{Math.Round(stats.Cpu.Load[0], 2)}**");
        cpuLoads.AppendLine(CultureInfo.InvariantCulture, $"5-Min: **{Math.Round(stats.Cpu.Load[1], 2)}**");
        cpuLoads.AppendLine(CultureInfo.InvariantCulture, $"15-Min: **{Math.Round(stats.Cpu.Load[2], 2)}**");
        fields.Add("CPU Load", new(cpuLoads.ToString(), true));

        memoryUsage.AppendLine(CultureInfo.InvariantCulture, $"Total: **{stats.Memory.Readable.Total}**");
        memoryUsage.AppendLine(CultureInfo.InvariantCulture, $"Used: **{stats.Memory.Readable.Used}**");
        memoryUsage.AppendLine(CultureInfo.InvariantCulture, $"Cached: **{stats.Memory.Readable.Cached}**");
        memoryUsage.AppendLine(CultureInfo.InvariantCulture, $"Free: **{stats.Memory.Readable.Free}**");
        fields.Add("Memory Usage", new(memoryUsage.ToString(), true));

        diskUsage.AppendLine(CultureInfo.InvariantCulture, $"Total: **{stats.Disk.Readable.Total}**");
        diskUsage.AppendLine(CultureInfo.InvariantCulture, $"Used: **{stats.Disk.Readable.Used}**");
        diskUsage.AppendLine(CultureInfo.InvariantCulture, $"Free: **{stats.Disk.Readable.Free}**");
        fields.Add("Disk Usage", new(diskUsage.ToString(), true));

        foreach (AzuraNetworkData network in stats.Network)
        {
            networkUsage.AppendLine(CultureInfo.InvariantCulture, $"Interface: **{network.InterfaceName}**");
            networkUsage.AppendLine(CultureInfo.InvariantCulture, $"Received: **{network.Received.Speed.Readable}**");
            networkUsage.AppendLine(CultureInfo.InvariantCulture, $"Transmitted: **{network.Transmitted.Speed.Readable}**");
            networkUsage.AppendLine();
        }

        fields.Add("Network Usage", new(networkUsage.ToString()));

        return CreateBasicEmbed(title, null, DiscordColor.Orange, AzuraCastPic, null, null, fields);
    }

    public static DiscordEmbed BuildAzuraCastMusicNowPlayingEmbed(AzuraNowPlayingDataRecord data, string? playlistName = null)
    {
        ArgumentNullException.ThrowIfNull(data, nameof(data));

        const string title = "Now Playing";
        string? message = null;
        string thumbnailUrl = (!string.IsNullOrWhiteSpace(data.Live.Art)) ? data.Live.Art : data.NowPlaying.Song.Art;

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new()
        {
            ["Title"] = new(data.NowPlaying.Song.Title),
            ["By"] = new(data.NowPlaying.Song.Artist.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase))
        };

        if (!string.IsNullOrWhiteSpace(data.NowPlaying.Song.Album))
            fields.Add("On", new(data.NowPlaying.Song.Album.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(data.NowPlaying.Song.Genre))
            fields.Add("Genre", new(data.NowPlaying.Song.Genre));

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
            string progressBar = Misc.GetProgressBar(14, elapsed.TotalSeconds, duration.TotalSeconds);

            fields.Add("Duration", new($"{progressBar} `[{songElapsed} / {songDuration}]`"));

            if (!string.IsNullOrWhiteSpace(playlistName))
                fields.Add("Playlist", new(playlistName));
        }

        return CreateBasicEmbed(title, message, DiscordColor.Aquamarine, new(thumbnailUrl), null, null, fields);
    }

    public static DiscordEmbed BuildAzuraCastMusicSearchSongEmbed(AzuraRequestRecord song, bool isQueued, bool isPlayed)
    {
        ArgumentNullException.ThrowIfNull(song, nameof(song));

        const string title = "Song Search";
        const string description = "Here is the song you requested.";
        string? footerText = null;

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new()
        {
            ["Title"] = new(song.Song.Title),
            ["By"] = new(song.Song.Artist)
        };

        if (!string.IsNullOrWhiteSpace(song.Song.Album))
            fields.Add("On", new(song.Song.Album));

        if (!string.IsNullOrWhiteSpace(song.Song.Genre))
            fields.Add("Genre", new(song.Song.Genre));

        if (!string.IsNullOrWhiteSpace(song.Song.Isrc))
            fields.Add("ISRC", new(song.Song.Isrc));

        if (isQueued)
            footerText = "This song is already queued and will be played soon!";

        if (isPlayed)
            footerText = "This song was played in the last couple of minutes. Give it a break!";

        return CreateBasicEmbed(title, description, DiscordColor.Aquamarine, new(song.Song.Art), footerText, null, fields);
    }

    public static DiscordEmbed BuildAzuraCastUpdatesAvailableEmbed(AzuraUpdateRecord update)
    {
        ArgumentNullException.ThrowIfNull(update, nameof(update));

        const string title = "AzuraCast Updates Available";
        string description = $"Your AzuraCast installation needs **{update.RollingUpdatesList.Count}** updates.";
        if (update.RollingUpdatesList.Count == 1)
            description = "Your AzuraCast installation needs **1** update.";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new()
        {
            ["Current Version"] = new(update.CurrentRelease)
        };

        if ((update.CurrentRelease != update.LatestRelease) && update.NeedsReleaseUpdate)
            fields.Add("Latest Release", new(update.LatestRelease));

        if (update.CanSwitchToStable)
            fields.Add("Stable Switch Available?", new("Yes"));

        return CreateBasicEmbed(title, description, DiscordColor.White, AzuraCastPic, null, null, fields);
    }

    public static DiscordEmbed BuildAzuraCastUpdatesChangelogEmbed(IReadOnlyList<string> changelog, bool isRolling)
    {
        ArgumentNullException.ThrowIfNull(changelog, nameof(changelog));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(changelog.Count, nameof(changelog));

        const string title = "AzuraCast Updates Changelog";

        IEnumerable<string> revChangelog = changelog.Reverse();
        StringBuilder body = new();
        foreach (string line in revChangelog)
        {
            body.AppendLine(CultureInfo.InvariantCulture, $"- {line}");
        }

        if (body.Length > 4096 || title.Length + body.Length > 6000)
            body = new($"The changelog is too big to display it in an Embed, you can view it [here]({((isRolling) ? AzuraCastRollingUrl : AzuraCastStableUrl)}).");

        return CreateBasicEmbed(title, body.ToString(), DiscordColor.White);
    }

    public static DiscordEmbed BuildAzuraCastUploadFileEmbed(AzuraFilesDetailedRecord file, int fileSize, string stationName)
    {
        ArgumentNullException.ThrowIfNull(file, nameof(file));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fileSize, nameof(fileSize));

        const string title = "File Uploaded";
        const string description = "Your song was uploaded successfully.";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new()
        {
            ["Station"] = new(stationName),
            ["Title"] = new(file.Title),
            ["Artist"] = new(file.Artist)
        };

        if (!string.IsNullOrWhiteSpace(file.Album))
            fields.Add("Album", new(file.Album));

        fields.Add("Duration", new(file.Length));

        if (!string.IsNullOrWhiteSpace(file.Genre))
            fields.Add("Genre", new(file.Genre));

        if (!string.IsNullOrWhiteSpace(file.Isrc))
            fields.Add("ISRC", new(file.Isrc));

        fields.Add("File Size", new($"{Math.Round(fileSize / (1024.0 * 1024.0), 2)} MB"));

        return CreateBasicEmbed(title, description, DiscordColor.SpringGreen, new(file.Art), null, null, fields);
    }

    public static async Task<DiscordEmbed> BuildAzzyHardwareStatsEmbedAsync(Uri avaUrl)
    {
        const string title = "AzzyBot Hardware Stats";
        const string notLinux = "To display more information you need to have a linux os.";
        string os = AzzyStatsHardware.GetSystemOs;
        string osArch = AzzyStatsHardware.GetSystemOsArch;
        bool isDocker = AzzyStatsHardware.CheckIfDocker;
        long uptime = Converter.ConvertToUnixTime(AzzyStatsHardware.GetSystemUptime);

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new()
        {
            ["Operating System"] = new(os, true),
            ["Architecture"] = new(osArch, true),
            ["Dockerized?"] = new(Misc.ReadableBool(isDocker, ReadbleBool.YesNo), true),
            ["System Uptime"] = new($"<t:{uptime}>")
        };

        if (!AzzyStatsHardware.CheckIfLinuxOs)
            return CreateBasicEmbed(title, null, DiscordColor.Orange, null, notLinux, null, fields);

        Dictionary<int, double> cpuUsage = await AzzyStatsHardware.GetSystemCpuAsync();
        Dictionary<string, double> cpuTemp = await AzzyStatsHardware.GetSystemCpuTempAsync();
        AzzyCpuLoadRecord cpuLoads = await AzzyStatsHardware.GetSystemCpuLoadAsync();
        AzzyMemoryUsageRecord memory = await AzzyStatsHardware.GetSystemMemoryUsageAsync();
        AzzyDiskUsageRecord disk = AzzyStatsHardware.GetSystemDiskUsage();
        Dictionary<string, AzzyNetworkSpeedRecord> networkUsage = await AzzyStatsHardware.GetSystemNetworkUsageAsync();

        if (cpuTemp.Count > 0)
        {
            StringBuilder cpuTempBuilder = new();
            foreach (KeyValuePair<string, double> kvp in cpuTemp)
            {
                cpuTempBuilder.AppendLine(CultureInfo.InvariantCulture, $"{kvp.Key}: **{kvp.Value} °C**");
            }

            fields.Add("Temperatures", new(cpuTempBuilder.ToString(), false));
        }

        if (cpuUsage.Count > 0)
        {
            StringBuilder cpuUsageBuilder = new();
            foreach (KeyValuePair<int, double> kvp in cpuUsage)
            {
                int counter = kvp.Key;

                if (counter == 0)
                {
                    cpuUsageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Total: **{kvp.Value}%**");
                    continue;
                }

                cpuUsageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Core {counter}: **{kvp.Value}%**");
            }

            fields.Add("CPU Usage", new(cpuUsageBuilder.ToString(), false));
        }

        if (cpuLoads is not null)
        {
            string cpuLoad = $"1-Min: **{cpuLoads.OneMin}**\n5-Min: **{cpuLoads.FiveMin}**\n15-Min: **{cpuLoads.FifteenMin}**";
            fields.Add("CPU Load", new(cpuLoad, true));
        }

        if (memory is not null)
        {
            string memoryUsage = $"Total: **{memory.Total} GB**\nUsed: **{memory.Used} GB**\nFree: **{Math.Round(memory.Total - memory.Used, 2)} GB**";
            fields.Add("Memory Usage", new(memoryUsage, true));
        }

        if (disk is not null)
        {
            string diskUsage = $"Total: **{disk.TotalSize} GB**\nUsed: **{disk.TotalUsedSpace} GB**\nFree: **{disk.TotalFreeSpace} GB**";
            fields.Add("Disk Usage", new(diskUsage, true));
        }

        if (networkUsage.Count > 0)
        {
            StringBuilder networkUsageBuilder = new();
            foreach (KeyValuePair<string, AzzyNetworkSpeedRecord> kvp in networkUsage)
            {
                networkUsageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Interface: **{kvp.Key}**\nReceived: **{kvp.Value.Received} KB**\nTransmitted: **{kvp.Value.Transmitted} KB**\n");
            }

            fields.Add("Network Usage", new(networkUsageBuilder.ToString()));
        }

        return CreateBasicEmbed(title, null, DiscordColor.Orange, avaUrl, null, null, fields);
    }

    public static DiscordEmbed BuildAzzyHelpEmbed(AzzyHelpRecord command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));

        string title = command.Name;
        string description = command.Description;

        Dictionary<string, AzzyDiscordEmbedRecord> fields = [];
        foreach (KeyValuePair<string, string> kvp in command.Parameters)
        {
            fields.Add(kvp.Key, new(kvp.Value));
        }

        return CreateBasicEmbed(title, description, DiscordColor.Blurple, null, null, null, fields);
    }

    public static DiscordEmbed BuildAzzyHelpEmbed(IReadOnlyList<AzzyHelpRecord> commands)
    {
        ArgumentNullException.ThrowIfNull(commands, nameof(commands));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(commands.Count, nameof(commands));

        const string preTitle = "Command List For";

        // Make the first letter an uppercase one and append the rest
        string title = $"{preTitle} {string.Concat(commands[0].SubCommand[0].ToString().ToUpperInvariant(), commands[0].SubCommand.AsSpan(1))} Group";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = [];
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

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new()
        {
            ["Uptime"] = new($"<t:{Converter.ConvertToUnixTime(AzzyStatsSoftware.GetBotUptime().ToLocalTime())}>", true),
            ["Bot Version"] = new(AzzyStatsSoftware.GetBotVersion, true),
            [".NET Version"] = new(AzzyStatsSoftware.GetBotDotNetVersion, true),
            ["D#+ Version"] = new(dspVersion, true),
            ["Authors"] = new(formattedAuthors, true),
            ["Repository"] = new($"[GitHub]({botUrl})", true),
            ["Environment"] = new(AzzyStatsSoftware.GetBotEnvironment, true),
            ["Source Code"] = new(sourceCode, true),
            ["Memory Usage"] = new($"{AzzyStatsSoftware.GetBotMemoryUsage()} GB", true),
            ["Compilation Date"] = new($"<t:{Converter.ConvertToUnixTime(compileDate.ToLocalTime())}>", true),
            ["AzzyBot GitHub Commit"] = new(formattedCommit)
        };

        return CreateBasicEmbed(title, null, DiscordColor.Orange, avaUrl, null, null, fields);
    }

    public static DiscordEmbed BuildAzzyUpdatesAvailableEmbed(string version, in DateTime updateDate, Uri url)
    {
        ArgumentNullException.ThrowIfNull(version, nameof(version));

        const string title = "Azzy Updates Available";
        const string description = "Update now to get the latest bug fixes, features and improvements!";
        string yourVersion = AzzyStatsSoftware.GetBotVersion;

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new()
        {
            ["Release Date"] = new($"<t:{Converter.ConvertToUnixTime(updateDate.ToLocalTime())}>"),
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

    public static DiscordEmbed BuildGetSettingsGuildEmbed(string serverName, GuildEntity guild, string adminRole)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName, nameof(serverName));

        const string title = "Settings Overview";
        string description = $"Here are all settings which are currently set for **{serverName}**";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new()
        {
            ["Server ID"] = new(guild.UniqueId.ToString(CultureInfo.InvariantCulture)),
            ["Admin Role"] = new((!string.IsNullOrWhiteSpace(adminRole?.Trim()) && adminRole.Trim() is not "()") ? adminRole.Trim() : "Not set"),
            ["Admin Notify Channel"] = new((guild.AdminNotifyChannelId > 0) ? $"<#{guild.AdminNotifyChannelId}>" : "Not set"),
            ["Error Channel"] = new((guild.ErrorChannelId > 0) ? $"<#{guild.ErrorChannelId}>" : "Not set"),
            ["Configuration Complete"] = new(Misc.ReadableBool(guild.ConfigSet, ReadbleBool.YesNo))
        };

        return CreateBasicEmbed(title, description, DiscordColor.White, null, null, null, fields);
    }

    public static IReadOnlyList<DiscordEmbed> BuildGetSettingsAzuraEmbed(AzuraCastEntity azuraCast, string instanceRole, IReadOnlyDictionary<ulong, string> stationRoles)
    {
        ArgumentNullException.ThrowIfNull(azuraCast, nameof(azuraCast));

        const string title = "AzuraCast Settings";
        List<DiscordEmbed> embeds = [];
        Dictionary<string, AzzyDiscordEmbedRecord> fields = new()
        {
            ["Base Url"] = new($"||{((!string.IsNullOrWhiteSpace(azuraCast.BaseUrl)) ? Crypto.Decrypt(azuraCast.BaseUrl) : "Not set")}||"),
            ["Admin Api Key"] = new($"||{((!string.IsNullOrWhiteSpace(azuraCast.AdminApiKey)) ? Crypto.Decrypt(azuraCast.AdminApiKey) : "Not set")}||"),
            ["Instance Admin Role"] = new((!string.IsNullOrWhiteSpace(instanceRole?.Trim()) && instanceRole.Trim() is not "()") ? instanceRole.Trim() : "Not set"),
            ["Notification Channel"] = new((azuraCast.NotificationChannelId > 0) ? $"<#{azuraCast.NotificationChannelId}>" : "Not set"),
            ["Outages Channel"] = new((azuraCast.OutagesChannelId > 0) ? $"<#{azuraCast.OutagesChannelId}>" : "Not set"),
            ["Automatic Checks"] = new($"- Server Status: {Misc.ReadableBool(azuraCast.Checks.ServerStatus, ReadbleBool.EnabledDisabled)}\n- Updates: {Misc.ReadableBool(azuraCast.Checks.Updates, ReadbleBool.EnabledDisabled)}\n- Updates Changelog: {Misc.ReadableBool(azuraCast.Checks.UpdatesShowChangelog, ReadbleBool.EnabledDisabled)}")
        };

        embeds.Add(CreateBasicEmbed(title, string.Empty, DiscordColor.White, null, null, null, fields));

        const string stationTitle = "AzuraCast Stations";
        foreach (AzuraCastStationEntity station in azuraCast.Stations)
        {
            string stationName = Crypto.Decrypt(station.Name);
            string stationId = station.StationId.ToString(CultureInfo.InvariantCulture);
            string stationApiKey = $"||{((!string.IsNullOrWhiteSpace(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : "Not set")}||";
            string stationAdminRole;
            if (station.StationAdminRoleId > 0)
            {
                string role = stationRoles.FirstOrDefault(x => x.Key == station.StationAdminRoleId).Value;
                ulong roleId = stationRoles.FirstOrDefault(x => x.Key == station.StationAdminRoleId).Key;
                stationAdminRole = (role is not null) ? $"{role} ({roleId})" : "Not set";
            }
            else
            {
                stationAdminRole = "Not set";
            }

            string stationDjRole;
            if (station.StationDjRoleId > 0)
            {
                string role = stationRoles.FirstOrDefault(x => x.Key == station.StationDjRoleId).Value;
                ulong roleId = stationRoles.FirstOrDefault(x => x.Key == station.StationDjRoleId).Key;
                stationDjRole = (role is not null) ? $"{role} ({roleId})" : "Not set";
            }
            else
            {
                stationDjRole = "Not set";
            }

            string fileUploadChannel = (station.FileUploadChannelId > 0) ? $"<#{station.FileUploadChannelId}>" : "Not set";
            string requestsChannel = (station.RequestsChannelId > 0) ? $"<#{station.RequestsChannelId}>" : "Not set";
            string fileUploadPath = (!string.IsNullOrWhiteSpace(station.FileUploadPath)) ? station.FileUploadPath : "Not set";
            string preferHls = Misc.ReadableBool(station.PreferHls, ReadbleBool.EnabledDisabled);
            string showPlaylist = Misc.ReadableBool(station.ShowPlaylistInNowPlaying, ReadbleBool.EnabledDisabled);
            string fileChanges = Misc.ReadableBool(station.Checks.FileChanges, ReadbleBool.EnabledDisabled);
            string mounts = (station.Mounts.Count > 0) ? string.Join('\n', station.Mounts.Select(x => $"- {Crypto.Decrypt(x.Name)}: {Crypto.Decrypt(x.Mount)}")) : "No Mount Points added";

            fields = new()
            {
                ["Station Name"] = new(stationName),
                ["Station ID"] = new(stationId),
                ["Station Api Key"] = new(stationApiKey),
                ["Station Admin Role"] = new(stationAdminRole),
                ["Station DJ Role"] = new(stationDjRole),
                ["File Upload Channel"] = new(fileUploadChannel),
                ["Music Requests Channel"] = new(requestsChannel),
                ["File Upload Path"] = new(fileUploadPath),
                ["Prefer HLS Streaming"] = new(preferHls),
                ["Show Playlist In Now Playing"] = new(showPlaylist),
                ["Automatic Checks"] = new($"- File Changes: {fileChanges}"),
                ["Mount Points"] = new(mounts)
            };

            embeds.Add(CreateBasicEmbed(stationTitle, string.Empty, DiscordColor.White, null, null, null, fields));
        }

        return embeds;
    }

    public static DiscordEmbed BuildGuildAddedEmbed(DiscordGuild guild, bool getInfo = false)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));

        string title = (getInfo) ? "Guild Information" : "Guild Added";
        string description = (getInfo) ? $"Here is everything I know about **{guild.Name}**" : $"I was added to **{guild.Name}**.";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new()
        {
            ["Guild ID"] = new(guild.Id.ToString(CultureInfo.InvariantCulture)),
            ["Creation Date"] = new($"<t:{Converter.ConvertToUnixTime(guild.CreationTimestamp.Date)}>"),
            ["Owner"] = new(guild.Owner.Mention),
            ["Members"] = new(guild.MemberCount.ToString(CultureInfo.InvariantCulture), true)
        };

        Uri? iconUrl = null;
        if (guild.IconUrl is not null)
            iconUrl = new(guild.IconUrl);

        return CreateBasicEmbed(title, description, DiscordColor.Gold, iconUrl, null, null, fields);
    }

    public static DiscordEmbed BuildGuildRemovedEmbed(ulong guildId, DiscordGuild? guild = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(guildId, nameof(guildId));

        const string title = "Guild Removed";
        string description = $"I was removed from **{((!string.IsNullOrWhiteSpace(guild?.Name)) ? guild.Name : guildId)}**.";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = [];
        if (guild is not null)
        {
            fields.Add("Guild ID", new(guild.Id.ToString(CultureInfo.InvariantCulture)));
            fields.Add("Removal Date", new($"<t:{Converter.ConvertToUnixTime(DateTime.Now)}>"));
            fields.Add("Owner", new($"<@!{guild.OwnerId}>"));
        }

        return CreateBasicEmbed(title, description, DiscordColor.Gold, null, null, null, fields);
    }
}
