using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Commands;
using AzzyBot.Commands.Converters;
using AzzyBot.Database;
using AzzyBot.Logging;
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

public sealed class DiscordBotServiceHost(ILogger<DiscordBotServiceHost> logger, IServiceProvider serviceProvider, AzzyBotSettingsRecord settings, DiscordClient client) : IHostedService, IClientErrorHandler
{
    private readonly ILogger<DiscordBotServiceHost> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly AzzyBotSettingsRecord _settings = settings;
    private readonly DiscordClient _client = client;
    private DiscordBotService? _botService;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_settings, nameof(_settings));

        cancellationToken.ThrowIfCancellationRequested();
        _botService = _serviceProvider.GetRequiredService<DiscordBotService>();
        await RegisterCommandsAsync();
        RegisterInteractivity();
        await _client.ConnectAsync();

        _logger.BotReady();
        _logger.InviteUrl(_client.CurrentApplication.Id);

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

        await _client.DisconnectAsync();
    }

    public async Task SetBotStatusAsync(int status = 1, int type = 2, string doing = "Music", Uri? url = null, bool reset = false)
    {
        if (reset)
        {
            await _client.UpdateStatusAsync(new DiscordActivity("Music", DiscordActivityType.ListeningTo), DiscordUserStatus.Online);
            return;
        }

        DiscordActivityType activityType = (DiscordActivityType)Enum.ToObject(typeof(DiscordActivityType), type);
        if (activityType.Equals(DiscordActivityType.Streaming) && url is null)
            activityType = DiscordActivityType.Playing;

        DiscordActivity activity = new(doing, activityType);
        if (activityType.Equals(DiscordActivityType.Streaming) && url is not null && (url.Host.Contains("twitch", StringComparison.OrdinalIgnoreCase) || url.Host.Contains("youtube", StringComparison.OrdinalIgnoreCase)))
            activity.StreamUrl = url.OriginalString;

        DiscordUserStatus userStatus = (DiscordUserStatus)Enum.ToObject(typeof(DiscordUserStatus), status);

        await _client.UpdateStatusAsync(activity, userStatus);
    }

    private async Task RegisterCommandsAsync()
    {
        ArgumentNullException.ThrowIfNull(_settings, nameof(_settings));

        CommandsExtension commandsExtension = _client.UseCommands(new()
        {
            RegisterDefaultCommandProcessors = false,
            UseDefaultCommandErrorHandler = false
        });

        commandsExtension.CommandErrored += CommandErroredAsync;

        // These commands are for every server
        commandsExtension.AddCommands(typeof(AzuraCastCommands.AzuraCastGroup));
        commandsExtension.AddCommands(typeof(AzuraCastCommands.MusicGroup));
        commandsExtension.AddCommands(typeof(ConfigCommands.ConfigGroup));
        commandsExtension.AddCommands(typeof(CoreCommands.CoreGroup));

        // Only add admin commands to the main server
        commandsExtension.AddCommand(typeof(AdminCommands.AdminGroup), _settings.ServerId);

        // Only add debug commands if it's a dev build
        if (AzzyStatsSoftware.GetBotName.EndsWith("Dev", StringComparison.OrdinalIgnoreCase))
            commandsExtension.AddCommands(typeof(DebugCommands.DebugGroup), _settings.ServerId);

        SlashCommandProcessor slashCommandProcessor = new();
        slashCommandProcessor.AddConverter<Uri>(new UriArgumentConverter());

        await commandsExtension.AddProcessorAsync(slashCommandProcessor);
    }

    private void RegisterInteractivity()
    {
        ArgumentNullException.ThrowIfNull(_client, nameof(_client));

        InteractivityConfiguration config = new()
        {
            ResponseBehavior = InteractionResponseBehavior.Ignore,
            ResponseMessage = "This is not a valid option!",
            Timeout = TimeSpan.FromMinutes(15)
        };

        _client.UseInteractivity(config);
    }

    public async Task CommandErroredAsync(CommandsExtension c, CommandErroredEventArgs e)
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

    public async ValueTask HandleEventHandlerError(string name, Exception exception, Delegate invokedDelegate, object sender, object args)
    {
        if (_botService is null)
            return;

        DateTime now = DateTime.Now;

        //switch (ex)
        //{
        //    case RateLimitException:
        //        break;

        //    case BadRequestException:
        //    case NotFoundException:
        //    case RequestSizeException:
        //    case ServerErrorException:
        //    case UnauthorizedException:
        //        await _botService.LogExceptionAsync(ex, now);
        //        break;

        //    default:
        //        if (ex is not DiscordException)
        //        {
        //            await _botService.LogExceptionAsync(ex, now);
        //            break;
        //        }

        //        await _botService.LogExceptionAsync(ex, now, 0, ((DiscordException)e.Exception).JsonMessage);
        //        break;
        //}
    }

    public async ValueTask HandleGatewayError(Exception exception)
    {
        if (_botService is null)
            return;

        DateTime now = DateTime.Now;
    }
}
