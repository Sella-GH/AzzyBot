using System.Text;
using AzzyBot.Core.Utilities.Encryption;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AzzyBot.Data.Extensions;

public static class IServiceCollectionExtensions
{
    public static void AzzyBotDataServices(this IServiceCollection services, bool isDev, string encryptionKey, string host, int port, string user, string password, string database)
    {
        // Set the encryption key
        Crypto.EncryptionKey = Encoding.UTF8.GetBytes(encryptionKey);

        string connectionString = GetConnectionString(host, port, user, password, database);
        services.AddDbContext<AzzyDbContext>(o => o.UseNpgsql(connectionString).UseExceptionProcessor().EnableSensitiveDataLogging(isDev), ServiceLifetime.Transient);
        services.AddSingleton<DbActions>();
        services.AddSingleton<DbMaintenance>();
    }

    private static string GetConnectionString(string host, int port, string user, string password, string database)
    {
        NpgsqlConnectionStringBuilder builder = [];
        builder.Host = host;
        builder.Port = port;
        builder.Username = user;
        builder.ConnectionIdleLifetime = 1200; // 20 minutes
        builder.Password = password;
        builder.Database = database;

        return builder.ConnectionString;
    }
}
