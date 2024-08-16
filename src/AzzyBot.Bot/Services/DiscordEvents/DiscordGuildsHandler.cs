using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities;
using AzzyBot.Core.Logging;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.DiscordEvents;

public sealed class DiscordGuildsHandler(ILogger<DiscordGuildsHandler> logger, AzzyBotSettingsRecord settings, DiscordBotService botService, DbActions dbActions) : IEventHandler<GuildCreatedEventArgs>, IEventHandler<GuildDeletedEventArgs>, IEventHandler<GuildDownloadCompletedEventArgs>
{
    private readonly ILogger<DiscordGuildsHandler> _logger = logger;
    private readonly AzzyBotSettingsRecord _settings = settings;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    private const string NewGuildText = "Thank you for adding me to your server **%GUILD%**! Before you can make good use of me, you have to set my settings first.\n\nPlease use the command `config modify-core` for this.\nOnly administrators are able to execute this command right now.";

    public async Task HandleEventAsync(DiscordClient sender, GuildCreatedEventArgs eventArgs)
    {
        ArgumentNullException.ThrowIfNull(sender, nameof(sender));
        ArgumentNullException.ThrowIfNull(eventArgs, nameof(eventArgs));

        _logger.GuildCreated(eventArgs.Guild.Name);

        await _dbActions.AddGuildAsync(eventArgs.Guild.Id);
        DiscordMember owner = await eventArgs.Guild.GetGuildOwnerAsync();
        await owner.SendMessageAsync(NewGuildText.Replace("%GUILD%", eventArgs.Guild.Name, StringComparison.OrdinalIgnoreCase));

        DiscordEmbed embed = await EmbedBuilder.BuildGuildAddedEmbedAsync(eventArgs.Guild);
        await _botService.SendMessageAsync(_settings.NotificationChannelId, embeds: [embed]);
    }

    public async Task HandleEventAsync(DiscordClient sender, GuildDeletedEventArgs eventArgs)
    {
        ArgumentNullException.ThrowIfNull(sender, nameof(sender));
        ArgumentNullException.ThrowIfNull(eventArgs, nameof(eventArgs));

        if (eventArgs.Guild.Id == _settings.ServerId)
        {
            _logger.RemovedFromHomeGuild(_settings.ServerId);
            Environment.Exit(0);

            return;
        }
        else if (eventArgs.Unavailable)
        {
            _logger.GuildUnavailable(eventArgs.Guild.Name);
            return;
        }

        _logger.GuildDeleted(eventArgs.Guild.Name);

        await _dbActions.DeleteGuildAsync(eventArgs.Guild.Id);

        DiscordEmbed embed = EmbedBuilder.BuildGuildRemovedEmbed(eventArgs.Guild.Id, eventArgs.Guild);
        await _botService.SendMessageAsync(_settings.NotificationChannelId, embeds: [embed]);
    }

    public async Task HandleEventAsync(DiscordClient sender, GuildDownloadCompletedEventArgs eventArgs)
    {
        ArgumentNullException.ThrowIfNull(sender, nameof(sender));
        ArgumentNullException.ThrowIfNull(eventArgs, nameof(eventArgs));

        if (!eventArgs.Guilds.ContainsKey(_settings.ServerId))
        {
            _logger.NotInHomeGuild(_settings.ServerId);
            Environment.Exit(0);

            return;
        }

        DiscordEmbed embed;
        DiscordMember owner;
        IEnumerable<DiscordGuild> addedGuilds = await _dbActions.AddGuildsAsync(eventArgs.Guilds);
        if (addedGuilds.Any())
        {
            foreach (DiscordGuild guild in addedGuilds)
            {
                owner = await guild.GetGuildOwnerAsync();
                await owner.SendMessageAsync(NewGuildText.Replace("%GUILD%", guild.Name, StringComparison.OrdinalIgnoreCase));
                embed = await EmbedBuilder.BuildGuildAddedEmbedAsync(guild);
                await _botService.SendMessageAsync(_settings.NotificationChannelId, embeds: [embed]);
            }
        }

        IEnumerable<ulong> removedGuilds = await _dbActions.DeleteGuildsAsync(eventArgs.Guilds);
        if (removedGuilds.Any())
        {
            foreach (ulong guild in removedGuilds)
            {
                embed = EmbedBuilder.BuildGuildRemovedEmbed(guild);
                await _botService.SendMessageAsync(_settings.NotificationChannelId, embeds: [embed]);
            }
        }

        IAsyncEnumerable<GuildEntity> guilds = _dbActions.GetGuildsAsync(loadEverything: true);
        await _botService.CheckPermissionsAsync(guilds);
    }
}
