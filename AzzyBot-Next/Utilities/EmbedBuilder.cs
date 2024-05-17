﻿using System;
using System.Collections.Generic;
using System.Globalization;
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

    internal static DiscordEmbed BuildGetSettingsGuildEmbed(string serverName, GuildsEntity? guild = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName, nameof(serverName));

        const string title = "Settings overview";
        string description = $"Here are all settings which are currently set for {serverName}";

        Dictionary<string, DiscordEmbedRecord> fields = [];

        if (guild is not null)
        {
            fields.Add("Server ID", new(guild.UniqueId.ToString(CultureInfo.InvariantCulture)));
            fields.Add("Error channel", new((guild.ErrorChannelId > 0) ? $"<#{guild.ErrorChannelId}>" : "Not set"));
            fields.Add("Configuration complete", new(guild.ConfigSet.ToString()));
        }

        return CreateBasicEmbed(title, description, DiscordColor.White, null, null, null, fields);
    }

    internal static DiscordEmbed BuildGetSettingsAzuraEmbed(AzuraCastEntity? azuraCast = null)
    {
        const string title = "AzuraCast settings";

        Dictionary<string, DiscordEmbedRecord> fields = [];

        if (azuraCast is not null)
        {
            fields.Add("API Key", new($"||{((!string.IsNullOrWhiteSpace(azuraCast.ApiKey)) ? azuraCast.ApiKey : "Not set")}||"));
            fields.Add("API URL", new($"||{((!string.IsNullOrWhiteSpace(azuraCast.ApiUrl)) ? azuraCast.ApiUrl : "Not set")}||"));
            fields.Add("Station ID", new($"{((azuraCast.StationId > 0) ? azuraCast.StationId : "Not set")}"));
            fields.Add("Music Requests Channel", new((azuraCast.MusicRequestsChannelId > 0) ? $"<#{azuraCast.MusicRequestsChannelId}>" : "Not set"));
            fields.Add("Outages Channel", new((azuraCast.OutagesChannelId > 0) ? $"<#{azuraCast.OutagesChannelId}>" : "Not set"));
            fields.Add("Show Playlist In Now Playing", new(azuraCast.ShowPlaylistInNowPlaying.ToString()));
        }

        return CreateBasicEmbed(title, string.Empty, DiscordColor.White, null, null, null, fields);
    }

    internal static DiscordEmbed BuildGetSettingsAzuraChecksEmbed(AzuraCastChecksEntity checks)
    {
        const string title = "AzuraCast Checks settings";

        Dictionary<string, DiscordEmbedRecord> fields = [];

        fields.Add("File Changes", new(checks.FileChanges.ToString()));
        fields.Add("Server Status", new(checks.ServerStatus.ToString()));
        fields.Add("Updates", new(checks.Updates.ToString()));
        fields.Add("Updates Changelog", new(checks.UpdatesShowChangelog.ToString()));

        return CreateBasicEmbed(title, string.Empty, DiscordColor.White, null, null, null, fields);
    }

    internal static DiscordEmbed BuildAzzyUpdatesAvailableEmbed(Version version, in DateTime updateDate, Uri url)
    {
        const string title = "Azzy Updates Available";
        const string description = "Update now to get the latest bug fixes, features and improvements!";
        string yourVersion = AzzyStatsGeneral.GetBotVersion;

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
        bool isLinux = AzzyStatsGeneral.CheckIfLinuxOs;
        bool isWindows = AzzyStatsGeneral.CheckIfWindowsOs;
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
}
