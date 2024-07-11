using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using AzzyBot.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AzzyBot.Database;

public sealed class DatabaseService(ILogger<DatabaseService> logger, AzzyDbContext dbContext)
{
    private readonly ILogger<DatabaseService> _logger = logger;
    private readonly AzzyDbContext _dbContext = dbContext;

    public async Task BackupDatabaseAsync()
    {
        _logger.DatabaseStartBackup();

        if (Directory.Exists("Backups"))
            Directory.CreateDirectory("Backups");

        NpgsqlConnectionStringBuilder builder = new(_dbContext.Database.GetDbConnection().ConnectionString);
        string? host = builder.Host;
        string? userId = builder.Username;
        string? password = builder.Password;
        string? databaseName = builder.Database;
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("Database connection string is invalid");

        string backupName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{databaseName}.sql";
        string backupPath = Path.Combine("Backup", backupName);

        string args = $"-h {host} -U {userId} -F c -b -v -f \"{backupPath}\" {databaseName}";

        await ExecutePgDumpAsync(args, password);

        _logger.DatabaseBackupCompleted();
    }

    private async Task ExecutePgDumpAsync(string args, string password)
    {
        try
        {
            ProcessStartInfo processStartInfo = new("pg_dump", args)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Environment =
            {
                ["PGPASSWORD"] = password
            }
            };

            using Process process = new()
            {
                StartInfo = processStartInfo
            };

            process.Start();

            await process.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            _logger.DatabaseBackupFailed();
            throw new InvalidOperationException("Failed to backup database", ex);
        }
    }
}
