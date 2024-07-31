using System;
using System.IO;
using AzzyBot.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;

namespace AzzyBot.Core.Extensions;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        builder.AddConfiguration();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());
        LoggerProviderOptions.RegisterProviderOptions<FileLoggerConfiguration, FileLoggerProvider>(builder.Services);

        return builder;
    }

    public static ILoggingBuilder AddFile(this ILoggingBuilder builder, Action<FileLoggerConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));

        builder.AddFile();
        builder.Services.Configure(configure);

        return builder;
    }

    public static void AzzyBotLogging(this ILoggingBuilder logging, bool isDev = false, bool forceDebug = false)
    {
        if (!Directory.Exists("Logs"))
            Directory.CreateDirectory("Logs");

        logging.AddConsole();
        logging.AddFile(config => config.Directory = "Logs");
        logging.AddFilter("Microsoft.EntityFrameworkCore.ChangeTracking", LogLevel.Warning);
        logging.AddFilter("Microsoft.EntityFrameworkCore.Database", (isDev || forceDebug) ? LogLevel.Debug : LogLevel.Warning);
        logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Connection", LogLevel.Warning);
        logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", (isDev || forceDebug) ? LogLevel.Debug : LogLevel.Warning);
        logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Transaction", LogLevel.Warning);
        logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", (isDev || forceDebug) ? LogLevel.Debug : LogLevel.Warning);
        logging.AddFilter("Microsoft.EntityFrameworkCore.Migrations", (isDev || forceDebug) ? LogLevel.Debug : LogLevel.Information);
        logging.AddFilter("Microsoft.EntityFrameworkCore.Query", LogLevel.Warning);
        logging.AddFilter("Microsoft.Extensions.Hosting", LogLevel.Warning);
        logging.AddFilter("Microsoft.Extensions.Http.DefaultHttpClientFactory", LogLevel.Warning);
        logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
        logging.AddFilter("System.Net.Http.HttpClient.Default.ClientHandler", LogLevel.Warning);
        logging.AddFilter("System.Net.Http.HttpClient.Default.LogicalHandler", LogLevel.Warning);
        logging.AddSimpleConsole(config =>
        {
            config.ColorBehavior = LoggerColorBehavior.Enabled;
            config.IncludeScopes = true;
            config.SingleLine = true;
            config.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
        });
        logging.SetMinimumLevel((isDev || forceDebug) ? LogLevel.Debug : LogLevel.Information);
    }
}
