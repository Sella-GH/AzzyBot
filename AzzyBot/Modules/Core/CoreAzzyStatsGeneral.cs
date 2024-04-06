using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AzzyBot.ExceptionHandling;
using AzzyBot.Modules.Core.Enums;
using AzzyBot.Strings.Core;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Modules.Core;

/// <summary>
/// Contains methods for getting general information of the bot.
/// </summary>
internal static class CoreAzzyStatsGeneral
{
    #region EmbedAzzyStats

    /// <summary>
    /// Gets the memory usage only of the bot itself.
    /// </summary>
    /// <returns>The memory usage of the bot in GB.</returns>
    internal static double GetBotMemoryUsage()
    {
        Process? process = Process.GetCurrentProcess();
        double usedMemoryGB = process.WorkingSet64 / (1024.0 * 1024.0 * 1024.0);

        process.Dispose();

        return Math.Round(usedMemoryGB, 2);
    }

    /// <summary>
    /// Gets the disk usage of the server.
    /// </summary>
    /// <returns>A string representing the disk usage in GB.</returns>
    internal static string GetDiskUsage()
    {
        try
        {
            string diskUsage = string.Empty;

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name == "/")
                {
                    double totalSizeGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    double freeSpaceGB = drive.TotalFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    double usedSpaceGB = totalSizeGB - freeSpaceGB;

                    return CoreStringBuilder.GetEmbedAzzyStatsDiskUsageDesc(Math.Round(usedSpaceGB, 2), Math.Round(totalSizeGB, 2));
                }
            }

            return diskUsage;
        }
        catch (DriveNotFoundException)
        {
            ExceptionHandler.LogMessage(LogLevel.Error, "Main drive not found");
            throw;
        }
    }

    internal static long GetSystemUptime()
    {
        TimeSpan uptime = new(Environment.TickCount64);
        DateTime dateTime = DateTime.Now.AddMinutes(-uptime.Minutes);

        return CoreMisc.ConvertToUnixTime(dateTime);
    }

    #endregion EmbedAzzyStats

    #region EmbedAzzyInfo

    internal static string GetActivatedModules()
    {
        string text = "- Core";

        if (ModuleStates.AzuraCast)
            text += "\n- AzuraCast";

        if (ModuleStates.ClubManagement)
            text += "\n- ClubManagement";

        if (ModuleStates.MusicStreaming)
            text += "\n- MusicStreaming";

        return text;
    }

    internal static string GetBotAuthors => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).CompanyName ?? "Bot authors not found";
    internal static string GetDotNetVersion => Environment.Version.ToString() ?? ".NET version not found";
    internal static string GetDSharpPlusVersion => AzzyBot.GetDiscordClientVersion;
    internal static string GetBotEnvironment => (AzzyBot.GetDiscordClientId == 1217214768159653978) ? "Development" : "Production";
    internal static string GetBotVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "Azzy version not found";
    internal static string GetBotName => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductName ?? "Bot name not found";

    internal static async Task<string> GetBotFileInfoAsync(CoreFileValuesEnum file)
    {
        if (CoreModule.AzzyBotLock is null)
            return "Info not found";

        if (file is CoreFileValuesEnum.CompileDate)
        {
            if (!DateTime.TryParse(await CoreModule.AzzyBotLock.GetFileContentAsync(file), out DateTime date))
                return "CompileDate not found";

            return $"<t:{CoreMisc.ConvertToUnixTime(date)}>";
        }

        string value = await CoreModule.AzzyBotLock.GetFileContentAsync(file);

        return (string.IsNullOrWhiteSpace(value)) ? "Info not found" : value;
    }

    internal static string GetBotUptime()
    {
        using Process azzy = Process.GetCurrentProcess();

        return $"<t:{CoreMisc.ConvertToUnixTime(azzy.StartTime)}>";
    }

    #endregion EmbedAzzyInfo
}
