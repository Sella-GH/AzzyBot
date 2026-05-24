using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using AzzyBot.Bot.Logging;
using AzzyBot.Bot.Models.AzuraCast;
using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Bot.Services.Modules.Interfaces;
using AzzyBot.Bot.Utilities;
using AzzyBot.Core.Utilities;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services.Interfaces;

using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.Modules;

public sealed class AzuraCastFileService(ILogger<AzuraCastFileService> logger, IAzuraCastApiService azuraCast, IDbActions dbActions, IDiscordBotService discordBotService) : IAzuraCastFileService
{
    private readonly ILogger<AzuraCastFileService> _logger = logger;
    private readonly IAzuraCastApiService _azuraCast = azuraCast;
    private readonly IDbActions _dbActions = dbActions;
    private readonly IDiscordBotService _botService = discordBotService;

    public async Task CheckForFileChangesAsync(AzuraCastStationEntity station)
    {
        ArgumentNullException.ThrowIfNull(station);

        if (!Directory.Exists(_azuraCast.FilePath))
            Directory.CreateDirectory(_azuraCast.FilePath);

        Uri baseUrl = new(Crypto.Decrypt(station.AzuraCast.BaseUrl));
        string apiKey = (string.IsNullOrEmpty(station.ApiKey)) ? Crypto.Decrypt(station.AzuraCast.AdminApiKey) : Crypto.Decrypt(station.ApiKey);

        AzuraStationModel? azuraStation = await _azuraCast.GetStationAsync(baseUrl, apiKey, station.StationId);
        if (azuraStation is null)
        {
            await _botService.SendMessageAsync(station.AzuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **station** endpoint on station ID: {station.StationId}.\n{_azuraCast.AzuraCastPermissionsWiki}");
            return;
        }

        IEnumerable<AzuraFilesModel>? onlineFiles = await _azuraCast.GetFilesOnlineBasicAsync(baseUrl, apiKey, station.StationId);
        if (onlineFiles is null)
        {
            await _botService.SendMessageAsync(station.AzuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **files** endpoint on station *{azuraStation.Name}* (ID: {station.StationId}).\n{_azuraCast.AzuraCastPermissionsWiki}");
            return;
        }

        IEnumerable<AzuraFilesModel> localFiles = await _azuraCast.GetFilesLocalAsync(station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId);

        await CheckIfFilesWereModifiedAsync(onlineFiles, localFiles, station, azuraStation.Name, station.AzuraCast.Preferences.NotificationChannelId);
    }

    private async Task CheckIfFilesWereModifiedAsync(IEnumerable<AzuraFilesModel> onlineFiles, IEnumerable<AzuraFilesModel> localFiles, AzuraCastStationEntity station, string stationName, ulong channelId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stationName);

        HashSet<AzuraFilesModel> onlineHashSet = new(onlineFiles, new AzuraFileComparer());
        HashSet<AzuraFilesModel> localHashSet = new(localFiles, new AzuraFileComparer());

        List<AzuraFilesModel> addedFiles = [.. onlineHashSet.Except(localHashSet)];
        List<AzuraFilesModel> removedFiles = [.. localHashSet.Except(onlineHashSet)];
        bool filesChanged = addedFiles.Count is not 0 || removedFiles.Count is not 0;
        await _dbActions.UpdateAzuraCastStationChecksAsync(station.AzuraCast.Guild.UniqueId, station.StationId, updateLastFileChangesCheck: true);
        if (!filesChanged)
            return;

        _logger.BackgroundServiceStationFilesChanged(station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId);

        string addedFileName = Path.Combine(_azuraCast.FilePath, $"{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss-fffffff}-{station.AzuraCast.GuildId}-{station.AzuraCastId}-{station.Id}-{station.StationId}-added.txt");
        string removedFileName = Path.Combine(_azuraCast.FilePath, $"{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss-fffffff}-{station.AzuraCast.GuildId}-{station.AzuraCastId}-{station.Id}-{station.StationId}-removed.txt");
        StringBuilder added = new();
        StringBuilder removed = new();
        List<string> paths = new(addedFiles.Count + removedFiles.Count);

        if (addedFiles.Count is not 0)
        {
            foreach (AzuraFilesModel addedFile in addedFiles)
                added.AppendLine(addedFile.Path);

            await FileOperations.WriteToFileAsync(addedFileName, added.ToString());
            paths.Add(addedFileName);
        }

        if (removedFiles.Count is not 0)
        {
            foreach (AzuraFilesModel removedFile in removedFiles)
                removed.AppendLine(removedFile.Path);

            await FileOperations.WriteToFileAsync(removedFileName, removed.ToString());
            paths.Add(removedFileName);
        }

        await FileOperations.WriteToFileAsync(Path.Combine(_azuraCast.FilePath, $"{station.AzuraCast.GuildId}-{station.AzuraCastId}-{station.Id}-{station.StationId}-files.json"), JsonSerializer.Serialize(onlineFiles, JsonSourceGen.Default.IEnumerableAzuraFilesModel));
        DiscordEmbed embed = EmbedBuilder.BuildAzuraCastFileChangesEmbed(stationName, addedFiles.Count, removedFiles.Count);
        await _botService.SendMessageAsync(channelId, $"Changes in the files of station **{stationName}** detected. Check the details below.", [embed], paths);
    }
}
