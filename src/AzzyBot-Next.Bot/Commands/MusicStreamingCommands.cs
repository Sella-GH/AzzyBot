using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AzzyBot.Bot.Commands.Autocompletes;
using AzzyBot.Bot.Commands.Checks;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities.Enums;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
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
    public sealed class PlayerGroup(ILogger<PlayerGroup> logger, AzuraCastApiService azuraCast, DbActions dbActions, MusicStreamingService musicStreaming)
    {
        private readonly ILogger<PlayerGroup> _logger = logger;
        private readonly AzuraCastApiService _azuraCast = azuraCast;
        private readonly DbActions _dbActions = dbActions;
        private readonly MusicStreamingService _musicStreaming = musicStreaming;

        [Command("change-volume"), Description("Change the volume of the played music.")]
        public async Task ChangeVolumeAsync
        (
            CommandContext context,
            [Description("The volume you want to set.")] int volume
)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(ChangeVolumeAsync), context.User.GlobalName);

            if (volume is < 0 or > 100)
            {
                await context.EditResponseAsync("The volume must be between 0 and 100.");
                return;
            }

            await context.DeferResponseAsync();

            await _musicStreaming.SetVolumeAsync(context, volume);

            await context.EditResponseAsync($"I set the volume to {volume}%.");
        }

        [Command("join"), Description("Join the voice channel.")]
        public async Task JoinAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));
            ArgumentNullException.ThrowIfNull(context.Member, nameof(context.Member));
            ArgumentNullException.ThrowIfNull(context.Member.VoiceState, nameof(context.Member.VoiceState));
            ArgumentNullException.ThrowIfNull(context.Member.VoiceState.Channel, nameof(context.Member.VoiceState.Channel));

            _logger.CommandRequested(nameof(JoinAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            if (context.Member.VoiceState.Channel.Users.Contains(await context.Guild.GetMemberAsync(context.Client.CurrentUser.Id)))
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

            if (!await _musicStreaming.StopMusicAsync(context, true))
                return;

            await context.EditResponseAsync("I'm gone now.");
        }

        [Command("play"), Description("Choose a mount point of the station."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
        public async Task PlayAsync
        (
            CommandContext context,
            [Description("The station you want play."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("The mount point of the station."), SlashAutoCompleteProvider<AzuraCastMountAutocomplete>] int mountPoint
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(PlayAsync), context.User.GlobalName);

            AzuraCastEntity azura = await _dbActions.GetAzuraCastAsync(context.Guild.Id, false, false, true) ?? throw new InvalidOperationException("AzuraCast is not set up for this server.");
            AzuraNowPlayingDataRecord nowPlaying;
            try
            {
                nowPlaying = await _azuraCast.GetNowPlayingAsync(new(Crypto.Decrypt(azura.BaseUrl)), station);
            }
            catch (HttpRequestException)
            {
                await context.EditResponseAsync("This station is currently offline.");
                return;
            }

            string mount = (mountPoint is 0)
                ? nowPlaying.Station.HlsUrl ?? throw new InvalidOperationException("HTTP Live Streaming is not available for this station.")
                : nowPlaying.Station.Mounts.FirstOrDefault(m => m.Id == mountPoint)?.Url ?? throw new InvalidOperationException("Mount point not found.");

            await _musicStreaming.PlayMusicAsync(context, mount);

            await context.EditResponseAsync("I'm starting to play now!");
        }

        [Command("stop"), Description("Stop the music.")]
        public async Task StopAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(StopAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            await _musicStreaming.StopMusicAsync(context, false);

            await context.EditResponseAsync("I stopped the music.");
        }
    }
}
