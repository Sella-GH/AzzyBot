using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using AzzyBot.Modules.AzuraCast;
using AzzyBot.Modules.AzuraCast.Models;
using AzzyBot.Modules.ClubManagement.Models;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Enums;
using AzzyBot.Settings.ClubManagement;
using AzzyBot.Settings.Core;
using AzzyBot.Strings.ClubManagement;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace AzzyBot.Modules.ClubManagement;

internal static class ClubControls
{
    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is not security relevant as it's a simple random generated number")]
    internal static async Task CloseClubAsync()
    {
        // Get every playlist info
        List<PlaylistModel>? playlists = await AzuraCastServer.GetPlaylistsAsync();

        if (playlists is null)
            throw new InvalidOperationException($"{nameof(playlists)} is null!");

        foreach (PlaylistModel list in playlists)
        {
            //
            // Get the info about the specific playlist
            // IT'S ALWAYS playlist[0]!
            //

            List<PlaylistModel> playlist = await AzuraCastServer.GetPlaylistsAsync(list.Id);

            //
            // Check the id of the playlist
            // if the playlist is all songs or closed
            // check if disabled, if yes enable
            // if the playlist is none
            // check if enabled, if yes disable
            //

            if (playlist is null)
                throw new InvalidOperationException($"{nameof(playlist)} is null!");

            if (playlist[0].Id == ClubManagementSettings.AzuraAllSongsPlaylist || playlist[0].Id == ClubManagementSettings.AzuraClosedPlaylist)
            {
                if (!playlist[0].Is_enabled)
                {
                    await AzuraCastServer.TogglePlaylistAsync(playlist[0].Id);
                }
            }
            else if (playlist[0].Is_enabled)
            {
                await AzuraCastServer.TogglePlaylistAsync(playlist[0].Id);
            }
        }

        await AzuraCastServer.ChangeSongRequestAvailabilityAsync(false);
        ClubManagementModule.SetClubClosingInitiated(true);
        ClubManagementModule.SetClubClosing(DateTime.Now);
        ClubManagementModule.StartClubClosingTimer();

        // Set the right bot status
        string[] directories = [nameof(CoreFileDirectoriesEnum.Customization), nameof(CoreFileDirectoriesEnum.ClubManagement)];
        string json = await CoreFileOperations.GetFileContentAsync(nameof(CoreFileNamesEnum.ClubBotStatusJSON), directories);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("json is empty!");

        BotStatus? botStatus = JsonConvert.DeserializeObject<BotStatus>(json);

        int status = CoreSettings.BotStatus;
        int type = CoreSettings.BotActivity;
        string doing = CoreSettings.BotDoing;
        string? streamUrl = CoreSettings.BotStreamUrl;
        bool set = false;

        if (botStatus is null)
            throw new InvalidOperationException($"{nameof(botStatus)} is null!");

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

        await Program.SetBotStatusAsync(status, type, doing, streamUrl);
    }

    internal static async Task<string> OpenClubAsync(string playlistId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playlistId, nameof(playlistId));

        int id = Convert.ToInt32(playlistId, CultureInfo.InvariantCulture);
        List<PlaylistModel> playlist = await AzuraCastServer.GetPlaylistsAsync(id);

        if (playlist.Count != 1)
            throw new InvalidOperationException("There are more playlists than one!");

        // Ensure that Closed is disabled and AllSongs is enabled
        List<PlaylistModel> closed = await AzuraCastServer.GetPlaylistsAsync(ClubManagementSettings.AzuraClosedPlaylist);
        List<PlaylistModel> allSongs = await AzuraCastServer.GetPlaylistsAsync(ClubManagementSettings.AzuraAllSongsPlaylist);

        if (closed.Count == 1 && closed[0].Is_enabled)
            await AzuraCastServer.TogglePlaylistAsync(ClubManagementSettings.AzuraClosedPlaylist);

        if (allSongs.Count == 1 && !allSongs[0].Is_enabled)
            await AzuraCastServer.TogglePlaylistAsync(ClubManagementSettings.AzuraAllSongsPlaylist);

        // Activate the playlist
        await AzuraCastServer.TogglePlaylistAsync(id);

        // Ensure that Song Requests are enabled
        if (!await AzuraCastServer.CheckIfSongRequestsAreAllowedAsync())
            await AzuraCastServer.ChangeSongRequestAvailabilityAsync(true);

        ClubManagementModule.StopClubClosingTimer();
        ClubManagementModule.SetClubOpening(DateTime.Now);
        ClubManagementModule.SetClubClosingInitiated(false);

        // Set the right bot status
        string[] directories = [nameof(CoreFileDirectoriesEnum.Customization), nameof(CoreFileDirectoriesEnum.ClubManagement)];
        string json = await CoreFileOperations.GetFileContentAsync(nameof(CoreFileNamesEnum.ClubBotStatusJSON), directories);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("json is empty!");

        BotStatus? botStatus = JsonConvert.DeserializeObject<BotStatus>(json);

        int status;
        int type;
        string doing;
        string streamUrl;

        if (botStatus is not null)
        {
            status = botStatus.BotStatus;
            type = botStatus.BotActivity;
            doing = botStatus.BotDoing;
            streamUrl = botStatus.BotStreamUrl;
        }
        else
        {
            throw new InvalidOperationException($"{nameof(botStatus)} is null!");
        }

        await Program.SetBotStatusAsync(status, type, doing, streamUrl);

        return playlist[0].Name;
    }

    internal static async Task SendClubClosingStatisticsAsync(DiscordMessage message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        DiscordThreadChannel threadChannel = await message.CreateThreadAsync(ClubManagementStringBuilder.CommandCloseClubThreadTitle, AutoArchiveDuration.Hour, ClubManagementStringBuilder.CommandCloseClubThreadReason);
        await threadChannel.SendMessageAsync(string.Empty, await ClubEmbedBuilder.BuildClubStatisticsEmbedAsync(Program.GetDiscordClientUserName, Program.GetDiscordClientAvatarUrl));
    }
}
