using System;
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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Services.Modules;

public sealed class CoreServiceHost(ILogger<CoreServiceHost> logger, IOptions<AzzyBotSettings> azzySettings, IOptions<DatabaseSettings> dbSettings, IOptions<DiscordStatusSettings> discordSettings, IOptions<MusicStreamingSettings> musicStreamingSettings, IOptions<CoreUpdaterSettings> updaterSettings, IDbContextFactory<AzzyDbContext> dbContextFactory) : IHostedService
{
    private readonly ILogger<CoreServiceHost> _logger = logger;
    private readonly AzzyBotSettings _azzySettings = azzySettings.Value;
    private readonly CoreUpdaterSettings _updaterSettings = updaterSettings.Value;
    private readonly DatabaseSettings _dbSettings = dbSettings.Value;
    private readonly DiscordStatusSettings _discordSettings = discordSettings.Value;
    private readonly MusicStreamingSettings _musicStreamingSettings = musicStreamingSettings.Value;
    private readonly IDbContextFactory<AzzyDbContext> _dbContextFactory = dbContextFactory;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string name = SoftwareStats.GetAppName;
        string version = SoftwareStats.GetAppVersion;
        string arch = HardwareStats.GetSystemOsArch;
        string os = HardwareStats.GetSystemOs;
        string dotnet = SoftwareStats.GetAppDotNetVersion;

        _logger.BotStarting(name, version, os, arch, dotnet);

        await EnsureAzzyBotDbTableIsCreatedAsync();
        if (!string.IsNullOrWhiteSpace(_dbSettings.NewEncryptionKey) && (_dbSettings.NewEncryptionKey != _dbSettings.EncryptionKey))
            await ReencryptDatabaseAsync();

        await MigrateDatabaseEncryptionSchemaAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.BotStopping();

        return Task.CompletedTask;
    }

    // TODO: Remove this method in a future release after enough time has passed since the encryption schema change.
    private async Task MigrateDatabaseEncryptionSchemaAsync()
    {
        _logger.DatabaseNewEncryptionStart();

        await using AzzyDbContext dbContext = _dbContextFactory.CreateDbContext();

        List<AzuraCastEntity> azuraCast = await dbContext.AzuraCast.ToListAsync();
        List<AzuraCastStationEntity> azuraCastStations = await dbContext.AzuraCastStations.Where(static e => !string.IsNullOrEmpty(e.ApiKey)).ToListAsync();

        try
        {
            foreach (AzuraCastEntity entity in azuraCast)
            {
                if (!string.IsNullOrEmpty(entity.BaseUrl) && !Crypto.CheckIfNewCipherIsUsed(entity.BaseUrl))
                    entity.BaseUrl = Crypto.MigrateOldCipherToNew(entity.BaseUrl);

                if (!string.IsNullOrEmpty(entity.AdminApiKey) && !Crypto.CheckIfNewCipherIsUsed(entity.AdminApiKey))
                    entity.AdminApiKey = Crypto.MigrateOldCipherToNew(entity.AdminApiKey);
            }

            foreach (AzuraCastStationEntity entity in azuraCastStations.Where(static e => !Crypto.CheckIfNewCipherIsUsed(e.ApiKey)))
            {
                entity.ApiKey = Crypto.MigrateOldCipherToNew(entity.ApiKey);
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException or DbUpdateException)
        {
            throw new InvalidOperationException("An error occurred while migrating the encryption schema of the database", ex);
        }

        _logger.DatabaseNewEncryptionComplete();
    }

    private async Task EnsureAzzyBotDbTableIsCreatedAsync()
    {
        await using AzzyDbContext dbContext = _dbContextFactory.CreateDbContext();

        if (await dbContext.AzzyBot.AnyAsync())
            return;

        try
        {
            await dbContext.AzzyBot.AddAsync(new AzzyBotEntity());

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException or DbUpdateException)
        {
            throw new InvalidOperationException("An error occurred while creating the AzzyBot table", ex);
        }
    }

    private async Task ReencryptDatabaseAsync()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(_dbSettings.EncryptionKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(_dbSettings.NewEncryptionKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(_azzySettings.SettingsFile);

        _logger.DatabaseReencryptionStart();

        byte[] newEncryptionKey = Encoding.UTF8.GetBytes(_dbSettings.NewEncryptionKey);

        await using AzzyDbContext dbContext = _dbContextFactory.CreateDbContext();

        List<AzuraCastEntity> azuraCast = await dbContext.AzuraCast.ToListAsync();
        List<AzuraCastStationEntity> azuraCastStations = await dbContext.AzuraCastStations.Where(static e => !string.IsNullOrEmpty(e.ApiKey)).ToListAsync();

        try
        {
            foreach (AzuraCastEntity entity in azuraCast)
            {
                if (!string.IsNullOrEmpty(entity.BaseUrl))
                {
                    entity.BaseUrl = Crypto.Decrypt(entity.BaseUrl);
                    entity.BaseUrl = Crypto.Encrypt(entity.BaseUrl, newEncryptionKey);
                }

                if (!string.IsNullOrEmpty(entity.AdminApiKey))
                {
                    entity.AdminApiKey = Crypto.Decrypt(entity.AdminApiKey);
                    entity.AdminApiKey = Crypto.Encrypt(entity.AdminApiKey, newEncryptionKey);
                }
            }

            foreach (AzuraCastStationEntity entity in azuraCastStations)
            {
                entity.ApiKey = Crypto.Decrypt(entity.ApiKey);
                entity.ApiKey = Crypto.Encrypt(entity.ApiKey, newEncryptionKey);
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException or DbUpdateException)
        {
            throw new InvalidOperationException("An error occurred while re-encrypting the database", ex);
        }

        Crypto.SetEncryptionKey(newEncryptionKey);
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

        string json = JsonSerializer.Serialize(appSettings, JsonSourceGen.Default.AppSettingsRecord);
        await FileOperations.WriteToFileAsync(_azzySettings.SettingsFile, json);

        _logger.DatabaseReencryptionComplete();
    }
}
