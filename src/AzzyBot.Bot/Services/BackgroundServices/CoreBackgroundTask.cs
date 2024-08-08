using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzzyBot.Core.Logging;
using AzzyBot.Data.Entities;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.BackgroundServices;

public sealed class CoreBackgroundTask(ILogger<CoreBackgroundTask> logger, DiscordBotService botService)
{
    private readonly ILogger<CoreBackgroundTask> _logger = logger;
    private readonly DiscordBotService _botService = botService;

    public async Task CheckPermissionsAsync(DiscordGuild guild, ulong[] channelIds)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));
        ArgumentNullException.ThrowIfNull(channelIds, nameof(channelIds));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelIds.Length, nameof(channelIds));

        _logger.BackgroundServiceWorkItem(nameof(CheckPermissionsAsync));

        DiscordMember? member = await _botService.GetDiscordMemberAsync(guild.Id);
        if (member is null)
        {
            _logger.DiscordItemNotFound(nameof(DiscordMember), guild.Id);
            return;
        }

        List<ulong> channels = new(channelIds.Length);
        List<ulong> channelNotAccessible = new(channelIds.Length);
        foreach (ulong channelId in channelIds)
        {
            channels.Add(channelId);
            if (!await _botService.CheckChannelPermissionsAsync(member, channelId, DiscordPermissions.AccessChannels | DiscordPermissions.SendMessages))
                channelNotAccessible.Add(channelId);
        }

        if (channelNotAccessible.Count is 0)
            return;

        await guild.Owner.SendMessageAsync($"I don't have the required permissions to send messages in channel(s) {string.Join(", ", channelNotAccessible)} in guild {guild.Name} ({guild.Id}).\nPlease review your permission set.");
    }

    public async Task CheckPermissionsAsync(IAsyncEnumerable<GuildEntity> guilds)
    {
        ArgumentNullException.ThrowIfNull(guilds, nameof(guilds));

        _logger.BackgroundServiceWorkItem(nameof(CheckPermissionsAsync));

        DiscordMember? member;
        List<ulong> channels = [];
        List<ulong> channelNotAccessible = [];
        await foreach (GuildEntity guild in guilds)
        {
            member = await _botService.GetDiscordMemberAsync(guild.UniqueId);
            if (member is null)
            {
                _logger.DiscordItemNotFound(nameof(DiscordMember), guild.UniqueId);
                continue;
            }

            if (guild is null)
                continue;

            if (guild.Preferences.AdminNotifyChannelId is not 0)
                channels.Add(guild.Preferences.AdminNotifyChannelId);

            if (guild.Preferences.ErrorChannelId is not 0)
                channels.Add(guild.Preferences.ErrorChannelId);

            if (guild.AzuraCast is null)
                continue;

            if (guild.AzuraCast.Preferences.NotificationChannelId is not 0)
                channels.Add(guild.AzuraCast.Preferences.NotificationChannelId);

            if (guild.AzuraCast.Preferences.OutagesChannelId is not 0)
                channels.Add(guild.AzuraCast.Preferences.OutagesChannelId);

            foreach (AzuraCastStationEntity station in guild.AzuraCast.Stations)
            {
                if (station.Preferences.FileUploadChannelId is not 0)
                    channels.Add(station.Preferences.FileUploadChannelId);

                if (station.Preferences.RequestsChannelId is not 0)
                    channels.Add(station.Preferences.RequestsChannelId);
            }

            foreach (ulong channelId in channels)
            {
                if (!await _botService.CheckChannelPermissionsAsync(member, channelId, DiscordPermissions.AccessChannels | DiscordPermissions.SendMessages))
                    channelNotAccessible.Add(channelId);
            }

            if (channelNotAccessible.Count is 0)
            {
                channels.Clear();
                continue;
            }

            DiscordGuild? dGuild = _botService.GetDiscordGuild(guild.UniqueId);
            if (dGuild is null)
            {
                _logger.DiscordItemNotFound(nameof(DiscordGuild), guild.UniqueId);
                continue;
            }

            await dGuild.Owner.SendMessageAsync($"I don't have the required permissions to send messages in channel(s) {string.Join(", ", channelNotAccessible)} in guild {dGuild.Name} ({dGuild.Id}).\nPlease review your permission set.");
        }
    }
}
