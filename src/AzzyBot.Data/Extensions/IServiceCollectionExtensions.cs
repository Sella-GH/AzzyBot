using System.Diagnostics.CodeAnalysis;
using System.Text;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AzzyBot.Data.Extensions;

public static class IServiceCollectionExtensions
{
    public static void AzzyBotDataServices(this IServiceCollection services, bool isDev, string encryptionKey, string? host, int? port, string? user, string? password, string? database)
    {
        // Set the encryption key
        Crypto.EncryptionKey = Encoding.UTF8.GetBytes(encryptionKey);

        string connectionString = GetConnectionString(isDev, host, port, user, password, database);
        services.AddDbContextPool<AzzyDbContext>(o => o.UseNpgsql(connectionString).UseExceptionProcessor().EnableSensitiveDataLogging(isDev));
        services.AddSingleton<DbActions>();
    }

    [SuppressMessage("Style", "IDE0045:Convert to conditional expression", Justification = "No nested conditional expressions")]
    private static string GetConnectionString(bool isDev, string? host, int? port, string? user, string? password, string? database)
    {
        NpgsqlConnectionStringBuilder builder = [];
        builder.Host = (string.IsNullOrWhiteSpace(host)) ? "AzzyBot-Db" : host;
        builder.Port = port ?? 5432;
        builder.Username = (string.IsNullOrWhiteSpace(user)) ? "azzybot" : user;
        builder.ConnectionIdleLifetime = 1200; // 20 minutes

        // No password because it can be null when using non-docker
        builder.Password = (string.IsNullOrWhiteSpace(password) && HardwareStats.CheckIfDocker) ? "thisIsAzzyB0!P@ssw0rd" : password;
        if (string.IsNullOrWhiteSpace(database))
        {
            builder.Database = (isDev) ? "azzybot-dev" : "azzybot";
        }
        else
        {
            builder.Database = database;
        }

        return builder.ConnectionString;
    }
}
