using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AzzyBot.ExceptionHandling;
using AzzyBot.Modules.Core.Structs;
using AzzyBot.Settings;
using AzzyBot.Strings.Core;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Modules.Core;

/// <summary>
/// Contains methods for getting the CPU usage of the server.
/// </summary>
internal static class ServerCpuUsage
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
            ExceptionHandler.LogMessage(LogLevel.Error, "Directory not found: /proc");
            throw;
        }
        catch (FileNotFoundException)
        {
            ExceptionHandler.LogMessage(LogLevel.Error, "File not found: /proc/stat");
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            ExceptionHandler.LogMessage(LogLevel.Error, "Can't access file: /proc/stat - invalid permissions");
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
            ExceptionHandler.LogMessage(LogLevel.Error, "/proc not found");
            throw;
        }
        catch (FileNotFoundException)
        {
            ExceptionHandler.LogMessage(LogLevel.Error, "/proc/loadavg not found");
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            ExceptionHandler.LogMessage(LogLevel.Error, "Can't access file: /proc/loadavg - invalid permissions");
            throw;
        }
    }
}

/// <summary>
/// Contains methods for getting the memory usage of the server.
/// </summary>
internal static class ServerMemoryUsage
{
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
            ExceptionHandler.LogMessage(LogLevel.Error, "Directory not found: /proc");
            throw;
        }
        catch (FileNotFoundException)
        {
            ExceptionHandler.LogMessage(LogLevel.Error, "File not found: /proc/meminfo");
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            ExceptionHandler.LogMessage(LogLevel.Error, "Can't access file: /proc/meminfo - invalid permissions");
            throw;
        }
    }
}

/// <summary>
/// Contains methods for getting the memory usage only of the bot itself.
/// </summary>
internal static class BotMemoryUsage
{
    /// <summary>
    /// Gets the memory usage only of the bot itself.
    /// </summary>
    /// <returns>The memory usage of the bot in GB.</returns>
    internal static double GetMemoryUsage()
    {
        Process? process = Process.GetCurrentProcess();
        double usedMemoryGB = process.WorkingSet64 / (1024.0 * 1024.0 * 1024.0);

        process.Dispose();

        return Math.Round(usedMemoryGB, 2);
    }
}

/// <summary>
/// Contains methods for getting the disk usage of the server.
/// </summary>
internal static class ServerDiskUsage
{
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
}

/// <summary>
/// Contains methods for getting the network usage of the server.
/// </summary>
internal static class ServerNetworkUsage
{
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
            ExceptionHandler.LogMessage(LogLevel.Error, "/proc/net not found!");
            throw;
        }
        catch (FileNotFoundException)
        {
            ExceptionHandler.LogMessage(LogLevel.Error, "/proc/net/dev not found!");
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            ExceptionHandler.LogMessage(LogLevel.Error, "Sudo permissions required!");
            throw;
        }
    }
}

/// <summary>
/// Contains methods for getting information about the server.
/// </summary>
internal static class ServerInfo
{
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
            ExceptionHandler.LogMessage(LogLevel.Error, "/proc not found!");
            throw;
        }
        catch (FileNotFoundException)
        {
            ExceptionHandler.LogMessage(LogLevel.Error, "/proc/uptime not found!");
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            ExceptionHandler.LogMessage(LogLevel.Error, "Sudo permissions required!");
            throw;
        }
    }
}

/// <summary>
/// Contains methods for getting information about the bot.
/// </summary>
internal static class BotInfo
{
    private static DateTime StartTime = DateTime.MinValue;

    /// <summary>
    /// Sets the start time of the bot.
    /// </summary>
    internal static void SetStartTime()
    {
        if (StartTime == DateTime.MinValue)
            StartTime = DateTime.Now;
    }

    internal static string GetActivatedModules()
    {
        string text = "- Core";

        if (BaseSettings.ActivateAzuraCast)
            text += "\n- AzuraCast";

        if (BaseSettings.ActivateClubManagement)
            text += "\n- ClubManagement";

        return text;
    }

    internal static string GetBotName => Assembly.GetExecutingAssembly().GetName().Name ?? "Bot name not found";
    internal static string GetBotUptime => (CoreMisc.CheckIfLinuxOs()) ? $"<t:{CoreMisc.ConvertToUnixTime(StartTime).ToString(CultureInfo.InvariantCulture)}>" : "This feature is not available on Windows";
    internal static string GetBotEnvironment => (Program.GetDiscordClientId == 1169381408939192361) ? "Development" : "Production";
    internal static string GetBotVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "Azzy version not found";

    internal static async Task<string> GetBotCommitAsync()
    {
        if (CoreModule.CommitLock is null)
            return "Commit not found";

        string commit = await CoreModule.CommitLock.GetFileContentAsync();
        if (string.IsNullOrWhiteSpace(commit))
            commit = "Commit not found";

        return commit;
    }

    internal static async Task<string> GetBotCompileDateAsync()
    {
        if (CoreModule.BuildTimeLock is null)
            return "CompileDate not found";

        if (!DateTime.TryParse(await CoreModule.BuildTimeLock.GetFileContentAsync(), out DateTime dateTime))
            return "CompileDate not found";

        return $"<t:{CoreMisc.ConvertToUnixTime(dateTime)}>";
    }

    internal static string GetDotNetVersion => Environment.Version.ToString() ?? ".NET version not found";
    internal static string GetDSharpNetVersion => Program.GetDiscordClientVersion;
}
