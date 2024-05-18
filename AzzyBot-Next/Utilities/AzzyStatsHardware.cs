using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AzzyBot.Utilities.Records;

namespace AzzyBot.Utilities;

internal static class AzzyStatsHardware
{
    internal static bool CheckIfDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", EnvironmentVariableTarget.Process) == "true";
    internal static bool CheckIfLinuxOs => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    internal static bool CheckIfWindowsOs => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    internal static async Task<Dictionary<int, double>> GetSystemCpusAsync()
    {
        // Declare some variable stuff
        const int idleTime = 3;
        const double percentage = 100.0;
        const int delayInMs = 1000;

        #region Declare local methods

        static double CalculateCpuUsage(long prevIdle, long currIdle, long prevTotal, long currTotal) => (1.0 - ((double)(currIdle - prevIdle) / (currTotal - prevTotal))) * percentage;

        static long CalculateTimes(long[] times)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(times.Length, nameof(times));

            long sum = 0;
            foreach (long time in times)
            {
                sum += time;
            }

            return sum;
        }

        static long[] ConvertToLongArray(string line)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(line, nameof(line));

            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            long[] values = new long[parts.Length - 1];

            for (int i = 0; i < values.Length; i++)
            {
                if (!long.TryParse(parts[i], out values[i - 1]))
                    throw new InvalidOperationException("Could not convert string to long");
            }

            return values;
        }

        static async Task<Dictionary<int, long[]>> ReadCpuTimesAsync()
        {
            try
            {
                string[] statLines = await File.ReadAllLinesAsync(Path.Combine("/proc", "stat"));
                Dictionary<int, long[]> cpuTimes = new()
                {
                    // Index 0 is the aggregate of all cores
                    [0] = ConvertToLongArray(statLines[0])
                };

                for (int i = 1; i < statLines.Length; i++)
                {
                    // Starting from index 1, each line represents a core
                    if (statLines[i].StartsWith("cpu", StringComparison.OrdinalIgnoreCase))
                    {
                        cpuTimes.Add(i, ConvertToLongArray(statLines[i]));
                    }
                    else
                    {
                        break;
                    }
                }

                return cpuTimes;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Could not read cpu times", ex);
            }
        }

        #endregion Declare local methods

        Dictionary<int, long[]> prevCpuTimes = await ReadCpuTimesAsync();
        await Task.Delay(delayInMs);
        Dictionary<int, long[]> currCpuTimes = await ReadCpuTimesAsync();
        Dictionary<int, double> coreUsages = [];

        foreach (KeyValuePair<int, long[]> kvp in prevCpuTimes)
        {
            int coreIndex = kvp.Key;
            long[] prevCoreTimes = kvp.Value;
            long[] currCoreTimes = currCpuTimes[coreIndex];

            double coreUsage = CalculateCpuUsage(prevCoreTimes[idleTime], currCoreTimes[idleTime], CalculateTimes(prevCoreTimes), CalculateTimes(currCoreTimes));

            coreUsages.Add(coreIndex, coreUsage);
        }

        return coreUsages;
    }

    internal static async Task<CpuLoadRecord> GetSystemCpuLoadAsync()
    {
        try
        {
            string loadInfoLines = await File.ReadAllTextAsync(Path.Combine("/proc", "loadavg"));
            string[] loadInfoParts = loadInfoLines.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            double oneMin = double.Parse(loadInfoParts[0], CultureInfo.InvariantCulture);
            double fiveMin = double.Parse(loadInfoParts[1], CultureInfo.InvariantCulture);
            double fifteenMin = double.Parse(loadInfoParts[2], CultureInfo.InvariantCulture);

            return new(oneMin, fiveMin, fifteenMin);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not get cpu load", ex);
        }
    }

    internal static async Task<MemoryUsageRecord> GetSystemMemoryUsageAsync()
    {
        try
        {
            string[] memoryInfoLines = await File.ReadAllLinesAsync(Path.Combine("/proc", "meminfo"));
            long memTotalKb = 0;
            long memFreeKb = 0;

            static long ParseValue(string line)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(line, nameof(line));

                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (!long.TryParse(parts[1], out long value))
                    throw new InvalidOperationException("Could not parse value");

                return value;
            }

            foreach (string line in memoryInfoLines)
            {
                if (line.StartsWith("MemTotal:", StringComparison.OrdinalIgnoreCase))
                {
                    memTotalKb = ParseValue(line);
                }
                else if (line.StartsWith("MemFree:", StringComparison.OrdinalIgnoreCase))
                {
                    memFreeKb = ParseValue(line);
                }
                else if (memTotalKb > 0 && memFreeKb > 0)
                {
                    break;
                }
            }

            double memTotalGb = Math.Round(memTotalKb / (1024.0 * 1024.0), 2);
            double memFreeGb = Math.Round(memFreeKb / (1024.0 * 1024.0), 2);

            return new(memTotalGb, memFreeGb);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not get memory usage", ex);
        }
    }

    internal static async Task<Dictionary<string, NetworkSpeedRecord>> GetSystemNetworkUsageAsync()
    {
        const int delayInMs = 1000;
        const int bytesPerKbit = 125;

        static async Task<Dictionary<string, NetworkStatsRecord>> ReadNetworkStatsAsync()
        {
            try
            {
                string[] lines = await File.ReadAllLinesAsync(Path.Combine("/proc", "net", "dev"));
                Dictionary<string, NetworkStatsRecord> networkStats = [];

                for (int i = 2; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string[] parts = line.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 10)
                    {
                        string networkName = parts[0];
                        long rxBytes = long.Parse(parts[1], CultureInfo.InvariantCulture);
                        long txBytes = long.Parse(parts[9], CultureInfo.InvariantCulture);

                        networkStats[networkName] = new(rxBytes, txBytes);
                    }
                }

                return networkStats;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Could not read network stats", ex);
            }
        }

        Dictionary<string, NetworkStatsRecord> prevNetworkStats = await ReadNetworkStatsAsync();
        await Task.Delay(delayInMs);
        Dictionary<string, NetworkStatsRecord> currNetworkStats = await ReadNetworkStatsAsync();

        Dictionary<string, NetworkSpeedRecord> networkSpeeds = [];
        foreach (KeyValuePair<string, NetworkStatsRecord> kvp in prevNetworkStats)
        {
            string networkName = kvp.Key;
            double rxSpeedKbits = (currNetworkStats[networkName].Received - kvp.Value.Received) * 8.0 / bytesPerKbit / (delayInMs / 1000.0);
            double txSpeedKbits = (currNetworkStats[networkName].Transmitted - kvp.Value.Transmitted) * 8.0 / bytesPerKbit / (delayInMs / 1000.0);

            networkSpeeds[networkName] = new(rxSpeedKbits, txSpeedKbits);
        }

        return networkSpeeds;
    }

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
