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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services;

public sealed class DiscordBotServiceHost(ILogger<DiscordBotServiceHost> logger, AzzyBotSettingsRecord settings, DiscordBotService botService, DiscordClient client) : IHostedService
{
    private readonly ILogger<DiscordBotServiceHost> _logger = logger;
    private readonly AzzyBotSettingsRecord _settings = settings;
    private readonly DiscordBotService _botService = botService;
    private readonly DiscordClient _client = client;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RegisterCommands();
        RegisterInteractivity();
        await _client.ConnectAsync();

        _logger.BotReady();
        string invite = _client.CurrentApplication.GenerateOAuthUri(null, DiscordPermissions.AccessChannels | DiscordPermissions.AttachFiles | DiscordPermissions.SendMessages, [DiscordOAuthScope.ApplicationsCommands, DiscordOAuthScope.Bot]);
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

        await _client.DisconnectAsync();
    }

    private void RegisterCommands()
    {
        CommandsExtension commandsExtension = _client.UseCommands(new()
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

        commandsExtension.AddProcessor(slashCommandProcessor);
    }

    private void RegisterInteractivity()
    {
        InteractivityConfiguration config = new()
        {
            ResponseBehavior = InteractionResponseBehavior.Ignore,
            ResponseMessage = "This is not a valid option!",
            Timeout = TimeSpan.FromMinutes(15)
        };

        _client.UseInteractivity(config);
    }

    private async Task CommandErroredAsync(CommandsExtension c, CommandErroredEventArgs e)
    {
        _logger.CommandsError();
        _logger.CommandsErrorType(e.Exception.GetType().Name);

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
