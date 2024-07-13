using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Commands;
using AzzyBot.Commands.Checks;
using AzzyBot.Commands.Converters;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
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
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services;

public sealed class DiscordBotServiceHost : IHostedService
{
    private readonly ILogger<DiscordBotServiceHost> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly AzzyBotSettingsRecord _settings;
    private readonly DbActions _dbActions;
    private DiscordBotService? _botService;

    public DiscordClient Client { get; init; }

    public DiscordBotServiceHost(AzzyBotSettingsRecord settings, DbActions dbActions, ILogger<DiscordBotServiceHost> logger, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _serviceProvider = serviceProvider;
        _dbActions = dbActions;
        _settings = settings;

        Client = new(GetDiscordConfig());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_settings, nameof(_settings));

        cancellationToken.ThrowIfCancellationRequested();
        _botService = _serviceProvider.GetRequiredService<DiscordBotService>();
        RegisterEventHandlers();
        await RegisterCommandsAsync();
        RegisterInteractivity();
        await Client.ConnectAsync();

        _logger.BotReady();
        string invite = Client.CurrentApplication.GenerateOAuthUri(null, DiscordPermissions.AccessChannels | DiscordPermissions.SendMessages, [DiscordOAuthScope.ApplicationsCommands, DiscordOAuthScope.Bot]);
        _logger.InviteUrl(invite);

        // Wait 3 Seconds to let the client boot up
        await Task.Delay(3000, cancellationToken);

        int activity = _settings.DiscordStatus?.Activity ?? 2;
        string doing = _settings.DiscordStatus?.Doing ?? "Music";
        int status = _settings.DiscordStatus?.Status ?? 1;
        Uri? url = _settings.DiscordStatus?.StreamUrl;

