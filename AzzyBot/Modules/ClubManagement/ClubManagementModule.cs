using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.ExceptionHandling;
using AzzyBot.Modules.AzuraCast;
using AzzyBot.Modules.AzuraCast.Models;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Enums;
using AzzyBot.Settings.ClubManagement;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Modules.ClubManagement;

internal sealed class ClubManagementModule : BaseModule
{
    private static Timer? ClubClosingTimer;
    private readonly TimeSpan ClubCloseTimeStart = ClubManagementSettings.ClubClosingTimeStart;
    private readonly TimeSpan ClubCloseTimeEnd = ClubManagementSettings.ClubClosingTimeEnd;
    private static CoreFileLock? ClubBotStatusLock;

    internal static DateTime ClubOpening { get; private set; } = DateTime.MinValue;
    internal static DateTime ClubClosing { get; private set; } = DateTime.MinValue;
    internal static bool ClubClosingInitiated { get; private set; }
    internal static DateTime SetClubOpening(in DateTime time) => ClubOpening = time;
    internal static DateTime SetClubClosing(in DateTime time) => ClubClosing = time;
    internal static void SetClubClosingInitiated(bool value) => ClubClosingInitiated = value;
    internal override void RegisterCommands(SlashCommandsExtension slashCommandsExtension, ulong? serverId) => slashCommandsExtension.RegisterCommands<ClubManagementCommands>(serverId);

    internal override void RegisterFileLocks()
    {
        const string fileName = nameof(CoreFileNamesEnum.ClubBotStatusJSON);
        string[] directories = [nameof(CoreFileDirectoriesEnum.Customization)];
        ClubBotStatusLock = new(fileName, directories);
    }

    internal override void DisposeFileLocks() => ClubBotStatusLock?.Dispose();
    internal override void StopTimers() => StopClubClosingTimer();

    protected override void HandleModuleEvent(ModuleEvent evt)
    {
        switch (evt.Type)
        {
            case ModuleEventType.CheckForSystemGeneratedlistId:
                evt.ResultBool = CheckForSystemGeneratedPlaylist(evt.ParameterInt);
                break;

            case ModuleEventType.CheckForDeniedPlaylistId:
                evt.ResultBool = CheckForDeniedPlaylist(evt.ParameterInt);
                break;

            case ModuleEventType.CheckIfUserHasStaffRole:
                if (evt.ResultMember is null)
                    throw new InvalidOperationException("Can't check if user has staff role, evt.ResultMember is null");

                evt.ResultBool = CoreDiscordCommands.CheckIfUserHasRole(evt.ResultMember, ClubManagementSettings.StaffRoleId);
                break;

            case ModuleEventType.GlobalTimerTick:
                if (ClubManagementSettings.AutomaticClubClosingCheck)
                {
                    DateTime now = DateTime.Now;
                    TimeSpan nowTod = now.TimeOfDay;
                    if (nowTod >= ClubCloseTimeStart && nowTod <= ClubCloseTimeEnd)
                        Task.Run(NotifyUserIfClubIsClosedAsync);
                }

                break;

            case ModuleEventType.CheckIfNowPlayingSloganShouldChange:
            case ModuleEventType.CheckIfPlaylistChangesAreAppropriate:
            case ModuleEventType.CheckIfSongRequestsAreAppropriate:
                evt.ResultBool = Task.Run(CheckIfClubIsOpenAsync).Result;
                evt.ResultReason = (evt.ResultBool) ? "Open" : "Closed";
                break;

            case ModuleEventType.GetClubClosedTime:
                evt.ResultTimeSpan = ClubCloseTimeStart;
                break;

            case ModuleEventType.GetClubOpeningTime:
                evt.ResultTimeSpan = ClubCloseTimeEnd;
                break;
        }
    }

    internal static bool IsMusicServerOnline()
    {
        ModuleEvent evt = new(ModuleEventType.CheckForMusicServer);
        BroadcastModuleEvent(evt);
        return evt.ResultBool;
    }

    internal override void Activate() => ModuleStates.ActivateClubManagement();

    internal static void StartClubClosingTimer()
    {
        ClubClosingTimer = new(new TimerCallback(ClubClosingTimerTimeout), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        ExceptionHandler.LogMessage(LogLevel.Information, "ClubClosingTimer started!");
    }

    internal static async Task<bool> CheckIfClubIsOpenAsync()
    {
        List<PlaylistModel> playlists = await AzuraCastServer.GetPlaylistsAsync();
        List<PlaylistModel> closed = await AzuraCastServer.GetPlaylistsAsync(ClubManagementSettings.AzuraClosedPlaylist);

        if (closed.Count != 1)
            return false;

        foreach (PlaylistModel playlist in playlists)
        {
            if (playlist.Id != ClubManagementSettings.AzuraAllSongsPlaylist && playlist.Id != ClubManagementSettings.AzuraClosedPlaylist && playlist.Is_enabled)
                return true;
        }

        NowPlayingData nowPlaying = await AzuraCastServer.GetNowPlayingAsync();
        string activePlaylist = nowPlaying.Now_Playing.Playlist;

        return activePlaylist != closed[0].Name;
    }

    internal static void StopClubClosingTimer()
    {
        if (ClubClosingTimer is null)
            return;

        ClubClosingTimer.Change(Timeout.Infinite, Timeout.Infinite);
        ClubClosingTimer.Dispose();
        ClubClosingTimer = null;
        ExceptionHandler.LogMessage(LogLevel.Information, "ClubClosingTimer stopped");
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "General Exception is there to log unkown exceptions")]
    private static async void ClubClosingTimerTimeout(object? o)
    {
        try
        {
            if (!IsMusicServerOnline())
                return;

            if (!await CheckIfClubIsOpenAsync())
            {
                if (ClubClosingTimer is null)
                    throw new InvalidOperationException("ClubClosingTimer is null");

                StopClubClosingTimer();
                SetClubClosingInitiated(false);
            }
        }
        catch (Exception ex)
        {
            // System.Threading.Timer just eats exceptions as far as I know so best to log them here.
            await ExceptionHandler.LogErrorAsync(ex);
        }
    }

    private static async Task NotifyUserIfClubIsClosedAsync()
    {
        if (!IsMusicServerOnline() || ClubClosingInitiated)
            return;

        if (!await CheckIfClubIsOpenAsync())
            return;

        NowPlayingData data = await AzuraCastServer.GetNowPlayingAsync();

        if (data.Listeners.Current != 0)
            return;

        await ClubControls.CloseClubAsync();
        await ClubControls.SendClubClosingStatisticsAsync(await Program.SendMessageAsync(ClubManagementSettings.ClubNotifyChannelId, string.Empty, ClubEmbedBuilder.BuildCloseClubEmbed(Program.GetDiscordClientUserName, Program.GetDiscordClientAvatarUrl, true)));
    }

    internal static bool CheckForSystemGeneratedPlaylist(int playlistId) => playlistId == ClubManagementSettings.AzuraAllSongsPlaylist;
    internal static bool CheckForDeniedPlaylist(int playlistId) => playlistId == ClubManagementSettings.AzuraAllSongsPlaylist || playlistId == ClubManagementSettings.AzuraClosedPlaylist;
}
