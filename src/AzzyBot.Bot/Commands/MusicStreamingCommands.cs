﻿using System;
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
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

using Lavalink4NET.Players;
using Lavalink4NET.Tracks;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class MusicStreamingCommands
{
    [Command("player"), RequireGuild, RequirePermissions(BotPermissions = [DiscordPermission.Connect, DiscordPermission.Speak], UserPermissions = [DiscordPermission.Connect]), ModuleActivatedCheck([AzzyModules.LegalTerms])]
    public sealed class PlayerGroup(ILogger<PlayerGroup> logger, AzuraCastApiService azuraCast, DbActions dbActions, MusicStreamingService musicStreaming)
    {
        private readonly ILogger<PlayerGroup> _logger = logger;
        private readonly AzuraCastApiService _azuraCast = azuraCast;
        private readonly DbActions _dbActions = dbActions;
        private readonly MusicStreamingService _musicStreaming = musicStreaming;

        [Command("change-volume"), Description("Change the volume of the played music.")]
        public async ValueTask ChangeVolumeAsync
        (
            SlashCommandContext context,
            [Description("The volume you want to set.")] int volume
        )
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(ChangeVolumeAsync), context.User.GlobalName);

            if (volume is < 0 or > 100)
            {
                await context.RespondAsync(GeneralStrings.VolumeInvalid, true);
                return;
            }

            if (!await _musicStreaming.SetVolumeAsync(context, volume))
                return;

            await context.EditResponseAsync($"I set the volume to {volume}%.");
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

            if (context.Member.VoiceState?.Channel is null)
            {
                await context.EditResponseAsync(GeneralStrings.VoiceNoUser);
                return;
            }

            if (context.Member.VoiceState.Channel.Users.Contains(await context.Guild.GetMemberAsync(context.Client.CurrentUser.Id)))
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
            else if ((track.Author is "AzzyBot.Bot" || track.Title is "AzzyBot.Bot" || track.Identifier is "AzzyBot.Bot") && pos == TimeSpan.MinValue)
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
            else if (track.Author is "AzzyBot.Bot" && track.Title is "AzzyBot.Bot" && track.Identifier is "AzzyBot.Bot")
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

            _logger.CommandRequested(nameof(PlayAsync), context.User.GlobalName);

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
            [Description("The volume which should be set. This is only respected when no music is being played.")] int volume = 100
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(PlayMountAsync), context.User.GlobalName);

            if (volume is < 0 or > 100)
            {
                await context.RespondAsync(GeneralStrings.VolumeInvalid, true);
                return;
            }

            AzuraCastEntity? azura = await _dbActions.GetAzuraCastAsync(context.Guild.Id);
            if (azura is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraNowPlayingDataRecord? nowPlaying;
            try
            {
                nowPlaying = await _azuraCast.GetNowPlayingAsync(new(Crypto.Decrypt(azura.BaseUrl)), station);
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
            else if (track.Author is "AzzyBot.Bot" && track.Title is "AzzyBot.Bot" && track.Identifier is "AzzyBot.Bot")
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
            else if (track.Author is "AzzyBot.Bot" && track.Title is "AzzyBot.Bot" && track.Identifier is "AzzyBot.Bot")
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
    }
}
