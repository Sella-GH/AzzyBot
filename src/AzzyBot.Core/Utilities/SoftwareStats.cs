using System;
using System.Diagnostics;
using System.Reflection;

namespace AzzyBot.Core.Utilities;

public static class SoftwareStats
{
    private static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();

    public static string GetAppAuthors
        => ExecutingAssembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Bot authors not found";

    public static string GetAppDotNetVersion
        => Environment.Version.ToString() ?? ".NET version not found";

    public static string GetAppName
        => ExecutingAssembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product.Split('.')[0] ?? "Bot name not found";

    public static string GetAppVersion
        => ExecutingAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Bot version not found";

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
