using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities;
using AzzyBot.Core.Logging;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Services.DiscordEvents;

public sealed class DiscordGuildsHandler(ILogger<DiscordGuildsHandler> logger, IOptions<AzzyBotSettings> settings, CronJobManager cron, DbActions dbActions, DiscordBotService botService)
    : IEventHandler<GuildCreatedEventArgs>, IEventHandler<GuildDeletedEventArgs>, IEventHandler<GuildDownloadCompletedEventArgs>
{
    private readonly ILogger<DiscordGuildsHandler> _logger = logger;
    private readonly AzzyBotSettings _settings = settings.Value;
    private readonly CronJobManager _cron = cron;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    public async Task HandleEventAsync(DiscordClient sender, GuildCreatedEventArgs eventArgs)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(eventArgs);

        _logger.GuildCreated(eventArgs.Guild.Name);

        await _dbActions.CreateGuildAsync(eventArgs.Guild.Id);
        await GuildCreatedHelperAsync([eventArgs.Guild]);
    }

    public async Task HandleEventAsync(DiscordClient sender, GuildDeletedEventArgs eventArgs)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(eventArgs);

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
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(eventArgs);

        if (!eventArgs.Guilds.ContainsKey(_settings.ServerId))
        {
            _logger.NotInHomeGuild(_settings.ServerId);
            Environment.Exit(0);

            return;
        }

        IEnumerable<DiscordGuild> addedGuilds = await _dbActions.CreateGuildsAsync(eventArgs.Guilds);
        if (addedGuilds.Any())
            await GuildCreatedHelperAsync(addedGuilds);

        DiscordEmbed embed;
        IEnumerable<ulong> removedGuilds = await _dbActions.DeleteGuildsAsync(eventArgs.Guilds.Values.ToAsyncEnumerable());
        if (removedGuilds.Any())
        {
            foreach (ulong removedGuild in removedGuilds)
            {
                embed = EmbedBuilder.BuildGuildRemovedEmbed(removedGuild);
                await _botService.SendMessageAsync(_settings.NotificationChannelId, embeds: [embed]);
            }
        }

        IReadOnlyList<GuildEntity> guilds = await _dbActions.ReadGuildsAsync(loadEverything: true);
        await _botService.CheckPermissionsAsync(guilds);

        AzzyBotEntity? azzy = await _dbActions.ReadAzzyBotAsync();
        if (azzy is null || azzy.LastGuildReminderCheck.AddDays(0.98) > DateTimeOffset.UtcNow)
            return;

        // Start the Guild Reminder Check
        _cron.RunAzzyBotInactiveGuildJob();
        azzy.LastGuildReminderCheck = DateTimeOffset.UtcNow;
        await _dbActions.UpdateAzzyBotAsync(lastGuildReminder: true);
    }

    private async Task GuildCreatedHelperAsync(IEnumerable<DiscordGuild> guilds)
    {
        ArgumentNullException.ThrowIfNull(guilds);

        DiscordEmbed embed;
        DiscordMember owner;
        foreach (DiscordGuild guild in guilds)
        {
            owner = await guild.GetGuildOwnerAsync();
            await owner.SendMessageAsync(EmbedBuilder.BuildAzzyAddedEmbed());
            embed = await EmbedBuilder.BuildGuildAddedEmbedAsync(guild);
            await _botService.SendMessageAsync(_settings.NotificationChannelId, embeds: [embed]);
        }
    }
}
