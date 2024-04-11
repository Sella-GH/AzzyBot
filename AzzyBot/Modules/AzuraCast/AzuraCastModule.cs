using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.ExceptionHandling;
using AzzyBot.Modules.AzuraCast.Models;
using AzzyBot.Modules.AzuraCast.Settings;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Enums;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Modules.AzuraCast;

internal sealed class AzuraCastModule : BaseModule
{
    private static Timer? AzzyBotGlobalTimer;
    private static bool IsMusicServerOnline = true;
    private static DateTime LastFileCheckRun = DateTime.MinValue;
    private static DateTime LastMusicServerUpdateCheck = DateTime.MinValue;
    private static DateTime LastMusicServerUpdateNotify = DateTime.MinValue;
    private static readonly SemaphoreSlim PingLock = new(1, 1);
    internal static CoreFileLock? FavoriteSongsLock;
    internal static CoreFileLock? FileCacheLock;
    internal static CoreFileLock? PlaylistSlogansLock;

    internal override void RegisterCommands(SlashCommandsExtension slashCommandsExtension, ulong? serverId) => slashCommandsExtension.RegisterCommands<AzuraCastCommands>(serverId);

    internal override void RegisterFileLocks()
    {
        string fileName;
        string[] directory;

        fileName = nameof(CoreFileNamesEnum.FavoriteSongsJSON);
        directory = [nameof(CoreFileDirectoriesEnum.Customization)];
        FavoriteSongsLock = new(fileName, directory);

        fileName = nameof(CoreFileNamesEnum.FileCacheJSON);
        directory = [nameof(CoreFileDirectoriesEnum.Modules), nameof(CoreFileDirectoriesEnum.AzuraCast), nameof(CoreFileDirectoriesEnum.Files)];
        FileCacheLock = new(fileName, directory);

        fileName = nameof(CoreFileNamesEnum.PlaylistSlogansJSON);
        directory = [nameof(CoreFileDirectoriesEnum.Customization)];
        PlaylistSlogansLock = new(fileName, directory);
    }

    internal override void DisposeFileLocks()
    {
        FavoriteSongsLock?.Dispose();
        FileCacheLock?.Dispose();
        PlaylistSlogansLock?.Dispose();
    }

    internal override void StartGlobalTimers() => StartGlobalTimer();
    internal override void StopTimers() => StopGlobalTimer();

    protected override void HandleModuleEvent(ModuleEvent evt)
    {
        switch (evt.Type)
        {
            case ModuleEventType.CheckForMusicServer:
                evt.ResultBool = IsMusicServerOnline;
                break;

            case ModuleEventType.GetAzuraCastApiUrl:
                evt.ResultString = AzuraCastSettings.AzuraApiUrl;
                break;

            case ModuleEventType.GetAzuraCastIPv6Availability:
                evt.ResultBool = AzuraCastSettings.Ipv6Available;
                break;
        }
    }

    internal override void Activate() => ModuleStates.ActivateAzuraCast();

    /// <summary>
    /// Checks if the currently playing slogan should change.
    /// </summary>
    /// <returns>Returns true if the slogan should change, otherwise false.</returns>
    internal static bool CheckIfNowPlayingSloganShouldChange()
    {
        ModuleEvent evt = new(ModuleEventType.CheckIfNowPlayingSloganShouldChange);
        BroadcastModuleEvent(evt);

        if (string.IsNullOrWhiteSpace(evt.ResultReason))
            return true;

        return evt.ResultBool;
    }

    /// <summary>
    /// Checks if changes to the playlist are appropriate.
    /// </summary>
    /// <returns>Returns true if changes to the playlist are appropriate, otherwise false </returns>
    internal static bool CheckIfPlaylistChangesAreAppropriate()
    {
        ModuleEvent evt = new(ModuleEventType.CheckIfPlaylistChangesAreAppropriate);
        BroadcastModuleEvent(evt);

        if (string.IsNullOrWhiteSpace(evt.ResultReason))
            return true;

        return evt.ResultBool;
    }

    /// <summary>
    /// Checks if song requests are appropriate.
    /// </summary>
    /// <returns>Returns true if song requests are appropriate, otherwise false.</returns>
    internal static bool CheckIfSongRequestsAreAppropriate()
    {
        ModuleEvent evt = new(ModuleEventType.CheckIfSongRequestsAreAppropriate);
        BroadcastModuleEvent(evt);

        if (string.IsNullOrWhiteSpace(evt.ResultReason))
            return true;

        return evt.ResultBool;
    }

    /// <summary>
    /// Checks if the given playlist contains all songs.
    /// </summary>
    /// <param name="playlistId">The ID of the playlist to check.</param>
    /// <returns>Returns true if the playlist contains all songs, otherwise false.</returns>
    internal static bool CheckIfSystemGeneratedPlaylist(int playlistId)
    {
        ModuleEvent evt = new(ModuleEventType.CheckForSystemGeneratedlistId)
        {
            ParameterInt = playlistId
        };
        BroadcastModuleEvent(evt);
        return evt.ResultBool;
    }

    /// <summary>
    /// Checks if the playlist is a denied one.
    /// </summary>
    /// <param name="playlistId">The ID of the playlist to check.</param>
    /// <returns>Returns true if the playlist is a denied one, otherwise false.</returns>
    internal static bool CheckIfDeniedPlaylist(int playlistId)
    {
        ModuleEvent evt = new(ModuleEventType.CheckForDeniedPlaylistId)
        {
            ParameterInt = playlistId
        };
        BroadcastModuleEvent(evt);
        return evt.ResultBool;
    }

