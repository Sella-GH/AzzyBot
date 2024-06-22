using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using AzzyBot.Utilities.Enums;

namespace AzzyBot.Utilities;

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
    public static string ReadableBool(bool value, ReadbleBool type, bool lower = false)
    {
        string result = type switch
        {
            ReadbleBool.EnabledDisabled => (value) ? "Enabled" : "Disabled",
            ReadbleBool.StartedStopped => (value) ? "Started" : "Stopped",
            ReadbleBool.YesNo => (value) ? "Yes" : "No",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        return (lower) ? result.ToLowerInvariant() : result;
    }
}
