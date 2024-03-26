using System;
using System.Threading.Tasks;
using AzzyBot.Modules.Core.Enums;
using AzzyBot.Settings.Core;
using AzzyBot.Updater;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace AzzyBot.Modules.Core;

internal sealed class CoreModule : BaseModule
{
    private static DateTime LastUpdateCheck = DateTime.MinValue;
    internal static CoreFileLock? BuildTimeLock;
    internal static CoreFileLock? CommitLock;

    internal override void RegisterCommands(SlashCommandsExtension slashCommandsExtension, ulong? serverId) => slashCommandsExtension.RegisterCommands<CoreCommands>(serverId);

    internal override void RegisterFileLocks()
    {
        string fileName;
        string[] directory;

        fileName = nameof(CoreFileNamesEnum.BuildDateTXT);
        directory = [nameof(CoreFileDirectoriesEnum.None)];
        BuildTimeLock = new(fileName, directory);

        fileName = nameof(CoreFileNamesEnum.CommitTXT);
        directory = [nameof(CoreFileDirectoriesEnum.None)];
        CommitLock = new(fileName, directory);
    }

    internal override void DisposeFileLocks()
    {
        BuildTimeLock?.Dispose();
        CommitLock?.Dispose();
    }

    protected override void HandleModuleEvent(ModuleEvent evt)
    {
        switch (evt.Type)
        {
            case ModuleEventType.GetAzzyBotName:
                evt.ResultString = CoreAzzyStatsGeneral.GetBotName;
                break;

            case ModuleEventType.GlobalTimerTick:
                if (CoreAzzyStatsGeneral.GetBotEnvironment == "Development")
                    break;

                DateTime now = DateTime.Now;
                if (now - LastUpdateCheck >= TimeSpan.FromDays(1) || (CoreSettings.UpdateCheckInterval == 2 && now - LastUpdateCheck >= TimeSpan.FromDays(7)))
                {
                    LastUpdateCheck = now;
                    Task.Run(() => Updates.CheckForUpdatesAsync(true));
                }

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
