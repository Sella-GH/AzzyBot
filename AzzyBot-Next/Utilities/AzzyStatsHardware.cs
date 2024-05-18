using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using AzzyBot.Utilities.Records;

namespace AzzyBot.Utilities;

internal static class AzzyStatsHardware
{
    internal static bool CheckIfDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", EnvironmentVariableTarget.Process) == "true";
    internal static bool CheckIfLinuxOs => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    internal static bool CheckIfWindowsOs => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    internal static DiskUsageRecord GetSystemDiskUsage()
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

    internal static string GetSystemOs => RuntimeInformation.OSDescription;
    internal static string GetSystemOsArch => RuntimeInformation.OSArchitecture.ToString();

    internal static DateTime GetSystemUptime()
    {
        TimeSpan uptime = new(Environment.TickCount64);

        return DateTime.Now - uptime;
    }
}
