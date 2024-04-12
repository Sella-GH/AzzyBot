using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace AzzyBot.Modules.Core;

/// <summary>
/// Contains miscellaneous utility methods for the core module.
/// </summary>
[SuppressMessage("Roslynator", "RCS0036:Remove blank line between single-line declarations of same kind.", Justification = "Readability")]
internal static class CoreMisc
{
    /// <summary>
    /// Checks if the current architecture is either ARM64 or X64.
    /// </summary>
    /// <returns>true if the current architecture is either ARM64 or X64; otherwise, false.</returns>
    internal static bool CheckCorrectArchitecture() => RuntimeInformation.OSArchitecture == Architecture.Arm64 || RuntimeInformation.OSArchitecture == Architecture.X64;

    /// <summary>
    /// Checks if the current operating system is Linux.
    /// </summary>
    /// <returns>true if the current operating system is Linux; otherwise, false.</returns>
    internal static bool CheckIfLinuxOs() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// Converts a DateTime object to Unix time.
    /// </summary>
    /// <param name="time">The DateTime object to convert.</param>
    /// <returns>The Unix time.</returns>
    internal static long ConvertToUnixTime(in DateTime time)
    {
        DateTime epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        TimeSpan elapsed = time.ToUniversalTime() - epoch;
        return (long)elapsed.TotalSeconds;
    }

    /// <summary>
    /// Convert unix time to a TimeSpan object.
    /// </summary>
    /// <param name="unixTime">The Unix time to convert.</param>
    /// <returns><seealso cref="TimeSpan"/>The converted TimeSpan object.</returns>
    internal static TimeSpan ConvertFromUnixTime(long unixTime)
    {
        DateTime offset = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime.ToLocalTime();
        return new(offset.Hour, offset.Minute, offset.Second);
    }

    /// <summary>
    /// Gets the operation system.
    /// </summary>
    /// <returns>The operating system.</returns>
    internal static string GetOperatingSystem => RuntimeInformation.OSDescription;

    /// <summary>
    /// Gets the operating system architecture.
    /// </summary>
    /// <returns>The operating system architecture.</returns>
    internal static string GetOperatingSystemArch => RuntimeInformation.OSArchitecture.ToString();
}
