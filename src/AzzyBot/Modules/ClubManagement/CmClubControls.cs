using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using AzzyBot.Modules.AzuraCast;
using AzzyBot.Modules.AzuraCast.Models;
using AzzyBot.Modules.ClubManagement.Models;
using AzzyBot.Modules.ClubManagement.Settings;
using AzzyBot.Modules.ClubManagement.Strings;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Enums;
using AzzyBot.Modules.Core.Settings;
using DSharpPlus;
using DSharpPlus.Entities;

namespace AzzyBot.Modules.ClubManagement;

internal static class CmClubControls
{
    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is not security relevant as it's a simple random generated number")]
    internal static async Task CloseClubAsync()
    {
        // Get every playlist info
        List<AcPlaylistModel>? playlists = await AcServer.GetPlaylistsAsync();

        if (playlists is null)
            throw new InvalidOperationException($"{nameof(playlists)} is null!");

        foreach (AcPlaylistModel list in playlists)
        {
            //
            // Get the info about the specific playlist
            // IT'S ALWAYS playlist[0]!
            //

            AcPlaylistModel playlist = (await AcServer.GetPlaylistsAsync(list.Id))[0] ?? throw new InvalidOperationException("No playlist found!");

            //
            // Check the id of the playlist
            // if the playlist is all songs or closed
            // check if disabled, if yes enable
            // if the playlist is none
            // check if enabled, if yes disable
            //

            if (playlist.Id == CmSettings.AzuraAllSongsPlaylist || playlist.Id == CmSettings.AzuraClosedPlaylist)
            {
                if (!playlist.Is_enabled)
                    await AcServer.TogglePlaylistAsync(playlist.Id);
            }
            else if (playlist.Is_enabled)
            {
                await AcServer.TogglePlaylistAsync(playlist.Id);
            }
        }

        await AcServer.ChangeSongRequestAvailabilityAsync(false);
        CmModule.SetClubClosingInitiated(true);
        CmModule.SetClubClosing(DateTime.Now);
        CmTimer.StartClubClosingTimer();

        // Set the right bot status
        string[] directories = [nameof(CoreFileDirectoriesEnum.Customization), nameof(CoreFileDirectoriesEnum.ClubManagement)];
        string json = await CoreFileOperations.GetFileContentAsync(nameof(CoreFileNamesEnum.ClubBotStatusJSON), directories);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("json is empty!");

        BotStatus botStatus = JsonSerializer.Deserialize<BotStatus>(json) ?? throw new InvalidOperationException($"{nameof(botStatus)} is null!");

        int status = CoreSettings.BotStatus;
        int type = CoreSettings.BotActivity;
        string doing = CoreSettings.BotDoing;
        string? streamUrl = CoreSettings.BotStreamUrl;
        bool set = false;

        int number = new Random().Next(0, botStatus.ClubBotStatusList.Count);

        if (number == botStatus.ClubBotStatusList.Count)
            set = true;

        if (!set)
        {
            status = botStatus.ClubBotStatusList[number].BotStatus;
            type = botStatus.ClubBotStatusList[number].BotActivity;
            doing = botStatus.ClubBotStatusList[number].BotDoing;
            streamUrl = botStatus.ClubBotStatusList[number].BotStreamUrl;
        }

        await AzzyBot.SetBotStatusAsync(status, type, doing, streamUrl);
    }

    internal static async Task<string> OpenClubAsync(string playlistId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playlistId, nameof(playlistId));

        int id = Convert.ToInt32(playlistId, CultureInfo.InvariantCulture);
        AcPlaylistModel playlist = (await AcServer.GetPlaylistsAsync(id))[0] ?? throw new InvalidOperationException("No playlist found!");

        // Ensure that Closed is disabled and AllSongs is enabled
        AcPlaylistModel closed = (await AcServer.GetPlaylistsAsync(CmSettings.AzuraClosedPlaylist))[0] ?? throw new InvalidOperationException("Closed playlist not found!");
        AcPlaylistModel allSongs = (await AcServer.GetPlaylistsAsync(CmSettings.AzuraAllSongsPlaylist))[0] ?? throw new InvalidOperationException("All songs playlist not found!");

        if (closed.Is_enabled)
            await AcServer.TogglePlaylistAsync(CmSettings.AzuraClosedPlaylist);

        if (!allSongs.Is_enabled)
            await AcServer.TogglePlaylistAsync(CmSettings.AzuraAllSongsPlaylist);

        // Activate the playlist
        await AcServer.TogglePlaylistAsync(id);

        // Ensure that Song Requests are enabled
        if (!await AcServer.CheckIfSongRequestsAreAllowedAsync())
            await AcServer.ChangeSongRequestAvailabilityAsync(true);

        CmTimer.StopClubClosingTimer();
        CmModule.SetClubOpening(DateTime.Now);
        CmModule.SetClubClosingInitiated(false);

        // Set the right bot status
        string[] directories = [nameof(CoreFileDirectoriesEnum.Customization), nameof(CoreFileDirectoriesEnum.ClubManagement)];
        string json = await CoreFileOperations.GetFileContentAsync(nameof(CoreFileNamesEnum.ClubBotStatusJSON), directories);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("json is empty!");

        BotStatus botStatus = JsonSerializer.Deserialize<BotStatus>(json) ?? throw new InvalidOperationException($"{nameof(botStatus)} is null!");

        int status = botStatus.BotStatus;
        int type = botStatus.BotActivity;
        string doing = botStatus.BotDoing;
        string streamUrl = botStatus.BotStreamUrl;

        await AzzyBot.SetBotStatusAsync(status, type, doing, streamUrl);

        return playlist.Name;
    }

    internal static async Task SendClubClosingStatisticsAsync(DiscordMessage message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        DiscordThreadChannel threadChannel = await message.CreateThreadAsync(CmStringBuilder.CommandCloseClubThreadTitle, AutoArchiveDuration.Hour, CmStringBuilder.CommandCloseClubThreadReason);
        await threadChannel.SendMessageAsync(string.Empty, await CmEmbedBuilder.BuildClubStatisticsEmbedAsync(AzzyBot.GetDiscordClientUserName, AzzyBot.GetDiscordClientAvatarUrl));
    }
}