        await SetBotStatusAsync(status, activity, doing, url);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Client.DisconnectAsync();
        UnregisterEventHandlers();
    }

    public async Task SetBotStatusAsync(int status = 1, int type = 2, string doing = "Music", Uri? url = null, bool reset = false)
    {
        if (reset)
        {
            await Client.UpdateStatusAsync(new DiscordActivity("Music", DiscordActivityType.ListeningTo), DiscordUserStatus.Online);
            return;
        }

        DiscordActivityType activityType = (DiscordActivityType)Enum.ToObject(typeof(DiscordActivityType), type);
        if (activityType.Equals(DiscordActivityType.Streaming) && url is null)
            activityType = DiscordActivityType.Playing;

        DiscordActivity activity = new(doing, activityType);
        if (activityType.Equals(DiscordActivityType.Streaming) && url is not null && (url.Host.Contains("twitch", StringComparison.OrdinalIgnoreCase) || url.Host.Contains("youtube", StringComparison.OrdinalIgnoreCase)))
            activity.StreamUrl = url.OriginalString;

        DiscordUserStatus userStatus = (DiscordUserStatus)Enum.ToObject(typeof(DiscordUserStatus), status);

        await Client.UpdateStatusAsync(activity, userStatus);
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
            LoggerFactory = _loggerFactory,
            // Otherwise it stops reconnecting after 4 attempts
            // TODO Remove this when the new IoC Client is released #1908
            ReconnectIndefinitely = true,
            Token = _settings.BotToken
        };
    }

    private async Task RegisterCommandsAsync()
    {
        ArgumentNullException.ThrowIfNull(_settings, nameof(_settings));

        CommandsExtension commandsExtension = Client.UseCommands(new()
        {
            RegisterDefaultCommandProcessors = false,
            ServiceProvider = _serviceProvider,
            UseDefaultCommandErrorHandler = false
        });

        commandsExtension.CommandErrored += CommandErroredAsync;

        // Only add admin commands to the main server
        commandsExtension.AddCommand(typeof(AdminCommands.AdminGroup), _settings.ServerId);

        // These commands are for every server
        commandsExtension.AddCommands(typeof(AzuraCastCommands.AzuraCastGroup));
        commandsExtension.AddCommands(typeof(AzuraCastCommands.DjGroup));
        commandsExtension.AddCommands(typeof(AzuraCastCommands.MusicGroup));
        commandsExtension.AddCommands(typeof(ConfigCommands.ConfigGroup));
        commandsExtension.AddCommands(typeof(CoreCommands.CoreGroup));

        // Only add debug commands if it's a dev build
        if (AzzyStatsSoftware.GetBotEnvironment == Environments.Development)
        {
            IReadOnlyList<GuildsEntity> guilds = await _dbActions.GetGuildsWithDebugAsync();
            List<ulong> debugGuilds = guilds.Select(g => g.UniqueId).ToList();
            if (!debugGuilds.Contains(_settings.ServerId))
                debugGuilds.Add(_settings.ServerId);

            commandsExtension.AddCommands(typeof(DebugCommands.DebugGroup), [.. debugGuilds]);
        }

        commandsExtension.AddCheck<AzuraCastDiscordPermCheck>();
        commandsExtension.AddCheck<AzuraCastOnlineCheck>();
        commandsExtension.AddCheck<ModuleActivatedCheck>();

        SlashCommandProcessor slashCommandProcessor = new();
        slashCommandProcessor.AddConverter<Uri>(new UriArgumentConverter());

        await commandsExtension.AddProcessorAsync(slashCommandProcessor);
    }

    private void RegisterInteractivity()
    {
        ArgumentNullException.ThrowIfNull(Client, nameof(Client));

        InteractivityConfiguration config = new()
        {
            ResponseBehavior = InteractionResponseBehavior.Ignore,
            ResponseMessage = "This is not a valid option!",
            Timeout = TimeSpan.FromMinutes(15)
        };

        Client.UseInteractivity(config);
    }

    private void RegisterEventHandlers()
    {
        Client.ClientErrored += ClientErroredAsync;
        Client.GuildCreated += ClientGuildCreatedAsync;
        Client.GuildDeleted += ClientGuildDeletedAsync;
        Client.GuildDownloadCompleted += ClientGuildDownloadCompletedAsync;
    }

    private void UnregisterEventHandlers()
    {
        Client.ClientErrored -= ClientErroredAsync;
        Client.GuildCreated -= ClientGuildCreatedAsync;
        Client.GuildDeleted -= ClientGuildDeletedAsync;
        Client.GuildDownloadCompleted -= ClientGuildDownloadCompletedAsync;
    }

    private async Task CommandErroredAsync(CommandsExtension c, CommandErroredEventArgs e)
    {
        _logger.CommandsError();
        _logger.CommandsErrorType(e.Exception.GetType().Name);

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

        switch (ex)
        {
            case ChecksFailedException checksFailed:
                await _botService.RespondToChecksExceptionAsync(checksFailed, slashContext);
                break;

            case DiscordException:
                await _botService.LogExceptionAsync(ex, now, slashContext, guildId, ((DiscordException)e.Exception).JsonMessage);
                break;

            default:
                await _botService.LogExceptionAsync(ex, now, slashContext, guildId);
                break;
        }
    }

    private async Task ClientErroredAsync(DiscordClient c, ClientErrorEventArgs e)
    {
        if (_botService is null)
            return;

        Exception ex = e.Exception;
        DateTime now = DateTime.Now;

        switch (ex)
        {
            case RateLimitException:
                break;

            case BadRequestException:
            case NotFoundException:
            case RequestSizeException:
            case ServerErrorException:
            case UnauthorizedException:
                await _botService.LogExceptionAsync(ex, now);
                break;

            default:
                if (ex is not DiscordException)
                {
                    await _botService.LogExceptionAsync(ex, now);
                    break;
                }

                await _botService.LogExceptionAsync(ex, now, 0, ((DiscordException)e.Exception).JsonMessage);
                break;
        }
    }

    private async Task ClientGuildCreatedAsync(DiscordClient c, GuildCreateEventArgs e)
    {
        _logger.GuildCreated(e.Guild.Name);

        await _dbActions.AddGuildAsync(e.Guild.Id);
        await e.Guild.Owner.SendMessageAsync("Thank you for adding me to your server! Before you can make good use of me, you have to set my settings first.\n\nPlease use the command `config modify-core` for this.\nOnly you are able to execute this command right now.");
    }

    private async Task ClientGuildDeletedAsync(DiscordClient c, GuildDeleteEventArgs e)
    {
        _logger.GuildDeleted(e.Guild.Name);

        await _dbActions.DeleteGuildAsync(e.Guild.Id);
    }

    private async Task ClientGuildDownloadCompletedAsync(DiscordClient c, GuildDownloadCompletedEventArgs e)
        => await _dbActions.AddGuildsAsync(e.Guilds.Select(g => g.Value.Id).ToList());
}
