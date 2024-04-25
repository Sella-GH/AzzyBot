using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Modules.AzuraCast.Models;
using AzzyBot.Modules.AzuraCast.Settings;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Enums;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace AzzyBot.Modules.AzuraCast;

internal sealed class AzuraCastModule : BaseModule
{
    private static bool IsMusicServerOnline = true;
    private static DateTime LastFileCheckRun = DateTime.MinValue;
    private static DateTime LastMusicServerUpdateCheck = DateTime.MinValue;
    private static DateTime LastMusicServerUpdateNotify = DateTime.MinValue;
    private static readonly SemaphoreSlim PingLock = new(1, 1);
    internal static CoreFileLock? FavoriteSongsLock;
    internal static CoreFileLock? FileCacheLock;
    internal static CoreFileLock? PlaylistSlogansLock;

    internal override void RegisterCommands(SlashCommandsExtension slashCommandsExtension, ulong? serverId) => slashCommandsExtension.RegisterCommands<AcCommands>(serverId);

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

        LoggerBase.LogInfo(LoggerBase.GetLogger, "Registered AzuraCast File Locks", null);
    }

    internal override void DisposeFileLocks()
    {
        FavoriteSongsLock?.Dispose();
        FileCacheLock?.Dispose();
        PlaylistSlogansLock?.Dispose();
    }

    protected override async void HandleModuleEvent(ModuleEvent evt)
    {
        switch (evt.Type)
        {
            case ModuleEventType.CheckForMusicServer:
                evt.ResultBool = IsMusicServerOnline;
                break;

            case ModuleEventType.GetAzuraCastApiUrl:
                evt.ResultString = AcSettings.AzuraApiUrl;
                break;

            case ModuleEventType.GetAzuraCastIPv6Availability:
                evt.ResultBool = AcSettings.Ipv6Available;
                break;

            case ModuleEventType.GlobalTimerTick:
                await AcModuleTimerAsync();
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
        await AcServer.CheckIfFilesWereModifiedAsync();
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

    [SuppressMessage("Roslynator", "RCS1208:Reduce 'if' nesting", Justification = "Code clarity")]
    private static async Task AcModuleTimerAsync()
    {
        await PingLock.WaitAsync();
        try
        {
            LoggerBase.LogDebug(LoggerBase.GetLogger, "AzzyBotGlobalTimer ping check for music server", null);
            await PingMusicServerAsync();
        }
        finally
        {
            PingLock.Release();
        }

        if (!IsMusicServerOnline || !AcSettings.AzuraCastApiKeyIsValid)
            return;

        DateTime now = DateTime.Now;

        // 1 hour
        if (AcSettings.AutomaticChecksFileChanges && now - LastFileCheckRun >= TimeSpan.FromHours(0.98))
        {
            LoggerBase.LogDebug(LoggerBase.GetLogger, "AzzyBotGlobalTimer checking for file changes", null);
            LastFileCheckRun = now;
            await AcServer.CheckIfFilesWereModifiedAsync();
        }

        // 6 hours
        if (AcSettings.AutomaticChecksUpdates && now - LastMusicServerUpdateCheck >= TimeSpan.FromHours(5.98))
        {
            LoggerBase.LogDebug(LoggerBase.GetLogger, "AzzyBotGlobalTimer checking for music server updates", null);
            LastMusicServerUpdateCheck = now;
            await CheckForMusicServerUpdatesAsync();
        }
    }

    private static async Task CheckForMusicServerUpdatesAsync()
    {
        if (!IsMusicServerOnline)
            return;

        AcUpdateModel updates = await AcServer.CheckIfMusicServerNeedsUpdatesAsync();

        if (!updates.NeedsReleaseUpdate && !updates.NeedsRollingUpdate)
            return;

        DateTime now = DateTime.Now;
        if (now - LastMusicServerUpdateNotify <= TimeSpan.FromMinutes(14))
            return;

        LastMusicServerUpdateNotify = now;

        List<DiscordEmbed?> embeds = [AcEmbedBuilder.BuildUpdatesAvailableEmbed(updates)];
        if (AcSettings.AutomaticChecksUpdatesShowChangelog)
            embeds.Add(AcEmbedBuilder.BuildUpdatesAvailableChangelogEmbed(updates.RollingUpdatesList, updates.NeedsRollingUpdate));

        await AzzyBot.SendMessageAsync(AcSettings.OutagesChannelId, string.Empty, embeds);
    }

    private static async Task PingMusicServerAsync()
    {
        // When empty the server is offline
        if (string.IsNullOrWhiteSpace(await CoreWebRequests.GetPingTimeAsync(AcSettings.AzuraApiUrl)))
        {
            IsMusicServerOnline = false;

            if (AcSettings.AutomaticChecksServerPing)
                await AzzyBot.SendMessageAsync(AcSettings.OutagesChannelId, string.Empty, [AcEmbedBuilder.BuildServerIsOfflineEmbed(AzzyBot.GetDiscordClientUserName, AzzyBot.GetDiscordClientAvatarUrl, false)]);
        }

        // When the server was previously offline but is online again now
        else if (!IsMusicServerOnline)
        {
            IsMusicServerOnline = true;

            if (AcSettings.AutomaticChecksServerPing)
                await AzzyBot.SendMessageAsync(AcSettings.OutagesChannelId, string.Empty, [AcEmbedBuilder.BuildServerIsOfflineEmbed(AzzyBot.GetDiscordClientUserName, AzzyBot.GetDiscordClientAvatarUrl, true)]);
        }
    }
}
