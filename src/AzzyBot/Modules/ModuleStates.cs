using AzzyBot.Logging;

namespace AzzyBot.Modules;

internal static class ModuleStates
{
    internal static bool AzuraCast { get; private set; }
    internal static bool ClubManagement { get; private set; }
    internal static bool Core { get; private set; }
    internal static bool MusicStreaming { get; private set; }

    internal static void ActivateAzuraCast()
    {
        AzuraCast = true;
        LoggerBase.LogInfo(LoggerBase.GetLogger, "Module AzuraCast activated", null);
    }

    internal static void ActivateClubManagement()
    {
        ClubManagement = true;
        LoggerBase.LogInfo(LoggerBase.GetLogger, "Module ClubManagement activated", null);
    }

    internal static void ActivateCore()
    {
        Core = true;
        LoggerBase.LogInfo(LoggerBase.GetLogger, "Module Core activated", null);
    }

    internal static void ActivateMusicStreaming()
    {
        MusicStreaming = true;
        LoggerBase.LogInfo(LoggerBase.GetLogger, "Module MusicStreaming activated", null);
    }
}
