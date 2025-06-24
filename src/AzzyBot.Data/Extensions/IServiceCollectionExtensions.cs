using System;
using System.Text;

using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Services;
using AzzyBot.Data.Settings;

using EntityFramework.Exceptions.PostgreSQL;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

namespace AzzyBot.Data.Extensions;

public static class IServiceCollectionExtensions
{
    public static void AzzyBotDataServices(this IServiceCollection services, DatabaseSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settings?.EncryptionKey);

        // Set the encryption key
        Crypto.EncryptionKey = Encoding.UTF8.GetBytes(settings.EncryptionKey);

        string connectionString = GetConnectionString(settings.Host, settings.Port, settings.User, settings.Password, settings.DatabaseName, settings.UseSsl, settings.SslMode, settings.SslNegotiation, settings.SslRootCert, settings.SslCert, settings.SslPassword);
#if DEBUG || DOCKER_DEBUG
        services.AddPooledDbContextFactory<AzzyDbContext>(o => o.UseNpgsql(connectionString).UseExceptionProcessor().EnableSensitiveDataLogging(true));
#else
        services.AddPooledDbContextFactory<AzzyDbContext>(o => o.UseNpgsql(connectionString).UseExceptionProcessor());
#endif
        services.AddSingleton<DbActions>();
        services.AddSingleton<DbMaintenance>();
    }

    private static string GetConnectionString(string host, int port, string user, string password, string database, bool useSsl, SslMode sslMode, SslNegotiation sslNegotiation, string? sslRootCert, string? sslClientCert, string? sslClientPassword)
    {
        NpgsqlConnectionStringBuilder builder = [];
        builder.Host = host;
        builder.Port = port;
        builder.Username = user;
        builder.Password = password;
        builder.Database = database;

        if (useSsl)
        {
            builder.SslMode = sslMode;
            builder.SslNegotiation = sslNegotiation;

            if (!string.IsNullOrWhiteSpace(sslRootCert))
                builder.RootCertificate = sslRootCert;

            if (!string.IsNullOrWhiteSpace(sslClientCert))
                builder.SslCertificate = sslClientCert;

            if (!string.IsNullOrWhiteSpace(sslClientPassword))
                builder.SslPassword = sslClientPassword;
        }

        return builder.ConnectionString;
    }
}