    internal static TimeSpan GetClubClosedTime()
    {
        ModuleEvent evt = new(ModuleEventType.GetClubClosedTime);
        BroadcastModuleEvent(evt);
        return evt.ResultTimeSpan;
    }

    internal static TimeSpan GetClubOpenedTime()
    {
        ModuleEvent evt = new(ModuleEventType.GetClubOpeningTime);
        BroadcastModuleEvent(evt);
        return evt.ResultTimeSpan;
    }

    internal static async Task CheckIfFilesWereModifiedAsync()
    {
        LastFileCheckRun = DateTime.Now;
        await AzuraCastServer.CheckIfFilesWereModifiedAsync();
    }

    internal static async Task<bool> CheckIfMusicServerIsOnlineAsync()
    {
        await PingLock.WaitAsync();
        try
        {
            await PingMusicServerAsync();
            return IsMusicServerOnline;
        }
        finally
        {
            PingLock.Release();
        }
    }

    private static void StartGlobalTimer()
    {
        AzzyBotGlobalTimer = new(new TimerCallback(AzzyBotGlobalTimerTimeout), null, TimeSpan.Zero, TimeSpan.FromMinutes(15));
        ExceptionHandler.LogMessage(LogLevel.Information, "AzzyBotGlobalTimer started");
    }

    private static void StopGlobalTimer()
    {
        if (AzzyBotGlobalTimer is null)
            return;

        AzzyBotGlobalTimer.Change(Timeout.Infinite, Timeout.Infinite);
        AzzyBotGlobalTimer.Dispose();
        AzzyBotGlobalTimer = null;
        ExceptionHandler.LogMessage(LogLevel.Information, "AzzyBotGlobalTimer stopped");
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "General Exception is there to log unkown exceptions")]
    private static async void AzzyBotGlobalTimerTimeout(object? o)
    {
        try
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "AzzyBotGlobalTimer tick");

            await PingLock.WaitAsync();
            try
            {
                ExceptionHandler.LogMessage(LogLevel.Debug, "AzzyBotGlobalTimer ping check for music server");
                await PingMusicServerAsync();
            }
            finally
            {
                PingLock.Release();
            }

            if (!IsMusicServerOnline)
                return;

            DateTime now = DateTime.Now;

            // 1 hour
            if (AzuraCastSettings.AutomaticFileChangeCheck && now - LastFileCheckRun >= TimeSpan.FromHours(0.98))
            {
                ExceptionHandler.LogMessage(LogLevel.Debug, "AzzyBotGlobalTimer checking for file changes");
                LastFileCheckRun = now;
                await AzuraCastServer.CheckIfFilesWereModifiedAsync();
            }

            // 6 hours
            if (AzuraCastSettings.AutomaticUpdateCheck && now - LastMusicServerUpdateCheck >= TimeSpan.FromHours(5.98))
            {
                ExceptionHandler.LogMessage(LogLevel.Debug, "AzzyBotGlobalTimer checking for music server updates");
                LastMusicServerUpdateCheck = now;
                await CheckForMusicServerUpdatesAsync();
            }

            BroadcastModuleEvent(new ModuleEvent(ModuleEventType.GlobalTimerTick));
        }
        catch (Exception ex)
        {
            // System.Threading.Timer just eats exceptions as far as I know so best to log them here.
            await ExceptionHandler.LogErrorAsync(ex);
        }
    }

    private static async Task CheckForMusicServerUpdatesAsync()
    {
        if (!IsMusicServerOnline)
            return;

        AzuraCastUpdateModel updates = await AzuraCastServer.CheckIfMusicServerNeedsUpdatesAsync();

        if (!updates.NeedsReleaseUpdate && !updates.NeedsRollingUpdate)
            return;

        DateTime now = DateTime.Now;
        if (now - LastMusicServerUpdateNotify <= TimeSpan.FromMinutes(14))
            return;

        LastMusicServerUpdateNotify = now;
        await AzzyBot.SendMessageAsync(AzuraCastSettings.OutagesChannelId, string.Empty, [AzuraCastEmbedBuilder.BuildUpdatesAvailableEmbed(AzzyBot.GetDiscordClientUserName, AzzyBot.GetDiscordClientAvatarUrl, updates)]);
    }

    private static async Task PingMusicServerAsync()
    {
        // When empty the server is offline
        if (string.IsNullOrWhiteSpace(await CoreWebRequests.TryPingAsync(AzuraCastSettings.AzuraApiUrl)))
        {
            IsMusicServerOnline = false;

            if (AzuraCastSettings.AutomaticServerPing)
                await AzzyBot.SendMessageAsync(AzuraCastSettings.OutagesChannelId, string.Empty, [AzuraCastEmbedBuilder.BuildServerIsOfflineEmbed(AzzyBot.GetDiscordClientUserName, AzzyBot.GetDiscordClientAvatarUrl, false)]);
        }

        // When the server was previously offline but is online again now
        else if (!IsMusicServerOnline)
        {
            IsMusicServerOnline = true;

            if (AzuraCastSettings.AutomaticServerPing)
                await AzzyBot.SendMessageAsync(AzuraCastSettings.OutagesChannelId, string.Empty, [AzuraCastEmbedBuilder.BuildServerIsOfflineEmbed(AzzyBot.GetDiscordClientUserName, AzzyBot.GetDiscordClientAvatarUrl, true)]);
        }
    }
}
