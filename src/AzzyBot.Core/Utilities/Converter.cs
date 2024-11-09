using System;

namespace AzzyBot.Core.Utilities;

public static class Converter
{
    /// <summary>
    /// Convert unix time to a <seealso cref="TimeSpan"/> object.
    /// </summary>
    /// <param name="unixTime">The Unix time to convert.</param>
    /// <returns>The converted time as <seealso cref="TimeSpan"/>.</returns>
    public static TimeSpan ConvertFromUnixTime(long unixTime)
    {
        DateTimeOffset offset = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime.ToLocalTime();

        return new(offset.Hour, offset.Minute, offset.Second);
    }

    /// <summary>
    /// Converts a <seealso cref="DateTimeOffset"/> object to Unix time.
    /// </summary>
    /// <param name="time">The <seealso cref="DateTimeOffset"/> object to convert.</param>
    /// <returns>The Unix time as <see langword="long"/>.</returns>
    public static long ConvertToUnixTime(in DateTimeOffset time)
    {
        DateTimeOffset epoch = DateTimeOffset.UnixEpoch;
        TimeSpan elapsed = time.ToUniversalTime() - epoch;

        return (long)elapsed.TotalSeconds;
    }
}
