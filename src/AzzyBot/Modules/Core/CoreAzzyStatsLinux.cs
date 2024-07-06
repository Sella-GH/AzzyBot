using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Modules.Core.Structs;

namespace AzzyBot.Modules.Core;

/// <summary>
/// Contains methods for getting stats of the server on linux OS.
/// </summary>
internal static class CoreAzzyStatsLinux
{
    /// <summary>
    /// Gets the CPU usage of each core on the server.
    /// </summary>
    /// <returns>A dictionary where the key is the core index and the value is the CPU usage percentage.</returns>
    internal static async Task<Dictionary<int, double>> GetCpuUsageAsync()
    {
        const int idleTimeIndex = 3;
        const double percentageFactor = 100.0;
        const int delayInMs = 1000;

        static Task<long> CalculateTimes(long[] times)
        {
            ArgumentNullException.ThrowIfNull(times, nameof(times));

            long sum = 0;
            foreach (long time in times)
            {
                sum += time;
            }

            return Task.FromResult(sum);
        }

        static double CalculateUsage(long prevIdle, long currIdle, long prevTotal, long currTotal) => (1.0 - ((double)(currIdle - prevIdle) / (currTotal - prevTotal))) * percentageFactor;

        Dictionary<int, long[]> prevCpuTimes = await ReadCpuTimesAsync();
        await Task.Delay(delayInMs);
        Dictionary<int, long[]> currCpuTimes = await ReadCpuTimesAsync();
        Dictionary<int, double> coreUsages = [];

        foreach (KeyValuePair<int, long[]> kvp in prevCpuTimes)
        {
            int coreIndex = kvp.Key;
            long[] prevCoreTimes = kvp.Value;
            long[] currCoreTimes = currCpuTimes[coreIndex];

            double coreUsage = CalculateUsage(prevCoreTimes[idleTimeIndex], currCoreTimes[idleTimeIndex], await CalculateTimes(prevCoreTimes), await CalculateTimes(currCoreTimes));

            coreUsages.Add(coreIndex, Math.Round(coreUsage, 2));
        }

        return coreUsages;
    }

