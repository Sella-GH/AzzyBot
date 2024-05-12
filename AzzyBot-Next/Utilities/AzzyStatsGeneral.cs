using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using AzzyBot.Utilities.Records;
using Microsoft.Extensions.Hosting;

namespace AzzyBot.Utilities;

internal static class AzzyStatsGeneral
{
    internal static string GetBotAuthors => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).CompanyName ?? "Bot authors not found";
    internal static string GetBotDotNetVersion => Environment.Version.ToString() ?? ".NET version not found";
    internal static string GetBotEnvironment => (GetBotName.EndsWith("Dev", StringComparison.OrdinalIgnoreCase)) ? Environments.Development : Environments.Production;
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
        foreach (DriveInfo drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.Name == "/"))
        {
            double totalSize = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
            double totalFreeSpace = drive.TotalFreeSpace / (1024.0 * 1024.0 * 1024.0);
            double totalUsedSpace = totalSize - totalFreeSpace;

            return new(totalSize, totalFreeSpace, totalUsedSpace);
        }

        return new(0, 0, 0);
    }

    internal static bool CheckIfDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", EnvironmentVariableTarget.Process) == "true";
    internal static bool CheckIfLinuxOs => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    internal static bool CheckIfWindowsOs => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    internal static string GetOperatingSystem => RuntimeInformation.OSDescription;
    internal static string GetOsArchitecture => RuntimeInformation.OSArchitecture.ToString();

    internal static DateTime GetSystemUptime()
    {
        TimeSpan uptime = new(Environment.TickCount64);

        return DateTime.Now - uptime;
    }
}
