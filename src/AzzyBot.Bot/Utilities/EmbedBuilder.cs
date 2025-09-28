using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AzzyBot.Bot.Resources;
using AzzyBot.Bot.Utilities.Helpers;
using AzzyBot.Bot.Utilities.Records;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Bot.Utilities.Structs;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Core.Utilities.Enums;
using AzzyBot.Core.Utilities.Records;
using AzzyBot.Data.Entities;

using DSharpPlus.Entities;

using Lavalink4NET.Players;
using Lavalink4NET.Tracks;

using Microsoft.Extensions.Hosting;

namespace AzzyBot.Bot.Utilities;

public static class EmbedBuilder
{
    private static readonly Uri AzuraCastPic = new(UriStrings.AzuraCastPic);
    private static readonly Uri AzuraCastRollingUrl = new(UriStrings.AzuraCastRollingUrl);
    private static readonly Uri AzuraCastStableUrl = new(UriStrings.AzuraCastStableUrl);
    private static readonly Uri SetupInstructions = new(UriStrings.SetupInstructions);

    #region Constants

    private const string AlbumString = "On";
    private const string ArtistString = "By";
    private const string GenreString = "Genre";
    private const string NotSetString = "Not set.";
    private const string StationString = "Station";
    private const string TitleString = "Title";
    private const string DateTimeString = @"hh\:mm\:ss";

    #endregion Constants

    private static DiscordEmbedBuilder CreateBasicEmbed(string title, string? description = null, DiscordColor? color = null, EmbedAuthorStruct? author = null, Uri? thumbnailUrl = null, string? footerText = null, Uri? url = null, Dictionary<string, AzzyDiscordEmbedRecord>? fields = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

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

        if (author is not null)
            builder.WithAuthor(author.Value.Name, author.Value.Url, author.Value.IconUrl);

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
        ArgumentException.ThrowIfNullOrWhiteSpace(stationName);

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
            [StationString] = new(stationName),
            ["Added"] = new(addedFiles),
            ["Removed"] = new(removedFiles)
        };