    /// <summary>
    /// Reads the CPU times from the /proc/stat file.
    /// </summary>
    /// <returns>A dictionary where the key is the core index and the value is an array of CPU times.</returns>
    /// <exception cref="InvalidCastException">Throws when there is an internal error which causes the int to long conversion to fail.</exception>
    private static async Task<Dictionary<int, long[]>> ReadCpuTimesAsync()
    {
        try
        {
            string[] cpuStatLines = await File.ReadAllLinesAsync("/proc/stat");
            Dictionary<int, long[]> cpuTimes = new()
            {
                // Index 0 is the aggregate of all cores
                [0] = ConvertToLongArray(cpuStatLines[0])
            };

            // Starting from index 1, each line represents a core
            for (int i = 1; i < cpuStatLines.Length; i++)
            {
                if (cpuStatLines[i].StartsWith("cpu", StringComparison.OrdinalIgnoreCase))
                {
                    cpuTimes.Add(i, ConvertToLongArray(cpuStatLines[i]));
                }
                else
                {
                    break; // Stop if the line does not start with "cpu"
                }
            }

            static long[] ConvertToLongArray(string line)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(line);

                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                long[] result = new long[parts.Length - 1];

                for (int i = 1; i < parts.Length; i++)
                {
                    if (!long.TryParse(parts[i], out result[i - 1]))
                        throw new InvalidCastException("Couldn't convert int to long");
                }

                return result;
            }

            return cpuTimes;
        }
        catch (DirectoryNotFoundException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "Directory not found: /proc", null);
            throw;
        }
        catch (FileNotFoundException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "File not found: /proc/stat", null);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "Can't access file: /proc/stat - invalid permissions", null);
            throw;
        }
    }

    /// <summary>
    /// Gets the avg CPU load of the server.
    /// </summary>
    /// <returns>A struct containing the one minute, five minute, and fifteen minute load averages.</returns>
    internal static async Task<CpuLoadStruct> GetCpuLoadAsync()
    {
        try
        {
            string loadInfoLines = await File.ReadAllTextAsync("/proc/loadavg");
            string[] parts = loadInfoLines.Split(" ");
            double oneMinute = double.Parse(parts[0], CultureInfo.InvariantCulture);
            double fiveMinute = double.Parse(parts[1], CultureInfo.InvariantCulture);
            double fifteenMinute = double.Parse(parts[2], CultureInfo.InvariantCulture);

            return new(oneMinute, fiveMinute, fifteenMinute);
        }
        catch (DirectoryNotFoundException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "/proc not found", null);
            throw;
        }
        catch (FileNotFoundException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "/proc/loadavg not found", null);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "Can't access file: /proc/loadavg - invalid permissions", null);
            throw;
        }
    }

    /// <summary>
    /// Gets the total and used memory of the server.
    /// </summary>
    /// <returns>A struct containing the total and used memory in GB.</returns>
    internal static async Task<MemoryUsageStruct> GetMemoryUsageAsync()
    {
        try
        {
            string[] memInfoLines = await File.ReadAllLinesAsync("/proc/meminfo");
            long memTotalKB = 0;
            long memFreeKB = 0;

            static long ParseValue(string line)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(line);

                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return long.Parse(parts[1], CultureInfo.InvariantCulture);
            }

            foreach (string line in memInfoLines)
            {
                if (line.StartsWith("MemTotal:", StringComparison.OrdinalIgnoreCase))
                {
                    memTotalKB = ParseValue(line);
                }
                else if (line.StartsWith("MemFree:", StringComparison.OrdinalIgnoreCase))
                {
                    memFreeKB = ParseValue(line);
                }
                else if (memTotalKB > 0 && memFreeKB > 0)
                {
                    break;
                }
            }

            // Convert to GB
            double memTotalGB = memTotalKB / (1024.0 * 1024.0);
            double memUsedGB = (memTotalKB - memFreeKB) / (1024.0 * 1024.0);

            return new(Math.Round(memTotalGB, 2), Math.Round(memUsedGB, 2));
        }
        catch (DirectoryNotFoundException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "Directory not found: /proc", null);
            throw;
        }
        catch (FileNotFoundException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "File not found: /proc/meminfo", null);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "Can't access file: /proc/meminfo - invalid permissions", null);
            throw;
        }
    }

    /// <summary>
    /// Gets the network usage of the server.
    /// </summary>
    /// <returns>A dictionary where the key is the network interface name and the value is a struct containing the received and transmitted speeds in Kbits/s.</returns>
    internal static async Task<Dictionary<string, NetworkSpeedStruct>> GetNetworkUsageAsync()
    {
        const int delayInMs = 1000;
        const int bytesPerKbit = 125;

        Dictionary<string, NetworkStatsStruct> prevNetworkStats = await ReadNetworkStatsAsync();
        await Task.Delay(delayInMs);
        Dictionary<string, NetworkStatsStruct> currNetworkStats = await ReadNetworkStatsAsync();

        Dictionary<string, NetworkSpeedStruct> networkSpeeds = [];

        foreach (KeyValuePair<string, NetworkStatsStruct> kvp in prevNetworkStats)
        {
            string networkName = kvp.Key;
            double receivedSpeedKbits = (currNetworkStats[networkName].Received - kvp.Value.Received) * 8.0 / bytesPerKbit / (delayInMs / 1000.0);
            double transmittedSpeedKbits = (currNetworkStats[networkName].Transmitted - kvp.Value.Transmitted) * 8.0 / bytesPerKbit / (delayInMs / 1000.0);

            networkSpeeds[networkName] = new(receivedSpeedKbits, transmittedSpeedKbits);
        }

        return networkSpeeds;
    }

    private static readonly char[] Separator = [' ', ':'];

    /// <summary>
    /// Reads the network stats from the /proc/net/dev file.
    /// </summary>
    /// <returns>A dictionary where the key is the network interface name and the value is a struct containing the received and transmitted bytes.</returns>
    private static async Task<Dictionary<string, NetworkStatsStruct>> ReadNetworkStatsAsync()
    {
        try
        {
            string[] lines = await File.ReadAllLinesAsync("/proc/net/dev");
            Dictionary<string, NetworkStatsStruct> networkStats = [];

            for (int i = 2; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] tokens = line.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length >= 10)
                {
                    string networkName = tokens[0];
                    long receivedBytes = long.Parse(tokens[1], CultureInfo.InvariantCulture);
                    long transmittedBytes = long.Parse(tokens[9], CultureInfo.InvariantCulture);
                    networkStats[networkName] = new(receivedBytes, transmittedBytes);
                }
            }

            return networkStats;
        }
        catch (DirectoryNotFoundException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "/proc/net not found!", null);
            throw;
        }
        catch (FileNotFoundException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "/proc/net/dev not found!", null);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "Sudo permissions required!", null);
            throw;
        }
    }

    /// <summary>
    /// Gets the system uptime of the server.
    /// </summary>
    /// <returns>The system uptime in seconds.</returns>
    internal static async Task<long> GetSystemUptimeAsync()
    {
        try
        {
            string line = await File.ReadAllTextAsync("/proc/uptime");
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            double uptimeSeconds = double.Parse(parts[0], CultureInfo.InvariantCulture);
            DateTime time = DateTime.Now;
            return CoreMisc.ConvertToUnixTime(time.AddSeconds(-uptimeSeconds));
        }
        catch (DirectoryNotFoundException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "/proc not found!", null);
            throw;
        }
        catch (FileNotFoundException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "/proc/uptime not found!", null);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "Sudo permissions required!", null);
            throw;
        }
    }
}
