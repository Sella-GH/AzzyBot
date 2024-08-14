using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AzzyBot.Core.Utilities.Records;

namespace AzzyBot.Core.Utilities;

public static class HardwareStats
{
    public static bool CheckIfDocker
        => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", EnvironmentVariableTarget.Process) is "true";

    public static bool CheckIfLinuxOs
        => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    public static bool CheckIfMacOs
        => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public static bool CheckIfWindowsOs
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static async Task<Dictionary<int, double>> GetSystemCpuAsync()
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

            for (int i = 1; i < values.Length; i++)
            {
                if (!long.TryParse(parts[i], out values[i - 1]))
                    throw new InvalidOperationException("Could not convert string to long");
            }

            return values;
        }

        static async Task<Dictionary<int, long[]>> ReadCpuTimesAsync()
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

        #endregion Declare local methods

        Dictionary<int, long[]> prevCpuTimes = await ReadCpuTimesAsync();
        await Task.Delay(delayInMs);
        Dictionary<int, long[]> currCpuTimes = await ReadCpuTimesAsync();
        Dictionary<int, double> coreUsages = new(prevCpuTimes.Count);

        foreach (KeyValuePair<int, long[]> kvp in prevCpuTimes)
        {
            int coreIndex = kvp.Key;
            long[] prevCoreTimes = kvp.Value;
            long[] currCoreTimes = currCpuTimes[coreIndex];

            double coreUsage = CalculateCpuUsage(prevCoreTimes[idleTime], currCoreTimes[idleTime], CalculateTimes(prevCoreTimes), CalculateTimes(currCoreTimes));

            coreUsages.Add(coreIndex, Math.Round(coreUsage, 2));
        }

        return coreUsages;
    }

    public static async Task<Dictionary<string, double>> GetSystemCpuTempAsync()
    {
        string typeFolderPath = Path.Combine("/sys", "class", "thermal");
        if (!Directory.Exists(typeFolderPath))
            return [];

        Dictionary<string, double> result = [];
        foreach (string folder in Directory.GetDirectories(typeFolderPath))
        {
            string typeFilePath = Path.Combine(folder, "type");
            if (!File.Exists(typeFilePath))
                continue;

            string content = await File.ReadAllTextAsync(typeFilePath);

            // This can be extendend when needed
            string type = content switch
            {
                string c when c.StartsWith("pch_", StringComparison.OrdinalIgnoreCase) => "Chipset",
                _ => "CPU",
            };

            string tempFilePath = Path.Combine(folder, "temp");
            if (!File.Exists(tempFilePath))
                continue;

            string tempInfo = await File.ReadAllTextAsync(tempFilePath);

            result.Add(type, Math.Round(double.Parse(tempInfo, CultureInfo.InvariantCulture) / 1000.0));
        }

        return result;
    }

    public static async Task<AppCpuLoadRecord> GetSystemCpuLoadAsync()
    {
        string loadInfoLines = await File.ReadAllTextAsync(Path.Combine("/proc", "loadavg"));
        string[] loadInfoParts = loadInfoLines.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        double oneMin = double.Parse(loadInfoParts[0], CultureInfo.InvariantCulture);
        double fiveMin = double.Parse(loadInfoParts[1], CultureInfo.InvariantCulture);
        double fifteenMin = double.Parse(loadInfoParts[2], CultureInfo.InvariantCulture);

        return new(oneMin, fiveMin, fifteenMin);
    }

    public static AppDiskUsageRecord GetSystemDiskUsage()
    {
        DriveInfo drive = DriveInfo.GetDrives().FirstOrDefault(static d => d.IsReady && d.Name == "/") ?? throw new InvalidOperationException("There is more than one root drive");
        double totalSize = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
        double totalFreeSpace = drive.TotalFreeSpace / (1024.0 * 1024.0 * 1024.0);
        double totalUsedSpace = totalSize - totalFreeSpace;

        return new(Math.Round(totalSize, 2), Math.Round(totalFreeSpace, 2), Math.Round(totalUsedSpace, 2));
    }

    public static async Task<AppMemoryUsageRecord> GetSystemMemoryUsageAsync()
    {
        string[] memoryInfoLines = await File.ReadAllLinesAsync(Path.Combine("/proc", "meminfo"));
        long memTotalKb = 0;
        long memFreeKb = 0;

        static long ParseValue(string line)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(line, nameof(line));

            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return (long.TryParse(parts[1], out long value)) ? value : throw new InvalidOperationException("Could not parse value");
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
        double memUsedGb = Math.Round((memTotalKb - memFreeKb) / (1024.0 * 1024.0), 2);

        return new(memTotalGb, memUsedGb);
    }

    public static async Task<Dictionary<string, AppNetworkSpeedRecord>> GetSystemNetworkUsageAsync()
    {
        const int delayInMs = 1000;
        const int bytesPerKbit = 125;

        static async Task<Dictionary<string, AppNetworkStatsRecord>> ReadNetworkStatsAsync()
        {
            string[] lines = await File.ReadAllLinesAsync(Path.Combine("/proc", "net", "dev"));
            Dictionary<string, AppNetworkStatsRecord> networkStats = [];

            for (int i = 2; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split([' ', ':'], StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 10)
                {
                    string networkName = parts[0];
                    long rxBytes = long.Parse(parts[1], CultureInfo.InvariantCulture);
                    long txBytes = long.Parse(parts[9], CultureInfo.InvariantCulture);

                    networkStats.Add(networkName, new(rxBytes, txBytes));
                }
            }

            return networkStats;
        }

        Dictionary<string, AppNetworkStatsRecord> prevNetworkStats = await ReadNetworkStatsAsync();
        await Task.Delay(delayInMs);
        Dictionary<string, AppNetworkStatsRecord> currNetworkStats = await ReadNetworkStatsAsync();

        Dictionary<string, AppNetworkSpeedRecord> networkSpeeds = new(prevNetworkStats.Count);
        foreach (KeyValuePair<string, AppNetworkStatsRecord> kvp in prevNetworkStats)
        {
            string networkName = kvp.Key;
            double rxSpeedKbits = (currNetworkStats[networkName].Received - kvp.Value.Received) * 8.0 / bytesPerKbit / (delayInMs / 1000.0);
            double txSpeedKbits = (currNetworkStats[networkName].Transmitted - kvp.Value.Transmitted) * 8.0 / bytesPerKbit / (delayInMs / 1000.0);

            networkSpeeds.Add(networkName, new(Math.Round(rxSpeedKbits, 2), Math.Round(txSpeedKbits, 2)));
        }

        return networkSpeeds;
    }

    public static string GetSystemOs
        => RuntimeInformation.OSDescription;

    public static string GetSystemOsArch
        => RuntimeInformation.OSArchitecture.ToString();

    public static DateTime GetSystemUptime
        => DateTime.Now.AddMilliseconds(-Environment.TickCount64);
}
