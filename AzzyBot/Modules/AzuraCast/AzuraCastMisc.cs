using System;

namespace AzzyBot.Modules.AzuraCast;

/// <summary>
/// Contains miscellaneous utility methods for AzuraCast.
/// </summary>
internal static class AzuraCastMisc
{
    /// <summary>
    /// Generates a progress bar string for a song.
    /// </summary>
    /// <param name="number">The total number of characters in the progress bar.</param>
    /// <param name="elapsed">The elapsed time of the song.</param>
    /// <param name="duration">The total duration of the song.</param>
    /// <returns>A string representing the progress bar.</returns>
    internal static string GetProgressBar(double number, double elapsed, double duration)
    {
        double pos = Math.Floor(number * (elapsed / duration));

        string result = string.Empty;

        for (int i = 0; i <= number; i++)
        {
            if (i == pos)
            {
                result += ":radio_button:";
            }
            else
            {
                result += "▬";
            }
        }

        return result;
    }
}
