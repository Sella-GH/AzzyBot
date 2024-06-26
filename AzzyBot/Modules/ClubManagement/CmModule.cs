using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Modules.AzuraCast;
using AzzyBot.Modules.AzuraCast.Models;
using AzzyBot.Modules.ClubManagement.Settings;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Enums;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace AzzyBot.Modules.ClubManagement;

internal class CmModule : BaseModule
{
    protected static readonly TimeSpan ClubCloseTimeStart = CmSettings.ClubClosingTimeStart;
    protected static readonly TimeSpan ClubCloseTimeEnd = CmSettings.ClubClosingTimeEnd;
    private static CoreFileLock? ClubBotStatusLock;

    internal static DateTime ClubOpening { get; private set; } = DateTime.MinValue;
    internal static DateTime ClubClosing { get; private set; } = DateTime.MinValue;
    internal static bool ClubClosingInitiated { get; private set; }
    internal static DateTime SetClubOpening(in DateTime time) => ClubOpening = time;
    internal static DateTime SetClubClosing(in DateTime time) => ClubClosing = time;
    internal static void SetClubClosingInitiated(bool value) => ClubClosingInitiated = value;
    internal override void RegisterCommands(SlashCommandsExtension slashCommandsExtension, ulong? serverId) => slashCommandsExtension.RegisterCommands<CmCommands>(serverId);

    internal override void RegisterFileLocks()
    {
        const string fileName = nameof(CoreFileNamesEnum.ClubBotStatusJSON);
        string[] directories = [nameof(CoreFileDirectoriesEnum.Customization)];
        ClubBotStatusLock = new(fileName, directories);

        LoggerBase.LogInfo(LoggerBase.GetLogger, "Registered ClubManagement File Locks", null);
    }

    internal override void DisposeFileLocks() => ClubBotStatusLock?.Dispose();
    internal override void StopTimers() => CmTimer.StopClubClosingTimer();

    protected override async void HandleModuleEvent(ModuleEvent evt)
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

                evt.ResultBool = CoreDiscordChecks.CheckIfUserHasRole(evt.ResultMember, CmSettings.StaffRoleId);
                break;

            case ModuleEventType.GlobalTimerTick:
                if (CmSettings.AutomaticClubClosingCheck)
                    await ClubClosingCheckAsync();

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

    internal static async Task<bool> CheckIfClubIsOpenAsync()
    {
        List<AcPlaylistModel> playlists = await AcServer.GetPlaylistsAsync();
        List<AcPlaylistModel> closed = await AcServer.GetPlaylistsAsync(CmSettings.AzuraClosedPlaylist);

        if (closed.Count != 1)
            return false;

        foreach (AcPlaylistModel playlist in playlists)
        {
            if (playlist.Id != CmSettings.AzuraAllSongsPlaylist && playlist.Id != CmSettings.AzuraClosedPlaylist && playlist.Is_enabled)
                return true;
        }

        NowPlayingData nowPlaying = await AcServer.GetNowPlayingAsync();
        string activePlaylist = nowPlaying.Now_Playing.Playlist;

        return activePlaylist != closed[0].Name;
    }

    private static async Task ClubClosingCheckAsync()
    {
        DateTime now = DateTime.Now;
        TimeSpan nowTod = now.TimeOfDay;
        if (nowTod >= ClubCloseTimeStart && nowTod <= ClubCloseTimeEnd)
            await NotifyUserIfClubIsClosedAsync();
    }

    private static async Task NotifyUserIfClubIsClosedAsync()
    {
        if (!IsMusicServerOnline() || ClubClosingInitiated)
            return;

        if (!await CheckIfClubIsOpenAsync())
            return;

        NowPlayingData data = await AcServer.GetNowPlayingAsync();

        if (data.Listeners.Current != 0)
            return;

        await CmClubControls.CloseClubAsync();

        DiscordEmbed embed = CmEmbedBuilder.BuildCloseClubEmbed(AzzyBot.GetDiscordClientUserName, AzzyBot.GetDiscordClientAvatarUrl, true);
        await CmClubControls.SendClubClosingStatisticsAsync(await AzzyBot.SendMessageAsync(CmSettings.ClubNotifyChannelId, string.Empty, [embed]));
    }

    internal static bool CheckForSystemGeneratedPlaylist(int playlistId) => playlistId == CmSettings.AzuraAllSongsPlaylist;
    internal static bool CheckForDeniedPlaylist(int playlistId) => playlistId == CmSettings.AzuraAllSongsPlaylist || playlistId == CmSettings.AzuraClosedPlaylist;
}
