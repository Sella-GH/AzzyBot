using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using AzzyBot.Utilities.Records;

namespace AzzyBot.Utilities;

internal static class AzzyStatsGeneral
{
    internal static string GetBotAuthors => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).CompanyName ?? "Bot authors not found";
    internal static string GetBotDotNetVersion => Environment.Version.ToString() ?? ".NET version not found";
    internal static string GetBotEnvironment => (GetBotName.EndsWith("Dev", StringComparison.Ordinal)) ? "Development" : "Production";
    internal static string GetBotName => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductName ?? "Bot name not found";
    internal static string GetBotVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "Bot version not found";

    internal static double GetBotMemoryUsage()
    {
        using Process? process = Process.GetCurrentProcess();

        return process.WorkingSet64 / (1024.0 * 1024.0 * 1024.0);
    }

    internal static DateTime GetBotUptime()
    {
        using Process azzy = Process.GetCurrentProcess();

        return azzy.StartTime;
    }

    internal static DiskUsageRecord GetDiskUsage()
    {
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady && drive.Name == "/")
            {
                double totalSize = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                double totalFreeSpace = drive.TotalFreeSpace / (1024.0 * 1024.0 * 1024.0);
                double totalUsedSpace = totalSize - totalFreeSpace;

                return new(totalSize, totalFreeSpace, totalUsedSpace);
            }
        }

        return new(0, 0, 0);
    }

    internal static DateTime GetSystemUptime()
    {
        TimeSpan uptime = new(Environment.TickCount64);

        return DateTime.Now - uptime;
    }
}
