using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Services;
using AzzyBot.Data.Settings;

using EntityFramework.Exceptions.PostgreSQL;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

namespace AzzyBot.Data.Extensions;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Known Issue.")]
[SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Known Issue.")]
public static class IServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AzzyBotDataServices(DatabaseSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            // Set the encryption key
            Crypto.SetEncryptionKey(Encoding.UTF8.GetBytes(settings.EncryptionKey));

            #region Local Methods

            static string GetConnectionString(string host, int port, string user, string password, string database, bool useSsl, SslMode sslMode, SslNegotiation sslNegotiation, string? sslRootCert, string? sslClientCert, string? sslClientPassword)
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
                    const string certificatesDir = "Certificates";

                    if (!string.IsNullOrWhiteSpace(sslRootCert))
                        builder.RootCertificate = Path.Combine(certificatesDir, sslRootCert);

                    if (!string.IsNullOrWhiteSpace(sslClientCert))
                        builder.SslCertificate = Path.Combine(certificatesDir, sslClientCert);

                    if (!string.IsNullOrWhiteSpace(sslClientPassword))
                        builder.SslPassword = Path.Combine(certificatesDir, sslClientPassword);
                }

                return builder.ConnectionString;
            }

            #endregion Local Methods

            string connectionString = GetConnectionString(settings.Host, settings.Port, settings.User, settings.Password, settings.DatabaseName, settings.UseSsl, settings.SslMode, settings.SslNegotiation, settings.SslRootCert, settings.SslCert, settings.SslPassword);
#if DEBUG || DOCKER_DEBUG
            services.AddPooledDbContextFactory<AzzyDbContext>(o => o.UseNpgsql(connectionString, o => o.EnableRetryOnFailure().SetPostgresVersion(settings.DatabaseVersion)).UseExceptionProcessor().EnableSensitiveDataLogging(true));
#else
            services.AddPooledDbContextFactory<AzzyDbContext>(o => o.UseNpgsql(connectionString, o => o.EnableRetryOnFailure().SetPostgresVersion(settings.DatabaseVersion)).UseExceptionProcessor());
#endif
            services.AddSingleton<DbActions>();
            services.AddSingleton<DbMaintenance>();
        }
    }
}
