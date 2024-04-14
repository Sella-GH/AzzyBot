using AzzyBot.ExceptionHandling;
using AzzyBot.Modules.Core.Enums;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Modules.Core;

internal class CoreModule : BaseModule
{
    internal static CoreFileLock? AzzyBotLock;

    internal override void RegisterCommands(SlashCommandsExtension slashCommandsExtension, ulong? serverId) => slashCommandsExtension.RegisterCommands<CoreCommands>(serverId);

    internal override void RegisterFileLocks()
    {
        string fileName;
        string[] directory;

        fileName = nameof(CoreFileNamesEnum.AzzyBotJSON);
        directory = [nameof(CoreFileDirectoriesEnum.None)];
        AzzyBotLock = new(fileName, directory);

        ExceptionHandler.LogMessage(LogLevel.Debug, "Registered Core File Locks");
    }

    internal override void DisposeFileLocks() => AzzyBotLock?.Dispose();
    internal override void StartGlobalTimers() => CoreTimer.StartGlobalTimer();
    internal override void StopTimers() => CoreTimer.StopGlobalTimer();

    protected override void HandleModuleEvent(ModuleEvent evt)
    {
        switch (evt.Type)
        {
            case ModuleEventType.GetAzzyBotName:
                evt.ResultString = CoreAzzyStatsGeneral.GetBotName;
                break;
        }
    }

    internal override void Activate() => ModuleStates.ActivateCore();

    internal static bool CheckIfUserHasStaffRole(DiscordMember member)
    {
        ModuleEvent evt = new(ModuleEventType.CheckIfUserHasStaffRole)
        {
            ResultMember = member
        };
        BroadcastModuleEvent(evt);
        return evt.ResultBool;
    }

    internal static string GetAzuraCastApiUrl()
    {
        ModuleEvent evt = new(ModuleEventType.GetAzuraCastApiUrl);
        BroadcastModuleEvent(evt);
        return evt.ResultString;
    }

    internal static bool GetAzuracastIPv6Availability()
    {
        ModuleEvent evt = new(ModuleEventType.GetAzuraCastIPv6Availability);
        BroadcastModuleEvent(evt);
        return evt.ResultBool;
    }

    internal static bool GetMusicStreamingInactivity()
    {
        ModuleEvent evt = new(ModuleEventType.GetMusicStreamingInactivity);
        BroadcastModuleEvent(evt);
        return evt.ResultBool;
    }

    internal static int GetMusicStreamingInactivityTime()
    {
        ModuleEvent evt = new(ModuleEventType.GetMusicStreamingInactivityTime);
        BroadcastModuleEvent(evt);
        return evt.ResultInt;
    }

    internal static bool GetMusicStreamingLyrics()
    {
        ModuleEvent evt = new(ModuleEventType.GetMusicStreamingLyrics);
        BroadcastModuleEvent(evt);
        return evt.ResultBool;
    }
}
