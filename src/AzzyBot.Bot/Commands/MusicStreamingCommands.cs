using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AzzyBot.Bot.Commands.Autocompletes;
using AzzyBot.Bot.Commands.Checks;
using AzzyBot.Bot.Commands.Choices;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities.Enums;
using AzzyBot.Bot.Utilities.Helpers;
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
        public async ValueTask ChangeVolumeAsync
        (
            CommandContext context,
            [Description("The volume you want to set.")] int volume
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(ChangeVolumeAsync), context.User.GlobalName);

            if (volume is < 0 or > 100)
            {
                await context.EditResponseAsync(GeneralStrings.VolumeInvalid);
                return;
            }

            await context.DeferResponseAsync();

            await _musicStreaming.SetVolumeAsync(context, volume);

            await context.EditResponseAsync($"I set the volume to {volume}%.");
        }

        [Command("join"), Description("Join the voice channel.")]
        public async ValueTask JoinAsync(CommandContext context)
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
                await context.EditResponseAsync(GeneralStrings.VoiceAlreadyIn);
                return;
            }

            await _musicStreaming.JoinChannelAsync(context);

            await context.EditResponseAsync(GeneralStrings.VoiceJoined);
        }

        [Command("leave"), Description("Leave the voice channel.")]
        public async ValueTask LeaveAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(LeaveAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            if (!await _musicStreaming.StopMusicAsync(context, true))
                return;

            await context.EditResponseAsync(GeneralStrings.VoiceLeft);
        }

        [Command("play"), Description("Choose a mount point of the station."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
        public async ValueTask PlayAsync
        (
            CommandContext context,
            [Description("The station you want play."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("The mount point of the station."), SlashAutoCompleteProvider<AzuraCastMountAutocomplete>] int mountPoint
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(PlayAsync), context.User.GlobalName);

            AzuraCastEntity? azura = await _dbActions.GetAzuraCastAsync(context.Guild.Id);
            if (azura is null)
            {
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraNowPlayingDataRecord nowPlaying;
            try
            {
                nowPlaying = await _azuraCast.GetNowPlayingAsync(new(Crypto.Decrypt(azura.BaseUrl)), station);
            }
            catch (HttpRequestException)
            {
                await context.EditResponseAsync(GeneralStrings.StationOffline);
                return;
            }

            string? mount = (mountPoint is 0) ? nowPlaying.Station.HlsUrl : nowPlaying.Station.Mounts.FirstOrDefault(m => m.Id == mountPoint)?.Url;
            if (mount is null)
            {
                string response = (mountPoint is 0) ? GeneralStrings.HlsNotAvailable : GeneralStrings.MountPointNotFound;
                await context.EditResponseAsync(response);
                return;
            }

            await _musicStreaming.PlayMusicAsync(context, mount);

            await context.EditResponseAsync(GeneralStrings.VoicePlay);
        }

        [Command("stop"), Description("Stop the music.")]
        public async ValueTask StopAsync
        (
            CommandContext context,
            [Description("Leave the voice channel."), SlashChoiceProvider<BooleanYesNoStateProvider>] int leave = 0
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(StopAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            bool leaving = leave is 1;
            await _musicStreaming.StopMusicAsync(context, leaving);
            string response = (leaving) ? GeneralStrings.VoiceStopLeft : GeneralStrings.VoiceStop;

            await context.EditResponseAsync(response);
        }
    }
}
