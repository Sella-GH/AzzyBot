using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using AzzyBot.Core.Enums;

namespace AzzyBot.Core.Utilities;

public static class Misc
{
    public static bool CheckUpdateNotification(int notifyCounter, in DateTimeOffset lastNotificationTime)
    {
        TimeSpan elapsed = DateTimeOffset.UtcNow - lastNotificationTime;

        return notifyCounter switch
        {
            >= 7 when elapsed > TimeSpan.FromHours(5.98) => true,
            >= 3 when elapsed > TimeSpan.FromHours(11.98) => true,
            < 3 when elapsed > TimeSpan.FromHours(23.98) => true,
            _ => false
        };
    }

    public static string GetProgressBar(double number, double elapsed, double duration)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(elapsed);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duration);

        int pos = Convert.ToInt32(Math.Floor(number * (elapsed / duration)));

        StringBuilder bar = new();
        for (int i = 0; i <= number; i++)
        {
            if (i == pos)
            {
                bar.Append(":radio_button:");
            }
            else
            {
                bar.Append('▬');
            }
        }

        return bar.ToString();
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Make it more universal")]
    public static string GetReadableBool(bool value, ReadableBools type, bool lower = false)
    {
        string result = type switch
        {
            ReadableBools.EnabledDisabled => (value) ? "Enabled" : "Disabled",
            ReadableBools.StartedStopped => (value) ? "Started" : "Stopped",
            ReadableBools.YesNo => (value) ? "Yes" : "No",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        return (lower) ? result.ToLowerInvariant() : result;
    }

    public static Uri SanitizeUri(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        return new(uri.GetLeftPart(UriPartial.Authority).TrimEnd('/'));
    }
}
