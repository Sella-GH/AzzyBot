﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Commands;
using AzzyBot.Commands.Converters;
using AzzyBot.Database;
using AzzyBot.Logging;
using AzzyBot.Services.Modules;
using AzzyBot.Settings;
using AzzyBot.Utilities;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services;

internal sealed class DiscordBotServiceHost : IHostedService
{
    private readonly ILogger<DiscordBotServiceHost> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly AzzyBotSettingsRecord _settings;
    private readonly DbActions _dbActions;
    internal readonly DiscordShardedClient _shardedClient;
    private DiscordBotService? _botService;

    public DiscordBotServiceHost(AzzyBotSettingsRecord settings, DbActions dbActions, ILogger<DiscordBotServiceHost> logger, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _serviceProvider = serviceProvider;
        _dbActions = dbActions;
        _settings = settings;

        _shardedClient = new(GetDiscordConfig());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_settings, nameof(_settings));

        cancellationToken.ThrowIfCancellationRequested();
        _botService = _serviceProvider.GetRequiredService<DiscordBotService>();
        RegisterEventHandlers();
        await RegisterCommandsAsync();
        await RegisterInteractivityAsync();
        await _shardedClient.StartAsync();

        _logger.BotReady();
        _logger.InviteUrl(_shardedClient.CurrentApplication.Id);

        // Wait 3 Seconds to let the client boot up
        await Task.Delay(3000, cancellationToken);

        int activity = _settings.DiscordStatus?.Activity ?? 2;
        string doing = _settings.DiscordStatus?.Doing ?? "Music";
        int status = _settings.DiscordStatus?.Status ?? 1;
        string? url = _settings.DiscordStatus?.StreamUrl?.ToString();

