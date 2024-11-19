using System;
using System.Globalization;
using System.IO;
using System.Linq;
using AzzyBot.Core.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using NReco.Logging.File;

namespace AzzyBot.Core.Extensions;

public static class ILoggingBuilderExtensions
{
    public static void AzzyBotLogging(this ILoggingBuilder logging, bool isDev = false, bool forceDebug = false, bool forceTrace = false)
    {
        if (!Directory.Exists("Logs"))
            Directory.CreateDirectory("Logs");

        foreach (string file in Directory.GetFiles("Logs").Where(static f => !f.StartsWith("AzzyBot_", StringComparison.InvariantCultureIgnoreCase)))
        {
            File.Delete(file);
        }

        logging.AddConsole();
        logging.AddFile(Path.Combine("Logs", $"AzzyBot_{DateTimeOffset.Now:yyyy-MM-dd}.log"), c =>
        {
            DateTimeOffset now = DateTimeOffset.Now;

            c.Append = true;
            c.FileSizeLimitBytes = FileSizes.DiscordFileSize;
            c.UseUtcTimestamp = false;
            c.FormatLogFileName = (logTime) => string.Format(CultureInfo.InvariantCulture, logTime, $"{now:yyyy-MM-dd}");
            c.FormatLogEntry = (message) =>
            {
                string logMessage = $"[{now:yyyy-MM-dd HH:mm:ss}] {message.LogLevel}: {message.LogName}[{message.EventId}] {message.Message}";
                if (message.Exception is not null)
                    logMessage += Environment.NewLine + message.Exception;

                return logMessage;
            };
        });
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
        logging.AddSimpleConsole(static config =>
        {
            config.ColorBehavior = LoggerColorBehavior.Enabled;
            config.IncludeScopes = true;
            config.SingleLine = true;
            config.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
        });
        logging.SetMinimumLevel((isDev || forceDebug) ? LogLevel.Debug : LogLevel.Information);
        if (isDev && forceTrace)
            logging.SetMinimumLevel(LogLevel.Trace);
    }
}
