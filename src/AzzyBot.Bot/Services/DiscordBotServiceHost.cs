using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Commands;
using AzzyBot.Bot.Commands.Checks;
using AzzyBot.Bot.Commands.Converters;
using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
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

namespace AzzyBot.Bot.Services;

public sealed class DiscordBotServiceHost : IHostedService
{
    private readonly ILogger<DiscordBotServiceHost> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly AzzyBotSettingsRecord _settings;
    private readonly DbActions _dbActions;
    private DiscordBotService? _botService;
    private const string NewGuildText = "Thank you for adding me to your server **%GUILD%**! Before you can make good use of me, you have to set my settings first.\n\nPlease use the command `config modify-core` for this.\nOnly administrators are able to execute this command right now.";

    public DiscordClient Client { get; init; }

    public DiscordBotServiceHost(ILogger<DiscordBotServiceHost> logger, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, AzzyBotSettingsRecord settings, DbActions dbActions)
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
        string invite = Client.CurrentApplication.GenerateOAuthUri(null, DiscordPermissions.AccessChannels | DiscordPermissions.AttachFiles | DiscordPermissions.SendMessages, [DiscordOAuthScope.ApplicationsCommands, DiscordOAuthScope.Bot]);
        _logger.InviteUrl(invite);

        // Wait 3 Seconds to let the client boot up
        await Task.Delay(3000, cancellationToken);

        int activity = _settings.DiscordStatus?.Activity ?? 2;
        string doing = _settings.DiscordStatus?.Doing ?? "Music";
        int status = _settings.DiscordStatus?.Status ?? 1;
        Uri? url = _settings.DiscordStatus?.StreamUrl;

        await _botService.SetBotStatusAsync(status, activity, doing, url);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Client.DisconnectAsync();
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
            Intents = DiscordIntents.Guilds | DiscordIntents.GuildVoiceStates,
            LoggerFactory = _loggerFactory,
            // Otherwise it stops reconnecting after 4 attempts
            // TODO Remove this when adapating to the newest release of DSP
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
        commandsExtension.AddCommands(typeof(MusicStreamingCommands.PlayerGroup));

        // Only add debug commands if it's a dev build
        if (SoftwareStats.GetAppEnvironment == Environments.Development)
            commandsExtension.AddCommands(typeof(DebugCommands.DebugGroup), _settings.ServerId);

        commandsExtension.AddCheck<AzuraCastDiscordChannelCheck>();
        commandsExtension.AddCheck<AzuraCastDiscordPermCheck>();
        commandsExtension.AddCheck<AzuraCastOnlineCheck>();
        commandsExtension.AddCheck<FeatureAvailableCheck>();
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

                await _botService.LogExceptionAsync(ex, now, info: ((DiscordException)e.Exception).JsonMessage);
                break;
        }
    }

    private async Task ClientGuildCreatedAsync(DiscordClient c, GuildCreateEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(_botService, nameof(_botService));

        _logger.GuildCreated(e.Guild.Name);

        await _dbActions.AddGuildAsync(e.Guild.Id);
        await e.Guild.Owner.SendMessageAsync(NewGuildText.Replace("%GUILD%", e.Guild.Name, StringComparison.OrdinalIgnoreCase));

        DiscordEmbed embed = EmbedBuilder.BuildGuildAddedEmbed(e.Guild);
        await _botService.SendMessageAsync(_settings.NotificationChannelId, embeds: [embed]);
    }

    private async Task ClientGuildDeletedAsync(DiscordClient c, GuildDeleteEventArgs e)
    {
        if (e.Guild.Id == _settings.ServerId)
        {
            _logger.RemovedFromHomeGuild(_settings.ServerId);
            Environment.Exit(0);

            return;
        }

        if (e.Unavailable)
        {
            _logger.GuildUnavailable(e.Guild.Name);
            return;
        }

        ArgumentNullException.ThrowIfNull(_botService, nameof(_botService));

        _logger.GuildDeleted(e.Guild.Name);

        await _dbActions.DeleteGuildAsync(e.Guild.Id);

        DiscordEmbed embed = EmbedBuilder.BuildGuildRemovedEmbed(e.Guild.Id, e.Guild);
        await _botService.SendMessageAsync(_settings.NotificationChannelId, null, [embed]);
    }

    private async Task ClientGuildDownloadCompletedAsync(DiscordClient c, GuildDownloadCompletedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(_botService, nameof(_botService));

        if (!e.Guilds.ContainsKey(_settings.ServerId))
        {
            _logger.NotInHomeGuild(_settings.ServerId);
            Environment.Exit(0);

            return;
        }

        DiscordEmbed embed;
        IEnumerable<DiscordGuild> addedGuilds = await _dbActions.AddGuildsAsync(e.Guilds);
        if (addedGuilds.Any())
        {
            foreach (DiscordGuild guild in addedGuilds)
            {
                await guild.Owner.SendMessageAsync(NewGuildText.Replace("%GUILD%", guild.Name, StringComparison.OrdinalIgnoreCase));
                embed = EmbedBuilder.BuildGuildAddedEmbed(guild);
                await _botService.SendMessageAsync(_settings.NotificationChannelId, embeds: [embed]);
            }
        }

        IEnumerable<ulong> removedGuilds = await _dbActions.DeleteGuildsAsync(e.Guilds);
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
            await _botService.LogExceptionAsync(ex, now, guildId: guildId);
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
}
