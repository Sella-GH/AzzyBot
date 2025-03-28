﻿using System.Text;

using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Services;

using EntityFramework.Exceptions.PostgreSQL;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

namespace AzzyBot.Data.Extensions;

public static class IServiceCollectionExtensions
{
    public static void AzzyBotDataServices(this IServiceCollection services, string encryptionKey, string host, int port, string user, string password, string database)
    {
        // Set the encryption key
        Crypto.EncryptionKey = Encoding.UTF8.GetBytes(encryptionKey);

        string connectionString = GetConnectionString(host, port, user, password, database);
#if DEBUG || DOCKER_DEBUG
        services.AddPooledDbContextFactory<AzzyDbContext>(o => o.UseNpgsql(connectionString).UseExceptionProcessor().EnableSensitiveDataLogging(true));
#else
        services.AddPooledDbContextFactory<AzzyDbContext>(o => o.UseNpgsql(connectionString).UseExceptionProcessor());
#endif
        services.AddSingleton<DbActions>();
        services.AddSingleton<DbMaintenance>();
    }

    private static string GetConnectionString(string host, int port, string user, string password, string database)
    {
        NpgsqlConnectionStringBuilder builder = [];
        builder.Host = host;
        builder.Port = port;
        builder.Username = user;
        builder.Password = password;
        builder.Database = database;

        return builder.ConnectionString;
    }
}
