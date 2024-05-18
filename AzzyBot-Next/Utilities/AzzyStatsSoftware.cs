using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace AzzyBot.Utilities;

internal static class AzzyStatsSoftware
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
}
