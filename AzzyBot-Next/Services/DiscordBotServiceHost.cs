﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Commands;
using AzzyBot.Commands.Converters;
using AzzyBot.Database;
using AzzyBot.Database.Models;
using AzzyBot.Logging;
using AzzyBot.Services.Modules;
using AzzyBot.Settings;
using AzzyBot.Utilities;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    internal readonly DiscordShardedClient _shardedClient;
    private DiscordBotService? _botService;

    public DiscordBotServiceHost(AzzyBotSettingsRecord settings, IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<DiscordBotServiceHost> logger, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
    {
        _settings = settings;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _serviceProvider = serviceProvider;

        _shardedClient = new(GetDiscordConfig());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_settings, nameof(_settings));

        cancellationToken.ThrowIfCancellationRequested();
        _botService = _serviceProvider.GetRequiredService<DiscordBotService>();
        RegisterEventHandlers();
        await RegisterCommandsAsync();
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

            // Activate commands based on the modules
            if (_serviceProvider.GetRequiredService<CoreServiceHost>()._isActivated)
                commandsExtension.AddCommands(typeof(CoreCommands.Core));

            // Only add debug commands if it's a dev build
            if (AzzyStatsGeneral.GetBotName.EndsWith("Dev", StringComparison.OrdinalIgnoreCase))
                commandsExtension.AddCommands(typeof(CoreCommands.Debug));

            SlashCommandProcessor slashCommandProcessor = new();
            slashCommandProcessor.AddConverter<Uri>(new UriArgumentConverter());

            await commandsExtension.AddProcessorAsync(slashCommandProcessor);
        }
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
        CommandContext ctx = e.Context;
        DateTime now = DateTime.Now;

        if (ex is ChecksFailedException checksFailed)
        {
            await _botService.LogExceptionAsync(ex, now, ctx);
        }
        else if (ex is not DiscordException)
        {
            await _botService.LogExceptionAsync(ex, now, ctx);
        }
        else
        {
            await _botService.LogExceptionAsync(ex, now, ctx, ((DiscordException)e.Exception).JsonMessage);
        }
    }

    private async Task ShardedClientGuildCreatedAsync(DiscordClient c, GuildCreateEventArgs e)
    {
        _logger.GuildCreated(e.Guild.Name);

        await using DatabaseContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            await context.Guilds.AddAsync(new() { UniqueId = e.Guild.Id });
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();
        }

        await e.Guild.Owner.SendMessageAsync("Thank you for adding me to your server! Before you can make use of me, you have to set my settings first.\n\nPlease use the command `settings set` for this.\nOnly you are able to execute this command right now.");
    }

    private async Task ShardedClientGuildDeletedAsync(DiscordClient c, GuildDeleteEventArgs e)
    {
        _logger.GuildDeleted(e.Guild.Name);

        await using DatabaseContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            GuildsEntity? guild = null;
            AzuraCastEntity? azura = null;
            AzuraCastChecksEntity? checks = null;

            try
            {
                guild = await context.Guilds.SingleAsync(g => g.UniqueId == e.Guild.Id);
                azura = await context.AzuraCast.SingleAsync(a => a.GuildId == guild.Id);
                checks = await context.AzuraCastChecks.SingleAsync(c => c.AzuraCastId == azura.Id);
            }
            catch (InvalidOperationException)
            { }

            if (checks is not null)
                context.AzuraCastChecks.Remove(checks);

            if (azura is not null)
                context.AzuraCast.Remove(azura);

            if (guild is not null)
                context.Guilds.Remove(guild);

            await context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
            await transaction.RollbackAsync();
        }

        await e.Guild.Owner.SendMessageAsync("I am sorry for being not used anymore on this server. I removed every piece of data of your server from my database and hope you will find use of me again!");
    }

    private async Task ShardedClientGuildDownloadCompletedAsync(DiscordClient c, GuildDownloadCompletedEventArgs e)
    {
        await using DatabaseContext context = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            List<GuildsEntity> guilds = await context.Guilds.ToListAsync();

            foreach (DiscordGuild guild in e.Guilds.Values)
            {
                if (guilds.Count > 0)
                {
                    foreach (GuildsEntity entity in guilds)
                    {
                        if (guild.Id == entity.UniqueId)
                            continue;

                        await context.Guilds.AddAsync(new() { UniqueId = guild.Id });
                    }
                }
                else
                {
                    await context.Guilds.AddAsync(new() { UniqueId = guild.Id });
                }
            }

            await context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
        {
            _logger.DatabaseTransactionFailed(ex);
        }
    }

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

                await _botService.LogExceptionAsync(ex, now, ((DiscordException)e.Exception).JsonMessage);
                break;
        }
    }
}
