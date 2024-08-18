using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Settings;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.Modules;

public sealed class CoreServiceHost(ILogger<CoreServiceHost> logger, AzzyBotSettingsRecord settings, AzzyDbContext dbContext) : IHostedService
{
    private readonly ILogger<CoreServiceHost> _logger = logger;
    private readonly AzzyBotSettingsRecord _settings = settings;
    private readonly AzzyDbContext _dbContext = dbContext;
    private readonly Task _completed = Task.CompletedTask;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string name = SoftwareStats.GetAppName;
        string version = SoftwareStats.GetAppVersion;
        string arch = HardwareStats.GetSystemOsArch;
        string os = HardwareStats.GetSystemOs;

        _logger.BotStarting(name, version, os, arch);

        if (_settings.Database is not null && !string.IsNullOrWhiteSpace(_settings.Database.NewEncryptionKey) && (_settings.Database.NewEncryptionKey != _settings.Database.EncryptionKey))
            await ReencryptDatabaseAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.BotStopping();

        return _completed;
    }

    private async Task ReencryptDatabaseAsync()
    {
        ArgumentNullException.ThrowIfNull(_settings.Database);
        ArgumentException.ThrowIfNullOrWhiteSpace(_settings.Database.EncryptionKey, nameof(_settings.Database.EncryptionKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(_settings.Database.NewEncryptionKey, nameof(_settings.Database.NewEncryptionKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(_settings.SettingsFile, nameof(_settings.SettingsFile));

        _logger.DatabaseReencryptionStart();

        byte[] newEncryptionKey = Encoding.UTF8.GetBytes(_settings.Database.NewEncryptionKey);

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

            foreach (AzuraCastStationEntity entity in azuraCastStations.Where(static e => !string.IsNullOrWhiteSpace(e.ApiKey)))
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
        _settings.Database.EncryptionKey = _settings.Database.NewEncryptionKey;
        _settings.Database.NewEncryptionKey = string.Empty;

        await FileOperations.WriteToJsonFileAsync(_settings.SettingsFile, _settings);

        _logger.DatabaseReencryptionComplete();
    }
}
