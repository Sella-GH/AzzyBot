using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using AzzyBot.Bot.Commands.Autocompletes;
using AzzyBot.Bot.Commands.Checks;
using AzzyBot.Bot.Commands.Choices;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Enums;
using AzzyBot.Bot.Utilities.Helpers;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

using Lavalink4NET.Players;
using Lavalink4NET.Tracks;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class MusicStreamingCommands
{
    [Command("player"), RequireGuild, RequirePermissions(botPermissions: [DiscordPermission.Connect, DiscordPermission.Speak], userPermissions: [DiscordPermission.Connect]), ModuleActivatedCheck([AzzyModules.LegalTerms])]
    public sealed class PlayerGroup(ILogger<PlayerGroup> logger, AzuraCastApiService azuraCast, DbActions dbActions, MusicStreamingService musicStreaming)
    {
        private readonly ILogger<PlayerGroup> _logger = logger;
        private readonly AzuraCastApiService _azuraCast = azuraCast;
        private readonly DbActions _dbActions = dbActions;
        private readonly MusicStreamingService _musicStreaming = musicStreaming;
        private static readonly string AppName = SoftwareStats.GetAppName;

        [Command("change-volume"), Description("Change the volume of the played music.")]
        public async ValueTask ChangeVolumeAsync
        (
            SlashCommandContext context,
            [Description("The volume you want to set.")] int volume,
            [Description("Whether the volume should be saved for future use."), SlashChoiceProvider<BooleanYesNoStateProvider>] int saveState = 0
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(ChangeVolumeAsync), context.User.GlobalName);

            if (volume is < 0 or > 100)
            {
                await context.RespondAsync(GeneralStrings.VolumeInvalid, true);
                return;
            }

            if (!await _musicStreaming.SetVolumeAsync(context, volume))
                return;

            string message = $"I set the volume to {volume}%.";
            if (saveState is 1)
            {
                MusicStreamingEntity? ms = await _dbActions.ReadMusicStreamingAsync(context.Guild.Id);
                if (ms is null)
                {
                    await _dbActions.CreateMusicStreamingAsync(context.Guild.Id, volume: volume);
                }
                else
                {
                    await _dbActions.UpdateMusicStreamingAsync(context.Guild.Id, volume: volume);
                }

                message += " I also saved this volume level for future use.";
            }

            await context.EditResponseAsync(message);
        }

        [Command("history"), Description("Show the already played song history for this server.")]
        public async ValueTask HistoryAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(HistoryAsync), context.User.GlobalName);

            IEnumerable<ITrackQueueItem>? history = await _musicStreaming.HistoryAsync(context);
            if (history?.Any() is not true)
            {
                await context.EditResponseAsync("There is no song history.");
                return;
            }

            DiscordEmbed embed = EmbedBuilder.BuildMusicStreamingHistoryEmbed(history);
            await context.EditResponseAsync(embed);
        }

        [Command("join"), Description("Join the voice channel.")]
        public async ValueTask JoinAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);
            ArgumentNullException.ThrowIfNull(context.Member);

            _logger.CommandRequested(nameof(JoinAsync), context.User.GlobalName);

            if (context.Member.VoiceState?.ChannelId is null)
            {
                await context.EditResponseAsync(GeneralStrings.VoiceNoUser);
                return;
            }

            DiscordChannel? channel = await context.Member.VoiceState.GetChannelAsync();
            if (channel?.Users.Contains(await context.Guild.GetMemberAsync(context.Client.CurrentUser.Id)) is true)
            {
                await context.EditResponseAsync(GeneralStrings.VoiceAlreadyIn);
                return;
            }

            if (!await _musicStreaming.JoinChannelAsync(context))
                return;

            await context.EditResponseAsync(GeneralStrings.VoiceJoined);
        }

        [Command("leave"), Description("Leave the voice channel.")]
        public async ValueTask LeaveAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(LeaveAsync), context.User.GlobalName);

            if (!await _musicStreaming.StopMusicAsync(context, true))
                return;

            await context.EditResponseAsync(GeneralStrings.VoiceLeft);
        }

        [Command("now-playing"), Description("Show the song which is playing right now.")]
        public async ValueTask NowPlayingAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(NowPlayingAsync), context.User.GlobalName);

            LavalinkTrack? track = await _musicStreaming.NowPlayingAsync(context);
            TimeSpan? pos = await _musicStreaming.GetCurrentPositionAsync(context);
            if (track is null || pos is null)
            {
                await context.EditResponseAsync(GeneralStrings.VoiceNothingPlaying);
                return;
            }
            else if ((track.Author == SoftwareStats.GetAppAuthors || track.Identifier == SoftwareStats.GetAppName) && pos == TimeSpan.MinValue)
            {
                await context.EditResponseAsync(GeneralStrings.VoicePlayingAzuraCast);
                return;
            }

            DiscordEmbed embed = EmbedBuilder.BuildMusicStreamingNowPlayingEmbed(track, pos);

            await context.EditResponseAsync(embed);
        }

        [Command("pause"), Description("Pause the current played song.")]
        public async ValueTask PauseAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(PauseAsync), context.User.GlobalName);

            LavalinkTrack? track = await _musicStreaming.NowPlayingAsync(context);
            if (track is null)
            {
                await context.EditResponseAsync(GeneralStrings.VoiceNothingPlaying);
                return;
            }
            else if (track.Author == AppName && track.Title == AppName && track.Identifier == AppName)
            {
                await context.EditResponseAsync(GeneralStrings.VoicePlayingAzuraCast);
                return;
            }

            if (!await _musicStreaming.PauseAsync(context))
                return;

            await context.EditResponseAsync("The music has been paused.");
        }

        [Command("play"), Description("Choose a provider and play the track via the url.")]
        public async ValueTask PlayAsync
        (
            SlashCommandContext context,
            [Description("The provider you want to search for."), SlashChoiceProvider<MusicStreamingPlatformProvider>] string provider,
            [Description("The url of the track you want to play.")] string track,
            [Description("The volume which should be set. This is only respected when no music is being played.")] int volume = 50
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(PlayAsync), context.User.GlobalName);

            if (volume is not 50)
            {
                MusicStreamingEntity? ms = await _dbActions.ReadMusicStreamingAsync(context.Guild.Id);
                if (ms is null)
                {
                    _logger.DatabaseMusicStreamingNotFound(context.Guild.Id);
                    await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                    return;
                }

                volume = ms.Volume;
            }

            if (volume is < 0 or > 100)
            {
                await context.RespondAsync(GeneralStrings.VolumeInvalid, true);
                return;
            }

            string? text = await _musicStreaming.PlayMusicAsync(context, track, new(provider), volume);
            if (string.IsNullOrEmpty(text))
                return;

            await context.EditResponseAsync(text);
        }

        [Command("play-mount"), Description("Choose a mount point of the station to play it."), ModuleActivatedCheck([AzzyModules.AzuraCast]), AzuraCastOnlineCheck]
        public async ValueTask PlayMountAsync
        (
            SlashCommandContext context,
            [Description("The station you want play."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("The mount point of the station."), SlashAutoCompleteProvider<AzuraCastMountAutocomplete>] int mountPoint,
            [Description("The volume which should be set. This is only respected when no music is being played.")] int volume = 50
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(PlayMountAsync), context.User.GlobalName);

            if (volume is not 50)
            {
                MusicStreamingEntity? ms = await _dbActions.ReadMusicStreamingAsync(context.Guild.Id);
                if (ms is null)
                {
                    _logger.DatabaseMusicStreamingNotFound(context.Guild.Id);
                    await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                    return;
                }

                volume = ms.Volume;
            }

            if (volume is < 0 or > 100)
            {
                await context.RespondAsync(GeneralStrings.VolumeInvalid, true);
                return;
            }

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = ac.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, ac.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrEmpty(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(ac.AdminApiKey);
            Uri baseUrl = new(Crypto.Decrypt(ac.BaseUrl));

            AzuraNowPlayingDataRecord? nowPlaying;
            try
            {
                nowPlaying = await _azuraCast.GetNowPlayingAsync(baseUrl,apiKey, station);
                if (nowPlaying is null)
                    throw new HttpRequestException("NowPlaying is null.");
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

            if (!await _musicStreaming.PlayMountMusicAsync(context, mount, volume))
                return;

            await context.EditResponseAsync(GeneralStrings.VoicePlayMount.Replace("%station%", nowPlaying.Station.Name, StringComparison.OrdinalIgnoreCase));
        }

        [Command("queue"), Description("Shows the songs which will be played after this one.")]
        public async ValueTask QueueAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(QueueAsync), context.User.GlobalName);

            IEnumerable<ITrackQueueItem>? history = await _musicStreaming.HistoryAsync(context, true);
            if (history?.Any() is not true)
            {
                await context.EditResponseAsync("There are no upcoming songs.");
                return;
            }

            DiscordEmbed embed = EmbedBuilder.BuildMusicStreamingHistoryEmbed(history, true);
            await context.EditResponseAsync(embed);
        }

        [Command("queue-clear"), Description("Clears the whole queue or only one song from it.")]
        public async ValueTask QueueClearAsync
        (
            SlashCommandContext context,
            [Description("The number of the song which you want to clear. Use the queue command first to get all song numbers.")] int songNumber = -1
        )
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(QueueClearAsync), context.User.GlobalName);

            if (!await _musicStreaming.ClearQueueAsync(context, songNumber))
                return;

            await context.EditResponseAsync((songNumber is -1) ? "The queue was cleared successfully." : $"I removed the song with the number **{songNumber}**.");
        }

        [Command("resume"), Description("Resume the paused player and play music again.")]
        public async ValueTask ResumeAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(ResumeAsync), context.User.GlobalName);

            LavalinkTrack? track = await _musicStreaming.NowPlayingAsync(context);
            if (track is null)
            {
                await context.EditResponseAsync(GeneralStrings.VoiceNothingPlaying);
                return;
            }
            else if (track.Author == AppName && track.Title == AppName && track.Identifier == AppName)
            {
                await context.EditResponseAsync(GeneralStrings.VoicePlayingAzuraCast);
                return;
            }

            if (!await _musicStreaming.ResumeAsync(context))
                return;

            await context.EditResponseAsync("The music has been resumed!");
        }

        [Command("skip"), Description("Skip the current played song and go over to the next one")]
        public async ValueTask SkipAsync
        (
            SlashCommandContext context,
            [Description("Specify how many songs you want to skip.")] int count = 1
        )
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(SkipAsync), context.User.GlobalName);

            LavalinkTrack? track = await _musicStreaming.NowPlayingAsync(context);
            if (track is null)
            {
                await context.EditResponseAsync(GeneralStrings.VoiceNothingPlaying);
                return;
            }
            else if (track.Author == AppName && track.Title == AppName && track.Identifier == AppName)
            {
                await context.EditResponseAsync(GeneralStrings.VoicePlayingAzuraCast);
                return;
            }

            if (!await _musicStreaming.SkipSongAsync(context, count))
                return;

            await context.EditResponseAsync($"I skipped **{count}** {((count is 1) ? "song" : "songs")}.");
        }

        [Command("stop"), Description("Stop the music.")]
        public async ValueTask StopAsync
        (
            SlashCommandContext context,
            [Description("Leave the voice channel."), SlashChoiceProvider<BooleanYesNoStateProvider>] int leave = 0
        )
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(StopAsync), context.User.GlobalName);

            bool leaving = leave is 1;
            if (!await _musicStreaming.StopMusicAsync(context, leaving))
                return;

            string response = (leaving) ? GeneralStrings.VoiceStopLeft : GeneralStrings.VoiceStop;

            await context.EditResponseAsync(response);
        }

        [Command("streaming-nowplaying-embed"), Description("Configure the channel where the now playing embed should be sent. Leave empty to remove it.")]
        public async ValueTask StreamingNowPlayingEmbedAsync
        (
            SlashCommandContext context,
            [Description("The channel where the now playing embed should be sent.")] DiscordChannel? channel = null
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(StreamingNowPlayingEmbedAsync), context.User.GlobalName);

            string response = string.Empty;

            MusicStreamingEntity? ms = await _dbActions.ReadMusicStreamingAsync(context.Guild.Id);
            if (channel is null && ms is not null)
            {
                if (ms.NowPlayingEmbedChannelId is 0)
                {
                    response = "There is no now playing embed channel set for music streaming.";
                }
                else
                {
                    DiscordChannel? oldChannel = await context.Guild.GetChannelAsync(ms.NowPlayingEmbedChannelId);
                    if (oldChannel is null)
                    {
                        response = "The currently set now playing embed channel does not exist anymore.";
                    }
                    else
                    {
                        DiscordMessage? oldMessage = await oldChannel.GetMessageAsync(ms.NowPlayingEmbedMessageId);
                        if (oldMessage is not null)
                        {
                            try
                            {
                                await oldMessage.DeleteAsync();
                            }
                            catch (NotFoundException)
                            {
                                response = "The currently set now playing embed message does not exist anymore.";
                            }
                        }
                    }
                }
            }

            if (ms is null)
            {
                await _dbActions.CreateMusicStreamingAsync(context.Guild.Id, nowPlayingEmbedChannelId: channel?.Id ?? 0, nowPlayingEmbedMessageId: 0);
            }
            else
            {
                await _dbActions.UpdateMusicStreamingAsync(context.Guild.Id, nowPlayingEmbedChannelId: channel?.Id ?? 0, nowPlayingEmbedMessageId: 0);
            }

            if (string.IsNullOrEmpty(response))
            {
                response = (channel is null)
                    ? "I removed the now playing embed message and the configuration for the channel of music streaming."
                    : $"I set the now playing embed channel to **{channel.Mention}** for music streaming.";
            }

            await context.EditResponseAsync(response);
        }
    }
}
