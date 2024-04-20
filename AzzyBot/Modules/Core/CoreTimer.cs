using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using AzzyBot.Logging;
using AzzyBot.Modules.Core.Settings;
using AzzyBot.Modules.Core.Updater;

namespace AzzyBot.Modules.Core;

internal sealed class CoreTimer : CoreModule
{
    private static Timer? AzzyBotGlobalTimer;
    private static DateTime LastUpdateCheck = DateTime.MinValue;

    internal static void StartGlobalTimer()
    {
        AzzyBotGlobalTimer = new(new TimerCallback(AzzyBotGlobalTimerTimeout), null, TimeSpan.Zero, TimeSpan.FromMinutes(15));
        LoggerBase.LogInfo(LoggerBase.GetLogger, "AzzyBotGlobalTimer started", null);
    }

    internal static void StopGlobalTimer()
    {
        if (AzzyBotGlobalTimer is null)
            return;

        AzzyBotGlobalTimer.Change(Timeout.Infinite, Timeout.Infinite);
        AzzyBotGlobalTimer.Dispose();
        AzzyBotGlobalTimer = null;
        LoggerBase.LogInfo(LoggerBase.GetLogger, "AzzyBotGlobalTimer stopped", null);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "General Exception is there to log unkown exceptions")]
    private static async void AzzyBotGlobalTimerTimeout(object? e)
    {
        try
        {
            LoggerBase.LogDebug(LoggerBase.GetLogger, "AzzyBotGlobalTimer tick", null);

            if (CoreAzzyStatsGeneral.GetBotEnvironment != "Development")
            {
                DateTime now = DateTime.Now;
                if (now - LastUpdateCheck >= TimeSpan.FromDays(CoreSettings.UpdaterCheckInterval))
                {
                    LoggerBase.LogDebug(LoggerBase.GetLogger, "AzzyBotGlobalTimer checking for AzzyBot Updates", null);
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
