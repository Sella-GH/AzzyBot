using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using NReco.Logging.File;

namespace AzzyBot.Core.Extensions;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Known Issue.")]
[SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Known Issue.")]
public static class ILoggingBuilderExtensions
{
    extension(ILoggingBuilder logging)
    {
        public void AzzyBotLogging(LogLevel logLevel)
        {
            if (!Directory.Exists("Logs"))
                Directory.CreateDirectory("Logs");

            foreach (string file in Directory.EnumerateFiles("Logs").Where(static f => !f.Contains("AzzyBot_", StringComparison.OrdinalIgnoreCase)))
            {
                File.Delete(file);
            }

            logging.AddConsole();
            logging.AddFile(Path.Combine("Logs", "AzzyBot_{0:yyyy-MM-dd}.log"), c =>
            {
                string logPath = Path.Combine("Logs", "AzzyBot_{0:yyyy-MM-dd}.log");

                c.UseUtcTimestamp = false;
                c.FormatLogFileName = _ => string.Format(CultureInfo.InvariantCulture, logPath, DateTimeOffset.Now);
                c.FormatLogEntry = (message) =>
                {
                    string logMessage = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] {message.LogLevel}: {message.LogName}[{message.EventId.Id}] {message.Message}";
                    if (message.Exception is not null)
                        logMessage += Environment.NewLine + message.Exception;

                    return logMessage;
                };
            });
            logging.AddFilter("Microsoft.EntityFrameworkCore.ChangeTracking", LogLevel.Warning);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Database", (logLevel is LogLevel.Debug) ? LogLevel.Debug : LogLevel.Warning);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Connection", LogLevel.Warning);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", (logLevel is LogLevel.Debug) ? LogLevel.Debug : LogLevel.Warning);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Transaction", LogLevel.Warning);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", (logLevel is LogLevel.Debug) ? LogLevel.Debug : LogLevel.Warning);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Migrations", (logLevel is LogLevel.Debug) ? LogLevel.Debug : LogLevel.Information);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Query", LogLevel.Warning);
            logging.AddFilter("Microsoft.Extensions.Hosting", LogLevel.Warning);
            logging.AddFilter("Microsoft.Extensions.Http.DefaultHttpClientFactory", LogLevel.Warning);
            logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
            logging.AddFilter("System.Net.Http.HttpClient.Default.ClientHandler", LogLevel.Warning);
            logging.AddFilter("System.Net.Http.HttpClient.Default.LogicalHandler", LogLevel.Warning);
            logging.SetMinimumLevel(logLevel);
            logging.AddSimpleConsole(static config =>
            {
                config.ColorBehavior = LoggerColorBehavior.Enabled;
                config.IncludeScopes = true;
                config.SingleLine = true;
                config.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
            });
        }
    }
}
