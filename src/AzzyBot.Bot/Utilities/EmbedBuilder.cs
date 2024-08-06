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
using Lavalink4NET.Players;
using Lavalink4NET.Tracks;

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

        if (added is 1)
        {
            addedFiles = "**1** file was added.";
        }
        else if (added is not 0)
        {
            addedFiles = $"**{added}** files were added.";
        }

        if (removed is 1)
        {
            removedFiles = "**1** file was removed.";
        }
        else if (removed is not 0)
        {
            removedFiles = $"**{removed}** files were removed.";
        }

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new()
        {
            ["Station"] = new(stationName),
            ["Added"] = new(addedFiles),
            ["Removed"] = new(removedFiles)
        };

        return CreateBasicEmbed(title, color: DiscordColor.Orange, fields: fields);
    }

    public static DiscordEmbed BuildAzuraCastHardwareStatsEmbed(AzuraHardwareStatsRecord stats)
    {
        ArgumentNullException.ThrowIfNull(stats, nameof(stats));

        const string title = "AzuraCast Hardware Stats";
        StringBuilder cpuUsage = new();
        StringBuilder cpuLoads = new();
        StringBuilder memoryUsage = new();
        StringBuilder diskUsage = new();

        // 5 is the initial count of fields which are def added
        int init = 5 + ((stats.Network.Count > 20) ? 20 : stats.Network.Count);
        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(init)
        {
            { "Ping", new($"{stats.Ping} ms") }
        };

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
            if (fields.Count is 25)
                break;

            fields.Add($"Interface: {network.InterfaceName}", new($"Received: **{network.Received.Speed.Readable}**\nTransmitted: **{network.Transmitted.Speed.Readable}**", true));
        }

        return CreateBasicEmbed(title, color: DiscordColor.Orange, thumbnailUrl: AzuraCastPic, fields: fields);
    }

    public static DiscordEmbed BuildAzuraCastMusicNowPlayingEmbed(AzuraNowPlayingDataRecord data, string? playlistName = null)
    {
        ArgumentNullException.ThrowIfNull(data, nameof(data));

        const string title = "Now Playing";
        string? message = null;
        string thumbnailUrl = (!string.IsNullOrWhiteSpace(data.Live.Art)) ? data.Live.Art : data.NowPlaying.Song.Art;

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(8)
        {
            ["Station"] = new(data.Station.Name),
            ["Title"] = new(data.NowPlaying.Song.Title, true),
            ["By"] = new(data.NowPlaying.Song.Artist.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase), true)
        };

        if (!string.IsNullOrWhiteSpace(data.NowPlaying.Song.Album))
            fields.Add("On", new(data.NowPlaying.Song.Album.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase), true));

        if (!string.IsNullOrWhiteSpace(data.NowPlaying.Song.Genre))
            fields.Add("Genre", new(data.NowPlaying.Song.Genre, true));

        if (data.Live.IsLive)
        {
            message = $"Currently served *live* by the one and only **{data.Live.StreamerName}**";
            fields.Add("Streaming live since", new($"<t:{Converter.ConvertFromUnixTime(Convert.ToInt64(data.Live.BroadcastStart, CultureInfo.InvariantCulture))}>"));
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(playlistName))
                fields.Add("Playlist", new(playlistName, true));

            TimeSpan duration = TimeSpan.FromSeconds(data.NowPlaying.Duration);
            TimeSpan elapsed = TimeSpan.FromSeconds(data.NowPlaying.Elapsed);

            string songDuration = duration.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
            string songElapsed = elapsed.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
            string progressBar = Misc.GetProgressBar(14, elapsed.TotalSeconds, duration.TotalSeconds);

            fields.Add("Duration", new($"{progressBar} `[{songElapsed} / {songDuration}]`"));
        }

        return CreateBasicEmbed(title, message, DiscordColor.Aquamarine, new(thumbnailUrl), fields: fields);
    }

    public static DiscordEmbed BuildAzuraCastMusicSearchSongEmbed(AzuraRequestRecord song, bool isQueued, bool isPlayed)
    {
        ArgumentNullException.ThrowIfNull(song, nameof(song));

        const string title = "Song Search";
        const string description = "Here is the song you requested.";
        string? footerText = null;

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(5)
        {
            ["Title"] = new(song.Song.Title, true),
            ["By"] = new(song.Song.Artist, true)
        };

        if (!string.IsNullOrWhiteSpace(song.Song.Album))
            fields.Add("On", new(song.Song.Album, true));

        if (!string.IsNullOrWhiteSpace(song.Song.Genre))
            fields.Add("Genre", new(song.Song.Genre));

        if (!string.IsNullOrWhiteSpace(song.Song.Isrc))
            fields.Add("ISRC", new(song.Song.Isrc));

        if (isQueued)
            footerText = "This song is already queued and will be played soon!";

        if (isPlayed)
            footerText = "This song was played in the last couple of minutes. Give it a break!";

        return CreateBasicEmbed(title, description, DiscordColor.Aquamarine, new(song.Song.Art), footerText, fields: fields);
    }

    public static DiscordEmbed BuildAzuraCastUpdatesAvailableEmbed(AzuraUpdateRecord update)
    {
        ArgumentNullException.ThrowIfNull(update, nameof(update));

        const string title = "AzuraCast Updates Available";
        string description = $"Your AzuraCast installation needs **{update.RollingUpdatesList.Count}** updates.";
        if (update.RollingUpdatesList.Count == 1)
            description = "Your AzuraCast installation needs **1** update.";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(3)
        {
            ["Current Version"] = new(update.CurrentRelease)
        };

        if ((update.CurrentRelease != update.LatestRelease) && update.NeedsReleaseUpdate)
            fields.Add("Latest Release", new(update.LatestRelease));

        if (update.CanSwitchToStable)
            fields.Add("Stable Switch Available?", new("Yes"));

        return CreateBasicEmbed(title, description, DiscordColor.White, AzuraCastPic, fields: fields);
    }

    public static DiscordEmbed BuildAzuraCastUpdatesChangelogEmbed(IEnumerable<string> changelog, bool isRolling)
    {
        ArgumentNullException.ThrowIfNull(changelog, nameof(changelog));

        const string title = "AzuraCast Updates Changelog";

        StringBuilder body = new();
        foreach (string line in changelog.Reverse())
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

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(8)
        {
            ["Station"] = new(stationName),
            ["Title"] = new(file.Title, true),
            ["Artist"] = new(file.Artist, true)
        };

        if (!string.IsNullOrWhiteSpace(file.Album))
            fields.Add("Album", new(file.Album, true));

        if (!string.IsNullOrWhiteSpace(file.Genre))
            fields.Add("Genre", new(file.Genre, true));

        fields.Add("Duration", new(file.Length, true));

        if (!string.IsNullOrWhiteSpace(file.Isrc))
            fields.Add("ISRC", new(file.Isrc));

        fields.Add("File Size", new($"{Math.Round(fileSize / (1024.0 * 1024.0), 2)} MB"));

        return CreateBasicEmbed(title, description, DiscordColor.SpringGreen, new(file.Art), fields: fields);
    }

    public static async Task<DiscordEmbed> BuildAzzyHardwareStatsEmbedAsync(Uri avaUrl)
    {
        const string title = "AzzyBot Hardware Stats";
        const string notLinux = "To display more information you need to have a linux os.";
        string os = HardwareStats.GetSystemOs;
        string osArch = HardwareStats.GetSystemOsArch;
        bool isDocker = HardwareStats.CheckIfDocker;
        long uptime = Converter.ConvertToUnixTime(HardwareStats.GetSystemUptime);

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(25)
        {
            ["Operating System"] = new(os, true),
            ["Architecture"] = new(osArch, true),
            ["Dockerized?"] = new(Misc.GetReadableBool(isDocker, ReadableBool.YesNo), true),
            ["System Uptime"] = new($"<t:{uptime}>")
        };

        if (!HardwareStats.CheckIfLinuxOs)
            return CreateBasicEmbed(title, color: DiscordColor.Orange, footerText: notLinux, fields: fields);

        Dictionary<int, double> cpuUsage = await HardwareStats.GetSystemCpuAsync();
        Dictionary<string, double> cpuTemp = await HardwareStats.GetSystemCpuTempAsync();
        AppCpuLoadRecord cpuLoads = await HardwareStats.GetSystemCpuLoadAsync();
        AppMemoryUsageRecord memory = await HardwareStats.GetSystemMemoryUsageAsync();
        AppDiskUsageRecord disk = HardwareStats.GetSystemDiskUsage();
        Dictionary<string, AppNetworkSpeedRecord> networkUsage = await HardwareStats.GetSystemNetworkUsageAsync();

        if (cpuTemp.Count > 0)
        {
            StringBuilder cpuTempBuilder = new();
            foreach (KeyValuePair<string, double> kvp in cpuTemp)
            {
                cpuTempBuilder.AppendLine(CultureInfo.InvariantCulture, $"{kvp.Key}: **{kvp.Value} °C**");
            }

            fields.Add("Temperatures", new(cpuTempBuilder.ToString()));
        }

        if (cpuUsage.Count > 0)
        {
            StringBuilder cpuUsageBuilder = new();
            foreach (KeyValuePair<int, double> kvp in cpuUsage)
            {
                int counter = kvp.Key;

                if (counter is 0)
                {
                    cpuUsageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Total: **{kvp.Value}%**");
                    continue;
                }

                cpuUsageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Core {counter}: **{kvp.Value}%**");
            }

            fields.Add("CPU Usage", new(cpuUsageBuilder.ToString()));
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
            foreach (KeyValuePair<string, AppNetworkSpeedRecord> kvp in networkUsage)
            {
                if (fields.Count is 25)
                    break;

                fields.Add($"Interface: {kvp.Key}", new($"Received: **{kvp.Value.Received} KB**\nTransmitted: **{kvp.Value.Transmitted} KB**", true));
            }
        }

        return CreateBasicEmbed(title, color: DiscordColor.Orange, thumbnailUrl: avaUrl, fields: fields);
    }

    public static DiscordEmbed BuildAzzyHelpEmbed(AzzyHelpRecord command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));

        string title = command.Name;
        string description = command.Description;

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(command.Parameters.Count);
        foreach (KeyValuePair<string, string> kvp in command.Parameters)
        {
            fields.Add(kvp.Key, new(kvp.Value));
        }

        return CreateBasicEmbed(title, description, DiscordColor.Blurple, fields: fields);
    }

    public static DiscordEmbed BuildAzzyHelpEmbed(IReadOnlyList<AzzyHelpRecord> commands)
    {
        ArgumentNullException.ThrowIfNull(commands, nameof(commands));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(commands.Count, nameof(commands));

        // Make the first letter an uppercase one and append the rest
        string title = $"Command List For {string.Concat(commands[0].SubCommand[0].ToString().ToUpperInvariant(), commands[0].SubCommand.AsSpan(1))} Group";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(commands.Count);
        foreach (AzzyHelpRecord command in commands)
        {
            fields.Add(command.Name, new(command.Description, true));
        }

        return CreateBasicEmbed(title, color: DiscordColor.Blurple, fields: fields);
    }

    public static DiscordEmbed BuildAzzyInfoStatsEmbed(Uri avaUrl, string dspVersion, string commit, in DateTime compileDate, int loc)
    {
        const string title = "AzzyBot Informational Stats";
        const string githubUrl = "https://github.com/Sella-GH";
        const string botUrl = $"{githubUrl}/AzzyBot";
        const string commitUrl = $"{botUrl}/commit";
        const string contribUrl = $"{botUrl}/graphs/contributors";
        string[] authors = SoftwareStats.GetAppAuthors.Split(',');
        string sourceCode = $"{loc} lines";
        string formattedAuthors = $"- [{authors[0].Trim()}]({githubUrl})\n- [{authors[1].Trim()}]({contribUrl})";
        string formattedCommit = $"[{commit}]({commitUrl}/{commit})";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(11)
        {
            ["Uptime"] = new($"<t:{Converter.ConvertToUnixTime(SoftwareStats.GetAppUptime().ToLocalTime())}>", true),
            ["Bot Version"] = new(SoftwareStats.GetAppVersion, true),
            [".NET Version"] = new(SoftwareStats.GetAppDotNetVersion, true),
            ["D#+ Version"] = new(dspVersion, true),
            ["Authors"] = new(formattedAuthors, true),
            ["Repository"] = new($"[GitHub]({botUrl})", true),
            ["Environment"] = new(SoftwareStats.GetAppEnvironment, true),
            ["Source Code"] = new(sourceCode, true),
            ["Memory Usage"] = new($"{SoftwareStats.GetAppMemoryUsage()} GB", true),
            ["Compilation Date"] = new($"<t:{Converter.ConvertToUnixTime(compileDate.ToLocalTime())}>", true),
            ["AzzyBot GitHub Commit"] = new(formattedCommit)
        };

        return CreateBasicEmbed(title, color: DiscordColor.Orange, thumbnailUrl: avaUrl, fields: fields);
    }

    public static DiscordEmbed BuildAzzyUpdatesAvailableEmbed(string version, in DateTime updateDate, Uri url)
    {
        ArgumentNullException.ThrowIfNull(version, nameof(version));

        const string title = "Azzy Updates Available";
        const string description = "Update now to get the latest bug fixes, features and improvements!";
        string yourVersion = SoftwareStats.GetAppVersion;

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(3)
        {
            ["Release Date"] = new($"<t:{Converter.ConvertToUnixTime(updateDate.ToLocalTime())}>"),
            ["Your Version"] = new(yourVersion),
            ["New Version"] = new(version)
        };

        return CreateBasicEmbed(title, description, DiscordColor.White, url: url, fields: fields);
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
        const string title = "Update Instructions";
        const string description = "Please follow the instructions inside the [wiki](https://github.com/Sella-GH/AzzyBot/wiki/Azzy-2.0.0-Updating).";

        return CreateBasicEmbed(title, description, DiscordColor.White);
    }

    public static DiscordEmbed BuildGetSettingsGuildEmbed(string serverName, GuildEntity guild, string adminRole)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName, nameof(serverName));

        const string title = "Settings Overview";
        string description = $"Here are all settings which are currently set for **{serverName}**";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(5)
        {
            ["Server ID"] = new(guild.UniqueId.ToString(CultureInfo.InvariantCulture)),
            ["Admin Role"] = new((!string.IsNullOrWhiteSpace(adminRole?.Trim()) && adminRole.Trim() is not "()") ? adminRole.Trim() : "Not set"),
            ["Admin Notify Channel"] = new((guild.Preferences.AdminNotifyChannelId > 0) ? $"<#{guild.Preferences.AdminNotifyChannelId}>" : "Not set"),
            ["Error Channel"] = new((guild.Preferences.ErrorChannelId > 0) ? $"<#{guild.Preferences.ErrorChannelId}>" : "Not set"),
            ["Configuration Complete"] = new(Misc.GetReadableBool(guild.ConfigSet, ReadableBool.YesNo))
        };

        return CreateBasicEmbed(title, description, DiscordColor.White, fields: fields);
    }

    public static IEnumerable<DiscordEmbed> BuildGetSettingsAzuraEmbed(AzuraCastEntity azuraCast, string instanceRole, IReadOnlyDictionary<ulong, string> stationRoles, IReadOnlyDictionary<int, string> stationNames)
    {
        ArgumentNullException.ThrowIfNull(azuraCast, nameof(azuraCast));

        const string title = "AzuraCast Settings";
        List<DiscordEmbed> embeds = new(1 + azuraCast.Stations.Count);
        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(6)
        {
            ["Base Url"] = new($"||{((!string.IsNullOrWhiteSpace(azuraCast.BaseUrl)) ? Crypto.Decrypt(azuraCast.BaseUrl) : "Not set")}||"),
            ["Admin Api Key"] = new($"||{((!string.IsNullOrWhiteSpace(azuraCast.AdminApiKey)) ? Crypto.Decrypt(azuraCast.AdminApiKey) : "Not set")}||"),
            ["Instance Admin Role"] = new((!string.IsNullOrWhiteSpace(instanceRole?.Trim()) && instanceRole.Trim() is not "()") ? instanceRole.Trim() : "Not set"),
            ["Notification Channel"] = new((azuraCast.Preferences.NotificationChannelId > 0) ? $"<#{azuraCast.Preferences.NotificationChannelId}>" : "Not set"),
            ["Outages Channel"] = new((azuraCast.Preferences.OutagesChannelId > 0) ? $"<#{azuraCast.Preferences.OutagesChannelId}>" : "Not set"),
            ["Automatic Checks"] = new($"- Server Status: {Misc.GetReadableBool(azuraCast.Checks.ServerStatus, ReadableBool.EnabledDisabled)}\n- Updates: {Misc.GetReadableBool(azuraCast.Checks.Updates, ReadableBool.EnabledDisabled)}\n- Updates Changelog: {Misc.GetReadableBool(azuraCast.Checks.UpdatesShowChangelog, ReadableBool.EnabledDisabled)}")
        };

        embeds.Add(CreateBasicEmbed(title, color: DiscordColor.White, fields: fields));

        const string stationTitle = "AzuraCast Stations";
        foreach (AzuraCastStationEntity station in azuraCast.Stations)
        {
            string stationName = stationNames.FirstOrDefault(x => x.Key == station.Id).Value;
            string stationId = station.StationId.ToString(CultureInfo.InvariantCulture);
            string stationApiKey = $"||{((!string.IsNullOrWhiteSpace(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : "Not set")}||";
            string stationAdminRole;
            if (station.Preferences.StationAdminRoleId > 0)
            {
                string role = stationRoles.FirstOrDefault(x => x.Key == station.Preferences.StationAdminRoleId).Value;
                ulong roleId = stationRoles.FirstOrDefault(x => x.Key == station.Preferences.StationAdminRoleId).Key;
                stationAdminRole = (role is not null) ? $"{role} ({roleId})" : "Not set";
            }
            else
            {
                stationAdminRole = "Not set";
            }

            string stationDjRole;
            if (station.Preferences.StationDjRoleId > 0)
            {
                string role = stationRoles.FirstOrDefault(x => x.Key == station.Preferences.StationDjRoleId).Value;
                ulong roleId = stationRoles.FirstOrDefault(x => x.Key == station.Preferences.StationDjRoleId).Key;
                stationDjRole = (role is not null) ? $"{role} ({roleId})" : "Not set";
            }
            else
            {
                stationDjRole = "Not set";
            }

            string fileUploadChannel = (station.Preferences.FileUploadChannelId > 0) ? $"<#{station.Preferences.FileUploadChannelId}>" : "Not set";
            string requestsChannel = (station.Preferences.RequestsChannelId > 0) ? $"<#{station.Preferences.RequestsChannelId}>" : "Not set";
            string fileUploadPath = (!string.IsNullOrWhiteSpace(station.Preferences.FileUploadPath)) ? station.Preferences.FileUploadPath : "Not set";
            string showPlaylist = Misc.GetReadableBool(station.Preferences.ShowPlaylistInNowPlaying, ReadableBool.EnabledDisabled);
            string fileChanges = Misc.GetReadableBool(station.Checks.FileChanges, ReadableBool.EnabledDisabled);

            fields = new(10)
            {
                ["Station Name"] = new(stationName),
                ["Station ID"] = new(stationId),
                ["Station Api Key"] = new(stationApiKey),
                ["Station Admin Role"] = new(stationAdminRole),
                ["Station DJ Role"] = new(stationDjRole),
                ["File Upload Channel"] = new(fileUploadChannel),
                ["Music Requests Channel"] = new(requestsChannel),
                ["File Upload Path"] = new(fileUploadPath),
                ["Show Playlist In Now Playing"] = new(showPlaylist),
                ["Automatic Checks"] = new($"- File Changes: {fileChanges}")
            };

            embeds.Add(CreateBasicEmbed(stationTitle, color: DiscordColor.White, fields: fields));
        }

        return embeds;
    }

    public static DiscordEmbed BuildGuildAddedEmbed(DiscordGuild guild, bool getInfo = false)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));

        string title = (getInfo) ? "Guild Information" : "Guild Added";
        string description = (getInfo) ? $"Here is everything I know about **{guild.Name}**" : $"I was added to **{guild.Name}**.";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(4)
        {
            ["Guild ID"] = new(guild.Id.ToString(CultureInfo.InvariantCulture)),
            ["Creation Date"] = new($"<t:{Converter.ConvertToUnixTime(guild.CreationTimestamp.Date)}>"),
            ["Owner"] = new(guild.Owner.Mention),
            ["Members"] = new(guild.MemberCount.ToString(CultureInfo.InvariantCulture), true)
        };

        Uri? iconUrl = null;
        if (guild.IconUrl is not null)
            iconUrl = new(guild.IconUrl);

        return CreateBasicEmbed(title, description, DiscordColor.Gold, iconUrl, fields: fields);
    }

    public static DiscordEmbed BuildGuildRemovedEmbed(ulong guildId, DiscordGuild? guild = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(guildId, nameof(guildId));

        const string title = "Guild Removed";
        string description = $"I was removed from **{((!string.IsNullOrWhiteSpace(guild?.Name)) ? guild.Name : guildId)}**.";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(3);
        if (guild is not null)
        {
            fields.Add("Guild ID", new(guild.Id.ToString(CultureInfo.InvariantCulture)));
            fields.Add("Removal Date", new($"<t:{Converter.ConvertToUnixTime(DateTime.Now)}>"));
            fields.Add("Owner", new($"<@!{guild.OwnerId}>"));
        }

        return CreateBasicEmbed(title, description, DiscordColor.Gold, fields: fields);
    }

    public static DiscordEmbed BuildMusicStreamingHistoryEmbed(IEnumerable<ITrackQueueItem> history, bool isQueue = false)
    {
        string title = (isQueue) ? "Upcoming Song Queue" : "Song History";
        StringBuilder builder = new();

        int count = 0;
        foreach (ITrackQueueItem item in history.Where(i => i.Track is not null))
        {
            if (title.Length + builder.Length > 6000)
                break;

            if (isQueue)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"- [{count}] **[{item.Track!.Title}]({item.Track!.Uri})** by **{item.Track!.Author}** ({item.Track!.Duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)})");
            }
            else
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"- **[{item.Track!.Title}]({item.Track!.Uri})** by **{item.Track!.Author}** ({item.Track!.Duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)})");
            }

            count++;
        }

        return CreateBasicEmbed(title, builder.ToString(), DiscordColor.Blurple);
    }

    public static DiscordEmbed BuildMusicStreamingNowPlayingEmbed(LavalinkTrack track, TimeSpan? elapsed)
    {
        ArgumentNullException.ThrowIfNull(track, nameof(track));
        ArgumentNullException.ThrowIfNull(elapsed, nameof(elapsed));

        const string title = "Now Playing";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(4)
        {
            ["Source"] = new(track.SourceName ?? "Not defined"),
            ["Title"] = new(track.Title, true),
            ["By"] = new(track.Author, true)
        };

        string songDuration = track.Duration.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
        string songElapsed = (elapsed.HasValue) ? elapsed.Value.ToString(@"mm\:ss", CultureInfo.InvariantCulture) : TimeSpan.FromTicks(0).ToString(@"mm\:ss", CultureInfo.InvariantCulture);
        string progressBar = Misc.GetProgressBar(14, elapsed.Value.TotalSeconds, track.Duration.TotalSeconds);

        fields.Add("Duration", new($"{progressBar} `[{songElapsed} / {songDuration}]`"));

        return CreateBasicEmbed(title, color: DiscordColor.Aquamarine, thumbnailUrl: track.ArtworkUri, url: track.Uri, fields: fields);
    }
}
