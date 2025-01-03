﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;
using AzzyBot.Data.Settings;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Services.Modules;

public sealed class CoreServiceHost(ILogger<CoreServiceHost> logger, IOptions<AzzyBotSettings> azzySettings, IOptions<CoreUpdaterSettings> updaterSettings, IOptions<DatabaseSettings> dbSettings, IOptions<DiscordStatusSettings> discordSettings, IOptions<MusicStreamingSettings> musicStreamingSettings, AzzyDbContext dbContext) : IHostedService
{
    private readonly ILogger<CoreServiceHost> _logger = logger;
    private readonly AzzyBotSettings _azzySettings = azzySettings.Value;
    private readonly CoreUpdaterSettings _updaterSettings = updaterSettings.Value;
    private readonly DatabaseSettings _dbSettings = dbSettings.Value;
    private readonly DiscordStatusSettings _discordSettings = discordSettings.Value;
    private readonly MusicStreamingSettings _musicStreamingSettings = musicStreamingSettings.Value;
    private readonly AzzyDbContext _dbContext = dbContext;
    private readonly Task _completed = Task.CompletedTask;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string name = SoftwareStats.GetAppName;
        string version = SoftwareStats.GetAppVersion;
        string arch = HardwareStats.GetSystemOsArch;
        string os = HardwareStats.GetSystemOs;
        string dotnet = SoftwareStats.GetAppDotNetVersion;

        _logger.BotStarting(name, version, os, arch, dotnet);

        if (!string.IsNullOrWhiteSpace(_dbSettings.NewEncryptionKey) && (_dbSettings.NewEncryptionKey != _dbSettings.EncryptionKey))
            await ReencryptDatabaseAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.BotStopping();

        return _completed;
    }

    private async Task ReencryptDatabaseAsync()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(_dbSettings.EncryptionKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(_dbSettings.NewEncryptionKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(_azzySettings.SettingsFile);

        _logger.DatabaseReencryptionStart();

        byte[] newEncryptionKey = Encoding.UTF8.GetBytes(_dbSettings.NewEncryptionKey);

        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync();

        List<AzuraCastEntity> azuraCast = await _dbContext.AzuraCast.ToListAsync();
        List<AzuraCastStationEntity> azuraCastStations = await _dbContext.AzuraCastStations.ToListAsync();

        try
        {
            foreach (AzuraCastEntity entity in azuraCast)
            {
                entity.BaseUrl = Crypto.Decrypt(entity.BaseUrl);
                entity.BaseUrl = Crypto.Encrypt(entity.BaseUrl, newEncryptionKey);

                entity.AdminApiKey = Crypto.Decrypt(entity.AdminApiKey);
                entity.AdminApiKey = Crypto.Encrypt(entity.AdminApiKey, newEncryptionKey);
            }

            foreach (AzuraCastStationEntity entity in azuraCastStations.Where(static e => !string.IsNullOrEmpty(e.ApiKey)))
            {
                entity.ApiKey = Crypto.Decrypt(entity.ApiKey);
                entity.ApiKey = Crypto.Encrypt(entity.ApiKey, newEncryptionKey);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException or DbUpdateException)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException("An error occured while re-encrypting the database", ex);
        }

        Crypto.EncryptionKey = newEncryptionKey;
        _dbSettings.EncryptionKey = _dbSettings.NewEncryptionKey;
        _dbSettings.NewEncryptionKey = string.Empty;

        AppSettingsRecord appSettings = new()
        {
            AzzyBotSettings = _azzySettings,
            DatabaseSettings = _dbSettings,
            DiscordStatusSettings = _discordSettings,
            MusicStreamingSettings = _musicStreamingSettings,
            CoreUpdaterSettings = _updaterSettings
        };

        string json = JsonSerializer.Serialize(appSettings, JsonSerializationSourceGen.Default.AppSettingsRecord);
        await FileOperations.WriteToFileAsync(_azzySettings.SettingsFile, json);

        _logger.DatabaseReencryptionComplete();
    }
}
