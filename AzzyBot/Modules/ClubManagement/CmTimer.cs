using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using AzzyBot.ExceptionHandling;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Modules.ClubManagement;

internal class CmTimer : CmModule
{
    private static Timer? ClubClosingTimer;

    internal static void StartClubClosingTimer()
    {
        ClubClosingTimer = new(new TimerCallback(ClubClosingTimerTimeout), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        ExceptionHandler.LogMessage(LogLevel.Information, "ClubClosingTimer started!");
    }

    internal static void StopClubClosingTimer()
    {
        if (ClubClosingTimer is null)
            return;

        ClubClosingTimer.Change(Timeout.Infinite, Timeout.Infinite);
        ClubClosingTimer.Dispose();
        ClubClosingTimer = null;
        ExceptionHandler.LogMessage(LogLevel.Information, "ClubClosingTimer stopped");
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "General Exception is there to log unkown exceptions")]
    private static async void ClubClosingTimerTimeout(object? o)
    {
        try
        {
            if (!IsMusicServerOnline())
                return;

            if (!await CheckIfClubIsOpenAsync())
            {
                if (ClubClosingTimer is null)
                    throw new InvalidOperationException("ClubClosingTimer is null");

                StopClubClosingTimer();
                SetClubClosingInitiated(false);
            }
        }
        catch (Exception ex)
        {
            // System.Threading.Timer just eats exceptions as far as I know so best to log them here.
            await ExceptionHandler.LogErrorAsync(ex);
        }
    }
}
