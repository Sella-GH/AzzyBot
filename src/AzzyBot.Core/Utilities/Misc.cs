using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using AzzyBot.Core.Utilities.Enums;

namespace AzzyBot.Core.Utilities;

public static class Misc
{
    public static string GetProgressBar(double number, double elapsed, double duration)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number, nameof(number));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(elapsed, nameof(elapsed));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duration, nameof(duration));

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
    public static string GetReadableBool(bool value, ReadableBool type, bool lower = false)
    {
        string result = type switch
        {
            ReadableBool.EnabledDisabled => (value) ? "Enabled" : "Disabled",
            ReadableBool.StartedStopped => (value) ? "Started" : "Stopped",
            ReadableBool.YesNo => (value) ? "Yes" : "No",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        return (lower) ? result.ToLowerInvariant() : result;
    }
}
