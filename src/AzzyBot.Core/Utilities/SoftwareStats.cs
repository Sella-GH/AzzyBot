using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace AzzyBot.Core.Utilities;

public static class SoftwareStats
{
    public static string GetAppAuthors
        => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).CompanyName ?? "Bot authors not found";

    public static string GetAppDotNetVersion
        => Environment.Version.ToString() ?? ".NET version not found";

    public static string GetAppEnvironment
        => (GetAppName.EndsWith("Dev", StringComparison.OrdinalIgnoreCase)) ? Environments.Development : Environments.Production;

    public static string GetAppName
        => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductName?.Split('.')[0] ?? "Bot name not found";

    public static string GetAppVersion
        => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion ?? "Bot version not found";

    public static double GetAppMemoryUsage()
    {
        using Process app = Process.GetCurrentProcess();

        return Math.Round(app.WorkingSet64 / (1024.0 * 1024.0 * 1024.0), 2);
    }

    public static DateTime GetAppUptime()
    {
        using Process app = Process.GetCurrentProcess();

        return app.StartTime;
    }
}
