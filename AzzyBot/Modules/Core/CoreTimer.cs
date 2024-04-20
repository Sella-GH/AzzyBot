using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using AzzyBot.ExceptionHandling;
using AzzyBot.Logging;
using AzzyBot.Modules.Core.Settings;
using AzzyBot.Modules.Core.Updater;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Modules.Core;

internal sealed class CoreTimer : CoreModule
{
    private static Timer? AzzyBotGlobalTimer;
    private static DateTime LastUpdateCheck = DateTime.MinValue;

    internal static void StartGlobalTimer()
    {
        AzzyBotGlobalTimer = new(new TimerCallback(AzzyBotGlobalTimerTimeout), null, TimeSpan.Zero, TimeSpan.FromMinutes(15));
        ExceptionHandler.LogMessage(LogLevel.Information, "AzzyBotGlobalTimer started");
    }

    internal static void StopGlobalTimer()
    {
        if (AzzyBotGlobalTimer is null)
            return;

        AzzyBotGlobalTimer.Change(Timeout.Infinite, Timeout.Infinite);
        AzzyBotGlobalTimer.Dispose();
        AzzyBotGlobalTimer = null;
        ExceptionHandler.LogMessage(LogLevel.Information, "AzzyBotGlobalTimer stopped");
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "General Exception is there to log unkown exceptions")]
    private static async void AzzyBotGlobalTimerTimeout(object? e)
    {
        try
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "AzzyBotGlobalTimer tick");

            if (CoreAzzyStatsGeneral.GetBotEnvironment != "Development")
            {
                DateTime now = DateTime.Now;
                if (now - LastUpdateCheck >= TimeSpan.FromDays(CoreSettings.UpdaterCheckInterval))
                {
                    ExceptionHandler.LogMessage(LogLevel.Debug, "AzzyBotGlobalTimer checking for AzzyBot Updates");
                    LastUpdateCheck = now;
                    await Updates.CheckForUpdatesAsync();
                }
            }

            BroadcastModuleEvent(new ModuleEvent(ModuleEventType.GlobalTimerTick));
        }
        catch (Exception ex)
        {
            // System.Threading.Timer just eats exceptions as far as I know so best to log them here.
            await LoggerExceptions.LogErrorAsync(ex);
        }
    }
}
