using AzzyBot.ExceptionHandling;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Modules;

internal static class ModuleStates
{
    internal static bool AzuraCast { get; private set; }
    internal static bool ClubManagement { get; private set; }
    internal static bool Core { get; private set; }

    internal static void ActivateAzuraCast()
    {
        AzuraCast = true;
        ExceptionHandler.LogMessage(LogLevel.Information, "Module AzuraCast activated");
    }

    internal static void ActivateClubManagement()
    {
        ClubManagement = true;
        ExceptionHandler.LogMessage(LogLevel.Information, "Module ClubManagement activated");
    }

    internal static void ActivateCore()
    {
        Core = true;
        ExceptionHandler.LogMessage(LogLevel.Information, "Module Core activated");
    }
}
