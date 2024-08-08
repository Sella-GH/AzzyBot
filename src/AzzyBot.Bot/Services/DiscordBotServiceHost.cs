using System;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Commands;
using AzzyBot.Bot.Commands.Checks;
using AzzyBot.Bot.Commands.Converters;
using AzzyBot.Bot.Settings;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services;

public sealed class DiscordBotServiceHost : IClientErrorHandler, IHostedService
{
    private readonly ILogger<DiscordBotServiceHost> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly AzzyBotSettingsRecord _settings;
    private DiscordBotService? _botService;

    public DiscordClient Client { get; init; }

    public DiscordBotServiceHost(ILogger<DiscordBotServiceHost> logger, IServiceProvider serviceProvider, AzzyBotSettingsRecord settings, DiscordClient client)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _settings = settings;

        Client = client;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_settings, nameof(_settings));

        cancellationToken.ThrowIfCancellationRequested();
        _botService = _serviceProvider.GetRequiredService<DiscordBotService>();
        RegisterCommands();
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

    public async ValueTask HandleEventHandlerError(string name, Exception exception, Delegate invokedDelegate, object sender, object args)
    {
        ArgumentNullException.ThrowIfNull(exception, nameof(exception));

        if (_botService is null)
            return;

        DateTime now = DateTime.Now;

        switch (exception)
        {
            case RateLimitException:
                break;

            case BadRequestException:
            case NotFoundException:
            case RequestSizeException:
            case ServerErrorException:
            case UnauthorizedException:
                await _botService.LogExceptionAsync(exception, now);
                break;

            default:
                if (exception is not DiscordException)
                {
                    await _botService.LogExceptionAsync(exception, now);
                    break;
                }

                await _botService.LogExceptionAsync(exception, now, info: ((DiscordException)exception).JsonMessage);
                break;
        }
    }

    public async ValueTask HandleGatewayError(Exception exception)
    {
        if (_botService is null)
            return;

        await _botService.LogExceptionAsync(exception, DateTime.Now);
    }

    private void RegisterCommands()
    {
        ArgumentNullException.ThrowIfNull(_settings, nameof(_settings));

        CommandsExtension commandsExtension = Client.UseCommands(new()
        {
            RegisterDefaultCommandProcessors = false,
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

        commandsExtension.AddProcessor<SlashCommandProcessor>();
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
