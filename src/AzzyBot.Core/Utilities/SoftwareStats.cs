using System;
using System.Diagnostics;
using System.IO;

namespace AzzyBot.Core.Utilities;

public static class SoftwareStats
{
    private static readonly string AppFilePath = $"{Path.Combine(AppContext.BaseDirectory, AppDomain.CurrentDomain.FriendlyName)}.dll";

    public static string GetAppAuthors
        => FileVersionInfo.GetVersionInfo(AppFilePath).CompanyName ?? "Bot authors not found";

    public static string GetAppDotNetVersion
        => Environment.Version.ToString() ?? ".NET version not found";

    public static string GetAppName
        => FileVersionInfo.GetVersionInfo(AppFilePath).ProductName?.Split('.')[0] ?? "Bot name not found";

    public static string GetAppVersion
        => FileVersionInfo.GetVersionInfo(AppFilePath).ProductVersion ?? "Bot version not found";

    public static double GetAppMemoryUsage()
    {
        using Process app = Process.GetCurrentProcess();

        return Math.Round(app.WorkingSet64 / (1024.0 * 1024.0 * 1024.0), 2);
    }

    public static DateTimeOffset GetAppUptime()
    {
        using Process app = Process.GetCurrentProcess();

        return app.StartTime;
    }
}
