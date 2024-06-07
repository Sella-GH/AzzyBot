using System;
using System.Text;

namespace AzzyBot.Utilities;

public static class AzuraCastMisc
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
}
