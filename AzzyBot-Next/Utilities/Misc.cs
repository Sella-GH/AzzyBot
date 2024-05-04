using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AzzyBot.Utilities;

[SuppressMessage("Roslynator", "RCS0036:Remove blank line between single-line declarations of same kind.", Justification = "Readability")]
internal sealed class Misc
{
    /// <summary>
    /// Get the application environment.
    /// </summary>
    /// <returns>The environment as <see langword="string"/>.</returns>
    internal static string GetAppEnvironment()
    {
        string name = GetAppName;

        if (name.EndsWith("Dev", StringComparison.Ordinal))
            return "Development";

        return "Production";
    }

    /// <summary>
    /// Get the name of the application.
    /// </summary>
    /// <returns>The name as <see langword="string"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the application has no name.</exception>
    internal static string GetAppName => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductName ?? throw new InvalidOperationException("App has no name!");

    /// <summary>
    /// Get the operation system.
    /// </summary>
    /// <returns>The operating system as <see langword="string"/>.</returns>
    internal static string GetOperatingSystem => RuntimeInformation.OSDescription;

    /// <summary>
    /// Get the operation system architecture.
    /// </summary>
    /// <returns>The operating system architecture as <see langword="string"/>.</returns>
    internal static string GetProcessorArchitecture => RuntimeInformation.ProcessArchitecture.ToString();
}
