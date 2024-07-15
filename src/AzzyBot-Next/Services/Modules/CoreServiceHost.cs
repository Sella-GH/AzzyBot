using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using AzzyBot.Logging;
using AzzyBot.Settings;
using AzzyBot.Utilities;
using AzzyBot.Utilities.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

public sealed class CoreServiceHost(IDbContextFactory<AzzyDbContext> dbContextFactory, ILogger<CoreServiceHost> logger, AzzyBotSettingsRecord settings) : IHostedService
{
    private readonly IDbContextFactory<AzzyDbContext> _dbContextFactory = dbContextFactory;
    private readonly ILogger<CoreServiceHost> _logger = logger;
    private readonly AzzyBotSettingsRecord _settings = settings;
    private readonly Task _completed = Task.CompletedTask;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string name = AzzyStatsSoftware.GetBotName;
        string version = AzzyStatsSoftware.GetBotVersion;
        string arch = AzzyStatsHardware.GetSystemOsArch;
        string os = AzzyStatsHardware.GetSystemOs;

        _logger.BotStarting(name, version, os, arch);

        LogfileCleaning();

        if (_settings.Database is not null && !string.IsNullOrWhiteSpace(_settings.Database.NewEncryptionKey) && (_settings.Database.NewEncryptionKey != _settings.Database.EncryptionKey))
            await ReencryptDatabaseAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.BotStopping();

        return _completed;
    }

    private void LogfileCleaning()
    {
        _logger.LogfileCleaning();

        DateTime date = DateTime.Today.AddDays(-_settings.LogRetentionDays);
        string logPath = Path.Combine(Environment.CurrentDirectory, "Logs");
        int counter = 0;
        foreach (string logFile in Directory.GetFiles(logPath, "*.log").Where(f => File.GetLastWriteTime(f) < date))
        {
            File.Delete(logFile);
            counter++;
        }

        _logger.LogfileDeleted(counter);
    }

    private async Task ReencryptDatabaseAsync()
    {
        ArgumentNullException.ThrowIfNull(_settings.Database);
        ArgumentException.ThrowIfNullOrWhiteSpace(_settings.Database.EncryptionKey, nameof(_settings.Database.EncryptionKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(_settings.Database.NewEncryptionKey, nameof(_settings.Database.NewEncryptionKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(_settings.SettingsFile, nameof(_settings.SettingsFile));

        _logger.DatabaseReencryptionStart();

        byte[] newEncryptionKey = Encoding.UTF8.GetBytes(_settings.Database.NewEncryptionKey);

        await using AzzyDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();

        List<AzuraCastEntity> azuraCast = await dbContext.AzuraCast.ToListAsync();
        List<AzuraCastStationEntity> azuraCastStations = await dbContext.AzuraCastStations.ToListAsync();
        List<AzuraCastStationMountEntity> AzuraCastStationMounts = await dbContext.AzuraCastStationMounts.ToListAsync();

        try
        {
            foreach (AzuraCastEntity entity in azuraCast)
            {
                entity.BaseUrl = Crypto.Decrypt(entity.BaseUrl);
                entity.BaseUrl = Crypto.Encrypt(entity.BaseUrl, newEncryptionKey);

                entity.AdminApiKey = Crypto.Decrypt(entity.AdminApiKey);
                entity.AdminApiKey = Crypto.Encrypt(entity.AdminApiKey, newEncryptionKey);
            }

            foreach (AzuraCastStationEntity entity in azuraCastStations)
            {
                entity.Name = Crypto.Decrypt(entity.Name);
                entity.Name = Crypto.Encrypt(entity.Name, newEncryptionKey);

                if (!string.IsNullOrWhiteSpace(entity.ApiKey))
                {
                    entity.ApiKey = Crypto.Decrypt(entity.ApiKey);
                    entity.ApiKey = Crypto.Encrypt(entity.ApiKey, newEncryptionKey);
                }
            }

            foreach (AzuraCastStationMountEntity entity in AzuraCastStationMounts)
            {
                entity.Mount = Crypto.Decrypt(entity.Mount);
                entity.Mount = Crypto.Encrypt(entity.Mount, newEncryptionKey);

                entity.Name = Crypto.Decrypt(entity.Name);
                entity.Name = Crypto.Encrypt(entity.Name, newEncryptionKey);
            }

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
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
