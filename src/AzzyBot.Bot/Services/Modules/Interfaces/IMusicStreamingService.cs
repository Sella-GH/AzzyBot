using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DSharpPlus.Commands.Processors.SlashCommands;

using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;

namespace AzzyBot.Bot.Services.Modules.Interfaces;

public interface IMusicStreamingService
{
    LavalinkPlayer? GetLavalinkPlayer(ulong guildId);
    Task<bool> CheckIfPlayedMusicIsStationAsync(SlashCommandContext context, string station);
    Task<bool> ClearQueueAsync(SlashCommandContext context, int position = -1);
    TimeSpan? GetCurrentPosition(ulong guildId);
    Task<TimeSpan?> GetCurrentPositionAsync(SlashCommandContext context);
    Task<IEnumerable<ITrackQueueItem>?> HistoryAsync(SlashCommandContext context, bool queue = false);
    Task<bool> JoinChannelAsync(SlashCommandContext context);
    LavalinkTrack? NowPlaying(ulong guildId);
    Task<LavalinkTrack?> NowPlayingAsync(SlashCommandContext context);
    Task<bool> PauseAsync(SlashCommandContext context);
    Task<string?> PlayMusicAsync(SlashCommandContext context, string query, TrackSearchMode searchMode, float volume);
    Task<bool> PlayMountMusicAsync(SlashCommandContext context, string mountPoint, float volume);
    Task<bool> ResumeAsync(SlashCommandContext context);
    Task<bool> SetVolumeAsync(SlashCommandContext context, float volume);
    Task<bool> SkipSongAsync(SlashCommandContext context, int count = 1);
    Task<bool> StopMusicAsync(SlashCommandContext context, bool disconnect = false);
}