        return CreateBasicEmbed(title, color: DiscordColor.Orange, fields: fields);
    }

    public static DiscordEmbed BuildAzuraCastHardwareStatsEmbed(AzuraHardwareStatsRecord stats)
    {
        ArgumentNullException.ThrowIfNull(stats);

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
        ArgumentNullException.ThrowIfNull(data);

        const string title = "Now Playing";
        string? message = null;
        string thumbnailUrl = (!string.IsNullOrEmpty(data.Live.Art)) ? data.Live.Art : data.NowPlaying.Song.Art;

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(8)
        {
            [StationString] = new(data.Station.Name),
            [TitleString] = new(data.NowPlaying.Song.Title, true)
        };

        if (!string.IsNullOrEmpty(data.NowPlaying.Song.Artist))
            fields.Add(ArtistString, new(data.NowPlaying.Song.Artist.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase), true));

        if (!string.IsNullOrEmpty(data.NowPlaying.Song.Album))
            fields.Add(AlbumString, new(data.NowPlaying.Song.Album.Replace(",", " &", StringComparison.OrdinalIgnoreCase).Replace(";", " & ", StringComparison.OrdinalIgnoreCase), true));

        if (!string.IsNullOrEmpty(data.NowPlaying.Song.Genre))
            fields.Add(GenreString, new(data.NowPlaying.Song.Genre, true));

        bool isLive = data.Live.IsLive;
        bool isIcecastLive = data.NowPlaying.Duration is 0; // Fix for #305 because you can also stream over Icecast

        if (isLive)
        {
            message = $"Currently served *live* by the one and only **{data.Live.StreamerName}**";
            fields.Add("Streaming live since", new($"<t:{Converter.ConvertFromUnixTime(Convert.ToInt64(data.Live.BroadcastStart, CultureInfo.InvariantCulture))}>"));
        }
        else if (isIcecastLive)
        {
            message = "Currently served *live* by a great dj";
        }
        else
        {
            if (!string.IsNullOrEmpty(playlistName))
                fields.Add("Playlist", new(playlistName, true));

            TimeSpan duration = TimeSpan.FromSeconds(data.NowPlaying.Duration);
            TimeSpan elapsed = TimeSpan.FromSeconds(data.NowPlaying.Elapsed);

            string songDuration = duration.ToString(DateTimeString, CultureInfo.InvariantCulture);
            string songElapsed = elapsed.ToString(DateTimeString, CultureInfo.InvariantCulture);
            string progressBar = Misc.GetProgressBar(14, elapsed.TotalSeconds, duration.TotalSeconds);

            fields.Add("Duration", new($"{progressBar} `[{songElapsed} / {songDuration}]`"));
        }

        return CreateBasicEmbed(title, message, DiscordColor.Aquamarine, thumbnailUrl: new(thumbnailUrl), fields: fields);
    }

    public static DiscordEmbed BuildAzuraCastMusicSearchSongEmbed(AzuraRequestRecord song, bool isQueued, bool isPlayed)
    {
        ArgumentNullException.ThrowIfNull(song);

        const string title = "Song Search";
        const string description = "Here is the song you requested.";
        string? footerText = null;

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(5)
        {
            [TitleString] = new(song.Song.Title, true),
            [ArtistString] = new(song.Song.Artist, true)
        };

        if (!string.IsNullOrEmpty(song.Song.Album))
            fields.Add(AlbumString, new(song.Song.Album, true));

        if (!string.IsNullOrEmpty(song.Song.Genre))
            fields.Add(GenreString, new(song.Song.Genre));

        if (!string.IsNullOrEmpty(song.Song.Isrc))
            fields.Add("ISRC", new(song.Song.Isrc));

        if (isQueued)
            footerText = "This song is already queued and will be played soon!";

        if (isPlayed)
            footerText = "This song was played in the last couple of minutes. Give it a break!";

        return CreateBasicEmbed(title, description, DiscordColor.Aquamarine, thumbnailUrl: new(song.Song.Art), footerText: footerText, fields: fields);
    }

    public static DiscordEmbed BuildAzuraCastUpdatesAvailableEmbed(AzuraUpdateRecord update)
    {
        ArgumentNullException.ThrowIfNull(update);

        const string title = "AzuraCast Updates Available";
        string updateDesc = (update.RollingUpdatesAvailable is 1) ? "update" : "updates";
        string description = (update.NeedsRollingUpdate)
            ? $"Your AzuraCast installation needs **{update.RollingUpdatesAvailable}** {updateDesc}."
            : "A new release of AzuraCast is available. Update now to get the latest bug fixes, features and improvements!";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(3);
        if (!string.IsNullOrEmpty(update.CurrentRelease))
        {
            fields.Add("Current Version", new(update.CurrentRelease));
            if (!string.IsNullOrEmpty(update.LatestRelease) && ((update.CurrentRelease != update.LatestRelease) && update.NeedsReleaseUpdate))
                fields.Add("Latest Version", new(update.LatestRelease));
        }
        else
        {
            fields.Add("Current Version", new("Rolling Release"));
        }

        if (update.CanSwitchToStable)
            fields.Add("Stable Switch Available?", new("Yes"));

        return CreateBasicEmbed(title, description, DiscordColor.White, thumbnailUrl: AzuraCastPic, fields: fields);
    }

    public static DiscordEmbed BuildAzuraCastUpdatesChangelogEmbed(bool isRolling, string? onlineChangelog = null)
    {
        const string title = "AzuraCast Updates Changelog";

        StringBuilder body = new();
        if (!string.IsNullOrEmpty(onlineChangelog))
            body.AppendLine(onlineChangelog);

        if (body.Length > 4096 || title.Length + body.Length > 6000)
            body = new($"The changelog is too big to display it in an Embed, you can view it [here]({((isRolling) ? AzuraCastRollingUrl : AzuraCastStableUrl)}).");

        return CreateBasicEmbed(title, body.ToString(), DiscordColor.White);
    }

    public static DiscordEmbed BuildAzuraCastUploadFileEmbed(AzuraFilesDetailedRecord file, int fileSize, string stationName, string stationArt)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fileSize);

        const string title = "File Uploaded";
        const string description = "Your song was uploaded successfully.";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(8)
        {
            [StationString] = new(stationName),
            [TitleString] = new(file.Title, true),
            [ArtistString] = new(file.Artist, true)
        };

        if (!string.IsNullOrEmpty(file.Album))
            fields.Add(AlbumString, new(file.Album, true));

        if (!string.IsNullOrEmpty(file.Genre))
            fields.Add(GenreString, new(file.Genre, true));

        fields.Add("Duration", new(file.Length, true));

        if (!string.IsNullOrEmpty(file.Isrc))
            fields.Add("ISRC", new(file.Isrc));

        fields.Add("File Size", new($"{Math.Round(fileSize / (1024.0 * 1024.0), 2)} MB"));

        return CreateBasicEmbed(title, description, DiscordColor.SpringGreen, thumbnailUrl: new(stationArt), fields: fields);
    }

    public static DiscordEmbed BuildAzzyAddedEmbed()
    {
        const string title = "Thanks For Adding Me To Your Server!";
        const string description = GeneralStrings.GuildJoinLegals;

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(1)
        {
            ["Setup Instructions"] = new($"[GitHub Wiki]({SetupInstructions})")
        };

        return CreateBasicEmbed(title, description, DiscordColor.SpringGreen, fields: fields);
    }

    [SuppressMessage("Style", "IDE0045:Convert to conditional expression", Justification = "We do not nest ternary expressions.")]
    public static DiscordEmbed BuildAzzyInactiveGuildEmbed(bool config, bool legals, DiscordGuild guild, in DateTimeOffset leaveDate)
    {
        ArgumentNullException.ThrowIfNull(guild);

        const string title = "Configuration Reminder";
        long timestamp;
        if (leaveDate != DateTimeOffset.MinValue)
        {
            timestamp = leaveDate.ToUnixTimeSeconds();
        }
        else
        {
            timestamp = (legals) ? DateTimeOffset.UtcNow.AddDays(3).ToUnixTimeSeconds() : DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds();
        }

        StringBuilder message = new();
        message.AppendLine(GeneralStrings.ReminderBegin);
        if (legals)
        {
            message.Append(GeneralStrings.ReminderLegals);
            message.Append(' ');
            message.AppendLine(GeneralStrings.ReminderLegalsFix);
        }

        if (config)
        {
            message.Append(GeneralStrings.ReminderConfig);
            message.Append(' ');
            message.AppendLine(GeneralStrings.ReminderConfigFix);
        }

        message.AppendLine(GeneralStrings.ReminderForceLeaveThreat.Replace("{%TIMEFRAME%}", $"<t:{timestamp}:R>", StringComparison.InvariantCulture));

        EmbedAuthorStruct author = new(guild.Name, null, guild.IconUrl);

        return CreateBasicEmbed(title, message.ToString(), DiscordColor.Orange, author: author);
    }

    public static async Task<DiscordEmbed> BuildAzzyHardwareStatsEmbedAsync(Uri avaUrl, int ping)
    {
        const string title = "AzzyBot Hardware Stats";
        const string notLinux = "To display more information you need to have a linux os.";
        string os = HardwareStats.GetSystemOs;
        string osArch = HardwareStats.GetSystemOsArch;
        long uptime = Converter.ConvertToUnixTime(HardwareStats.GetSystemUptime);

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(25)
        {
            ["Operating System"] = new(os, true),
            ["Architecture"] = new(osArch, true),
#if DOCKER || DOCKER_DEBUG
            ["Dockerized?"] = new(Misc.GetReadableBool(true, ReadableBool.YesNo), true),
#else
            ["Dockerized?"] = new(Misc.GetReadableBool(false, ReadableBool.YesNo), true),
#endif
            ["System Uptime"] = new($"<t:{uptime}>", true),
            ["Bot Memory"] = new($"{SoftwareStats.GetAppMemoryUsage()} GB", true)
        };

        if (ping is not 0)
            fields.Add("Discord Ping", new($"{ping} ms", true));

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
        ArgumentNullException.ThrowIfNull(command);

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
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(commands.Count);

        // Make the first letter an uppercase one and append the rest
        string title = $"Command List For {string.Concat(commands[0].SubCommand[0].ToString().ToUpperInvariant(), commands[0].SubCommand.AsSpan(1))} Group";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(commands.Count);
        foreach (AzzyHelpRecord command in commands)
        {
            fields.Add(command.Name, new(command.Description, true));
        }

        return CreateBasicEmbed(title, color: DiscordColor.Blurple, fields: fields);
    }

    public static DiscordEmbed BuildAzzyHelpSetupEmbed()
    {
        const string title = "Setup Help";
        string description = $"If you need help on how to setup the bot on your server, please check out the following URL:\n\n[Setup description (GitHub)]({SetupInstructions})";

        return CreateBasicEmbed(title, description, DiscordColor.Blurple);
    }

    public static DiscordEmbed BuildAzzyInfoStatsEmbed(Uri avaUrl, string dspVersion, string commit, in DateTimeOffset compileDate, int loc)
    {
        const string title = "AzzyBot Informational Stats";
        string[] authors = SoftwareStats.GetAppAuthors.Split(',');
        string sourceCode = $"{loc} lines";
        string formattedAuthors = $"- [{authors[0].Trim()}]({UriStrings.GitHubCreatorUri})\n- [{authors[1].Trim()}]({UriStrings.GitHubRepoContribUri})";
        string formattedCommit = $"[{commit}]({UriStrings.GitHubRepoCommitUri}/{commit})";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(13)
        {
            ["Authors"] = new(formattedAuthors, true),
            ["Repository"] = new($"[GitHub]({UriStrings.GitHubRepoUri})", true),
#if DEBUG || DOCKER_DEBUG
            ["Environment"] = new(Environments.Development, true),
#else
            ["Environment"] = new(Environments.Production, true),
#endif
            ["Bot Name"] = new(SoftwareStats.GetAppName, true),
            ["Bot Version"] = new(SoftwareStats.GetAppVersion, true),
            [".NET Version"] = new(SoftwareStats.GetAppDotNetVersion, true),
            ["D#+ Version"] = new(dspVersion, true),
            ["Source Code"] = new(sourceCode, true),
            ["Compilation Date"] = new($"<t:{Converter.ConvertToUnixTime(compileDate.ToLocalTime())}>", true),
            ["AzzyBot GitHub Commit"] = new(formattedCommit),
            ["Uptime"] = new($"<t:{Converter.ConvertToUnixTime(SoftwareStats.GetAppUptime().ToLocalTime())}>"),
            ["License"] = new($"[AGPL-3.0]({UriStrings.GitHubRepoLicenseUrl})", true),
            ["Terms Of Service"] = new($"[Terms Of Service]({UriStrings.GitHubRepoTosUrl})", true),
            ["Privacy Policy"] = new($"[Privacy Policy]({UriStrings.GitHubRepoPrivacyPolicyUrl})", true)
        };

        return CreateBasicEmbed(title, color: DiscordColor.Orange, thumbnailUrl: avaUrl, fields: fields);
    }

    public static DiscordEmbed BuildAzzyUpdatesAvailableEmbed(string version, in DateTimeOffset updateDate, Uri url)
    {
        ArgumentNullException.ThrowIfNull(version);

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
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);

        const string title = "Settings Overview";
        string description = $"Here are all settings which are currently set for **{serverName}**";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(5)
        {
            ["Configuration Complete"] = new(Misc.GetReadableBool(guild.ConfigSet, ReadableBool.YesNo)),
            ["Legals Accepted"] = new(Misc.GetReadableBool(guild.LegalsAccepted, ReadableBool.YesNo)),
            ["Server ID"] = new(guild.UniqueId.ToString(CultureInfo.InvariantCulture)),
            ["Admin Role"] = new((!string.IsNullOrEmpty(adminRole?.Trim()) && adminRole.Trim() is not "()") ? adminRole.Trim() : NotSetString),
            ["Admin Notify Channel"] = new((guild.Preferences.AdminNotifyChannelId > 0) ? $"<#{guild.Preferences.AdminNotifyChannelId}>" : NotSetString)
        };

        return CreateBasicEmbed(title, description, DiscordColor.White, fields: fields);
    }

    public static DiscordEmbed BuildGetSettingsAzuraInstanceEmbed(AzuraCastEntity azuraCast, string instanceRole)
    {
        ArgumentNullException.ThrowIfNull(azuraCast);

        const string title = "AzuraCast Settings";
        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(7)
        {
            ["Base Url"] = new($"||{((!string.IsNullOrEmpty(azuraCast.BaseUrl)) ? Crypto.Decrypt(azuraCast.BaseUrl) : NotSetString)}||"),
            ["Admin Api Key"] = new($"||{((!string.IsNullOrEmpty(azuraCast.AdminApiKey)) ? Crypto.Decrypt(azuraCast.AdminApiKey) : NotSetString)}||"),
            ["Instance Online"] = new(Misc.GetReadableBool(azuraCast.IsOnline, ReadableBool.EnabledDisabled)),
            ["Instance Admin Role"] = new((!string.IsNullOrEmpty(instanceRole?.Trim()) && instanceRole.Trim() is not "()") ? instanceRole.Trim() : NotSetString),
            ["Notification Channel"] = new((azuraCast.Preferences.NotificationChannelId > 0) ? $"<#{azuraCast.Preferences.NotificationChannelId}>" : NotSetString),
            ["Outages Channel"] = new((azuraCast.Preferences.OutagesChannelId > 0) ? $"<#{azuraCast.Preferences.OutagesChannelId}>" : NotSetString),
            ["Automatic Checks"] = new($"- Server Status: {Misc.GetReadableBool(azuraCast.Checks.ServerStatus, ReadableBool.EnabledDisabled)}\n- Updates: {Misc.GetReadableBool(azuraCast.Checks.Updates, ReadableBool.EnabledDisabled)}\n- Updates Changelog: {Misc.GetReadableBool(azuraCast.Checks.UpdatesShowChangelog, ReadableBool.EnabledDisabled)}")
        };

        return CreateBasicEmbed(title, color: DiscordColor.White, fields: fields);
    }

    public static IEnumerable<DiscordEmbed> BuildGetSettingsAzuraStationsEmbed(AzuraCastEntity azuraCast, IReadOnlyDictionary<ulong, string> stationRoles, IReadOnlyDictionary<int, string> stationNames, IReadOnlyDictionary<int, int> stationRequests)
    {
        ArgumentNullException.ThrowIfNull(azuraCast);

        const string stationTitle = "AzuraCast Stations";
        List<DiscordEmbed> embeds = new(azuraCast.Stations.Count);
        foreach (AzuraCastStationEntity station in azuraCast.Stations)
        {
            string stationName = stationNames.FirstOrDefault(x => x.Key == station.Id).Value;
            string stationId = station.StationId.ToString(CultureInfo.InvariantCulture);
            string stationApiKey = $"||{((!string.IsNullOrEmpty(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : NotSetString)}||";
            string stationAdminRole;
            if (station.Preferences.StationAdminRoleId > 0)
            {
                string role = stationRoles.FirstOrDefault(x => x.Key == station.Preferences.StationAdminRoleId).Value;
                ulong roleId = stationRoles.FirstOrDefault(x => x.Key == station.Preferences.StationAdminRoleId).Key;
                stationAdminRole = (role is not null) ? $"{role} ({roleId})" : NotSetString;
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
                stationDjRole = (role is not null) ? $"{role} ({roleId})" : NotSetString;
            }
            else
            {
                stationDjRole = "Not set";
            }

            string fileUploadChannel = (station.Preferences.FileUploadChannelId > 0) ? $"<#{station.Preferences.FileUploadChannelId}>" : NotSetString;
            string fileUploadPath = (!string.IsNullOrEmpty(station.Preferences.FileUploadPath)) ? station.Preferences.FileUploadPath : NotSetString;
            string nowPlayingChannel = (station.Preferences.NowPlayingEmbedChannelId > 0) ? $"<#{station.Preferences.NowPlayingEmbedChannelId}>" : NotSetString;
            string requestsChannel = (station.Preferences.RequestsChannelId > 0) ? $"<#{station.Preferences.RequestsChannelId}>" : NotSetString;
            int requestCount = stationRequests.FirstOrDefault(x => x.Key == station.Id).Value;
            string showPlaylist = Misc.GetReadableBool(station.Preferences.ShowPlaylistInNowPlaying, ReadableBool.EnabledDisabled);
            string fileChanges = Misc.GetReadableBool(station.Checks.FileChanges, ReadableBool.EnabledDisabled);

            Dictionary<string, AzzyDiscordEmbedRecord> fields = new(12)
            {
                ["Station Name"] = new(stationName),
                ["Station ID"] = new(stationId),
                ["Station Api Key"] = new(stationApiKey),
                ["File Upload Channel"] = new(fileUploadChannel),
                ["File Upload Path"] = new(fileUploadPath),
                ["Now Playing Channel"] = new(nowPlayingChannel),
                ["Song Requests Channel"] = new(requestsChannel),
                ["Song Request Count"] = new(requestCount.ToString(CultureInfo.InvariantCulture)),
                ["Show Playlist In Now Playing"] = new(showPlaylist),
                ["Station Admin Role"] = new(stationAdminRole),
                ["Station DJ Role"] = new(stationDjRole),
                ["Automatic Checks"] = new($"- File Changes: {fileChanges}")
            };

            embeds.Add(CreateBasicEmbed(stationTitle, color: DiscordColor.White, fields: fields));
        }

        return embeds;
    }

    public static async Task<DiscordEmbed> BuildGuildAddedEmbedAsync(DiscordGuild guild, bool getInfo = false)
    {
        ArgumentNullException.ThrowIfNull(guild);

        string title = (getInfo) ? "Guild Information" : "Guild Added";
        string description = (getInfo) ? $"Here is everything I know about **{guild.Name}**" : $"I was added to **{guild.Name}**.";
        DiscordMember owner = await guild.GetGuildOwnerAsync();

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(4)
        {
            ["Guild ID"] = new(guild.Id.ToString(CultureInfo.InvariantCulture)),
            ["Creation Date"] = new($"<t:{Converter.ConvertToUnixTime(guild.CreationTimestamp.Date)}>"),
            ["Owner"] = new($"{owner.DisplayName} ({owner.Id})", true),
            ["Members"] = new(guild.MemberCount.ToString(CultureInfo.InvariantCulture), true)
        };

        Uri? iconUrl = null;
        if (guild.IconUrl is not null)
            iconUrl = new(guild.IconUrl);

        return CreateBasicEmbed(title, description, DiscordColor.Gold, thumbnailUrl: iconUrl, fields: fields);
    }

    public static DiscordEmbed BuildGuildRemovedEmbed(ulong guildId, DiscordGuild? guild = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(guildId);

        const string title = "Guild Removed";
        string description = $"I was removed from **{((!string.IsNullOrEmpty(guild?.Name)) ? guild.Name : guildId)}**.";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(3);
        if (guild is not null)
        {
            fields.Add("Guild ID", new(guild.Id.ToString(CultureInfo.InvariantCulture)));
            fields.Add("Removal Date", new($"<t:{Converter.ConvertToUnixTime(DateTimeOffset.Now)}>"));
            fields.Add("Owner", new(guild.OwnerId.ToString(CultureInfo.InvariantCulture)));
        }

        return CreateBasicEmbed(title, description, DiscordColor.Gold, fields: fields);
    }

    public static DiscordEmbed BuildMusicStreamingHistoryEmbed(IEnumerable<ITrackQueueItem> history, bool isQueue = false)
    {
        string title = (isQueue) ? "Upcoming Song Queue" : "Song History";
        StringBuilder builder = new();

        int count = 0;
        foreach (LavalinkTrack item in history.Where(static i => i.Track is not null).Select(i => i.Track!))
        {
            if (title.Length + builder.Length > 6000)
                break;

            if (isQueue)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"- [{count}] **[{item.Title}]({item.Uri})** by **{item.Author}** ({item.Duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)})");
                count++;
            }
            else
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"- **[{item.Title}]({item.Uri})** by **{item.Author}** ({item.Duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)})");
            }
        }

        return CreateBasicEmbed(title, builder.ToString(), DiscordColor.Blurple);
    }

    public static DiscordEmbed BuildMusicStreamingNowPlayingEmbed(LavalinkTrack track, TimeSpan? elapsed)
    {
        ArgumentNullException.ThrowIfNull(track);
        if (elapsed is null)
            throw new ArgumentNullException(nameof(elapsed), "Elapsed time cannot be null.");

        const string title = "Now Playing";

        Dictionary<string, AzzyDiscordEmbedRecord> fields = new(4)
        {
            ["Source"] = new(track.SourceName ?? "Not defined"),
            [TitleString] = new(track.Title, true),
            [ArtistString] = new(track.Author, true)
        };

        // Evalute this once there is a bug
        // As of 2024-09-26 this should work flawlessly
        // Specificially the *elapsed.Value* variable
        string songDuration = track.Duration.ToString(DateTimeString, CultureInfo.InvariantCulture);
        string songElapsed = elapsed.Value.ToString(DateTimeString, CultureInfo.InvariantCulture);
        string progressBar = Misc.GetProgressBar(14, elapsed.Value.TotalSeconds, track.Duration.TotalSeconds);

        fields.Add("Duration", new($"{progressBar} `[{songElapsed} / {songDuration}]`"));

        return CreateBasicEmbed(title, color: DiscordColor.Aquamarine, thumbnailUrl: track.ArtworkUri, url: track.Uri, fields: fields);
    }
}