        await SetBotStatusAsync(status, activity, doing, url);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _shardedClient.StopAsync();
        UnregisterEventHandlers();
    }

    internal async Task SetBotStatusAsync(int status = 1, int type = 2, string doing = "Music", string? url = null)
    {
        DiscordActivityType activityType = (DiscordActivityType)Enum.ToObject(typeof(DiscordActivityType), type);
        if (activityType.Equals(DiscordActivityType.Streaming) && string.IsNullOrWhiteSpace(url))
            activityType = DiscordActivityType.Playing;

        DiscordActivity activity = new(doing, activityType);
        if (activityType.Equals(DiscordActivityType.Streaming) && !string.IsNullOrWhiteSpace(url) && (url.Contains("twitch", StringComparison.OrdinalIgnoreCase) || url.Contains("youtube", StringComparison.OrdinalIgnoreCase)))
            activity.StreamUrl = url;

        DiscordUserStatus userStatus = (DiscordUserStatus)Enum.ToObject(typeof(DiscordUserStatus), status);

        await _shardedClient.UpdateStatusAsync(activity, userStatus);
    }

    private DiscordConfiguration GetDiscordConfig()
    {
        ArgumentNullException.ThrowIfNull(_settings, nameof(_settings));

        if (string.IsNullOrWhiteSpace(_settings.BotToken))
        {
            _logger.BotTokenInvalid();
            Environment.Exit(1);
        }

        return new()
        {
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers,
            LoggerFactory = _loggerFactory,
            Token = _settings.BotToken,
            TokenType = TokenType.Bot
        };
    }

    private async Task RegisterCommandsAsync()
    {
        ArgumentNullException.ThrowIfNull(_settings, nameof(_settings));

        IReadOnlyDictionary<int, CommandsExtension> commandsExtensions = await _shardedClient.UseCommandsAsync(new()
        {
            RegisterDefaultCommandProcessors = false,
            ServiceProvider = _serviceProvider,
            UseDefaultCommandErrorHandler = false
        });

        foreach (CommandsExtension commandsExtension in commandsExtensions.Values)
        {
            commandsExtension.CommandErrored += CommandErroredAsync;

            commandsExtension.AddCommands(typeof(ConfigCommands.Config));

            if (_serviceProvider.GetRequiredService<CoreServiceHost>()._isActivated)
                commandsExtension.AddCommands(typeof(CoreCommands.Core));

            // Only add admin commands to the main server
            commandsExtension.AddCommand(typeof(AdminCommands.Admin), _settings.ServerId);

            // Only add debug commands if it's a dev build
            if (AzzyStatsGeneral.GetBotName.EndsWith("Dev", StringComparison.OrdinalIgnoreCase))
                commandsExtension.AddCommands(typeof(DebugCommands.Debug), _settings.ServerId);

            SlashCommandProcessor slashCommandProcessor = new();
            slashCommandProcessor.AddConverter<Uri>(new UriArgumentConverter());

            await commandsExtension.AddProcessorAsync(slashCommandProcessor);
        }
    }

    private async Task RegisterInteractivityAsync()
    {
        ArgumentNullException.ThrowIfNull(_shardedClient, nameof(_shardedClient));

        InteractivityConfiguration config = new()
        {
            ResponseBehavior = InteractionResponseBehavior.Ignore,
            ResponseMessage = "This is not a valid option!",
            Timeout = TimeSpan.FromMinutes(15)
        };

        await _shardedClient.UseInteractivityAsync(config);
    }

    private void RegisterEventHandlers()
    {
        _shardedClient.ClientErrored += ShardedClientErroredAsync;
        _shardedClient.GuildCreated += ShardedClientGuildCreatedAsync;
        _shardedClient.GuildDeleted += ShardedClientGuildDeletedAsync;
        _shardedClient.GuildDownloadCompleted += ShardedClientGuildDownloadCompletedAsync;
    }

    private void UnregisterEventHandlers()
    {
        _shardedClient.ClientErrored -= ShardedClientErroredAsync;
        _shardedClient.GuildCreated -= ShardedClientGuildCreatedAsync;
        _shardedClient.GuildDeleted -= ShardedClientGuildDeletedAsync;
        _shardedClient.GuildDownloadCompleted -= ShardedClientGuildDownloadCompletedAsync;
    }

    private async Task CommandErroredAsync(CommandsExtension c, CommandErroredEventArgs e)
    {
        _logger.CommandsError();

        if (_botService is null)
            return;

        Exception ex = e.Exception;
        DateTime now = DateTime.Now;
        ulong guildId = 0;
        if (e.Context.Guild is not null)
            guildId = e.Context.Guild.Id;

        if (e.Context is not SlashCommandContext slashContext)
        {
            await _botService.LogExceptionAsync(ex, now);
            return;
        }

        if (ex is DiscordException)
        {
            await _botService.LogExceptionAsync(ex, now, slashContext, guildId, ((DiscordException)e.Exception).JsonMessage);
        }
        else
        {
            await _botService.LogExceptionAsync(ex, now, slashContext, guildId);
        }
    }

    private async Task ShardedClientGuildCreatedAsync(DiscordClient c, GuildCreateEventArgs e)
    {
        _logger.GuildCreated(e.Guild.Name);

        await _dbActions.AddGuildEntityAsync(e.Guild.Id);
        await e.Guild.Owner.SendMessageAsync("Thank you for adding me to your server! Before you can make use of me, you have to set my settings first.\n\nPlease use the command `settings set` for this.\nOnly you are able to execute this command right now.");
    }

    private async Task ShardedClientGuildDeletedAsync(DiscordClient c, GuildDeleteEventArgs e)
    {
        _logger.GuildDeleted(e.Guild.Name);

        await _dbActions.RemoveGuildEntityAsync(e.Guild.Id);
    }

    private async Task ShardedClientGuildDownloadCompletedAsync(DiscordClient c, GuildDownloadCompletedEventArgs e)
        => await _dbActions.AddBulkGuildEntitiesAsync(e.Guilds.Select(g => g.Value.Id).ToList());

    private async Task ShardedClientErroredAsync(DiscordClient c, ClientErrorEventArgs e)
    {
        if (_botService is null)
            return;

        Exception ex = e.Exception;

        switch (ex)
        {
            case RateLimitException:
                break;

            case BadRequestException:
            case NotFoundException:
            case RequestSizeException:
            case ServerErrorException:
            case UnauthorizedException:
                await _botService.LogExceptionAsync(ex, DateTime.Now);
                break;

            default:
                DateTime now = DateTime.Now;

                if (ex is not DiscordException)
                {
                    await _botService.LogExceptionAsync(ex, now);
                    break;
                }

                await _botService.LogExceptionAsync(ex, now, 0, ((DiscordException)e.Exception).JsonMessage);
                break;
        }
    }
}
