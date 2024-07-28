using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace AzzyBot.Core.Utilities;

public static class AzzyStatsSoftware
{
    public static string GetBotAuthors
        => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).CompanyName ?? "Bot authors not found";

    public static string GetBotDotNetVersion
        => Environment.Version.ToString() ?? ".NET version not found";

    public static string GetBotEnvironment
        => (GetBotName.EndsWith("Dev", StringComparison.OrdinalIgnoreCase)) ? Environments.Development : Environments.Production;

    public static string GetBotName
        => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductName?.Split('.')[0] ?? "Bot name not found";

    public static string GetBotVersion
        => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion ?? "Bot version not found";

    public static double GetBotMemoryUsage()
    {
        using Process azzy = Process.GetCurrentProcess();

        return Math.Round(azzy.WorkingSet64 / (1024.0 * 1024.0 * 1024.0), 2);
    }

    public static DateTime GetBotUptime()
    {
        using Process azzy = Process.GetCurrentProcess();

        return azzy.StartTime;
    }
}
