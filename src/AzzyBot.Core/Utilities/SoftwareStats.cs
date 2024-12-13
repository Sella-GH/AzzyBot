using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Hosting;

namespace AzzyBot.Core.Utilities;

public static class SoftwareStats
{
    public static string GetAppAuthors
        => FileVersionInfo.GetVersionInfo(GetAppFilePath()).CompanyName ?? "Bot authors not found";

    public static string GetAppDotNetVersion
        => Environment.Version.ToString() ?? ".NET version not found";

    public static string GetAppEnvironment
        => (GetAppName.EndsWith("Dev", StringComparison.OrdinalIgnoreCase)) ? Environments.Development : Environments.Production;

    public static string GetAppName
        => FileVersionInfo.GetVersionInfo(GetAppFilePath()).ProductName?.Split('.')[0] ?? "Bot name not found";

    public static string GetAppVersion
        => FileVersionInfo.GetVersionInfo(GetAppFilePath()).ProductVersion ?? "Bot version not found";

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

    private static string GetAppFilePath()
    {
        using Process app = Process.GetCurrentProcess();

        string fileName = app.MainModule!.FileName;

        return (!string.IsNullOrEmpty(fileName))
            ? Path.Combine(AppContext.BaseDirectory, fileName)
            : throw new InvalidOperationException("Bot file path not found");
    }
}
