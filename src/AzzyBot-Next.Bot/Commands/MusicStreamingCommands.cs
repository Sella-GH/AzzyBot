using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Bot.Commands.Autocompletes;
using AzzyBot.Bot.Commands.Checks;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities.Enums;
using AzzyBot.Core.Logging;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class MusicStreamingCommands
{
    [Command("player"), RequireGuild, RequirePermissions(DiscordPermissions.Speak | DiscordPermissions.UseVoice, DiscordPermissions.UseVoice)]
    public sealed class PlayerGroup(ILogger<PlayerGroup> logger, MusicStreamingService musicStreaming)
    {
        private readonly ILogger<PlayerGroup> _logger = logger;
        private readonly MusicStreamingService _musicStreaming = musicStreaming;

        [Command("join"), Description("Join the voice channel.")]
        public async Task JoinAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Member, nameof(context.Member));
            ArgumentNullException.ThrowIfNull(context.Member.VoiceState, nameof(context.Member.VoiceState));
            ArgumentNullException.ThrowIfNull(context.Member.VoiceState.Channel, nameof(context.Member.VoiceState.Channel));

            _logger.CommandRequested(nameof(JoinAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            if (context.Member.VoiceState.Channel.Users.Contains((DiscordMember)context.Client.CurrentUser))
            {
                await context.EditResponseAsync("I'm already in the voice channel.");
                return;
            }

            await _musicStreaming.JoinChannelAsync(context);

            await context.EditResponseAsync("I'm here now.");
        }

        [Command("leave"), Description("Leave the voice channel.")]
        public async Task LeaveAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(LeaveAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            await _musicStreaming.LeaveChannelAsync(context);

            await context.EditResponseAsync("I'm gone now.");
        }

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Needed for the AutoComplete.")]
        [Command("play"), Description("Choose a mount point of the station."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
        public async Task PlayAsync
        (
            CommandContext context,
            [Description("The station you want play."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("The mount point of the station."), SlashAutoCompleteProvider<AzuraCastMountAutocomplete>] string mountPoint
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(PlayAsync), context.User.GlobalName);

            await _musicStreaming.PlayMusicAsync(context, mountPoint);

            await context.EditResponseAsync("I'm starting to play now!");
        }
    }
}
