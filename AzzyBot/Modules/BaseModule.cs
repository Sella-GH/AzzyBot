using System;
using AzzyBot.Modules.AzuraCast;
using AzzyBot.Modules.ClubManagement;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.MusicStreaming;
using AzzyBot.Settings;
using AzzyBot.Settings.AzuraCast;
using AzzyBot.Settings.ClubManagement;
using AzzyBot.Settings.Core;
using AzzyBot.Settings.MusicStreaming;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace AzzyBot.Modules;

internal abstract class BaseModule
{
    protected enum ModuleEventType
    {
        CheckForSystemGeneratedlistId,
        CheckForDeniedPlaylistId,
        CheckForMusicServer,
        CheckIfNowPlayingSloganShouldChange,
        CheckIfPlaylistChangesAreAppropriate,
        CheckIfSongRequestsAreAppropriate,
        CheckIfUserHasStaffRole,
        GetAzuraCastApiUrl,
        GetClubOpeningTime,
        GetClubClosedTime,
        GetMusicStreamingInactivity,
        GetMusicStreamingInactivityTime,
        GlobalTimerTick,
        LavalinkPassword
    }

    protected sealed class ModuleEvent
    {
        internal ModuleEventType Type { get; }
        internal int ParameterInt { get; set; }
        internal bool ResultBool { get; set; }
        internal int ResultInt { get; set; }
        internal DiscordMember? ResultMember { get; set; }
        internal string ResultString { get; set; }
        internal TimeSpan ResultTimeSpan { get; set; }
        internal string ResultReason { get; set; }

        internal ModuleEvent(ModuleEventType type)
        {
            Type = type;
            ParameterInt = 0;
            ResultBool = false;
            ResultInt = 0;
            ResultMember = null;
            ResultString = string.Empty;
            ResultTimeSpan = TimeSpan.Zero;
            ResultReason = string.Empty;
        }
    }

    private BaseModule? Next;
    private static BaseModule? Head;

    internal static void RegisterAllModules()
    {
        // Always activate Core but check if the settings are loaded
        if (CoreSettings.CoreSettingsLoaded)
            RegisterModule(new CoreModule());

        if (!ModuleStates.Core)
            return;

        // Only activate them if Core is active and the settings are already loaded
        if (BaseSettings.ActivateAzuraCast && AzuraCastSettings.AzuraCastSettingsLoaded)
            RegisterModule(new AzuraCastModule());

        if (ModuleStates.AzuraCast && BaseSettings.ActivateClubManagement && ClubManagementSettings.ClubManagementSettingsLoaded)
            RegisterModule(new ClubManagementModule());

        if (ModuleStates.AzuraCast && BaseSettings.ActivateMusicStreaming && MusicStreamingSettings.MusicStreamingSettingsLoaded)
            RegisterModule(new MusicStreamingModule());
    }

    private static void ForEachModuleDo(Action<BaseModule> action)
    {
        BaseModule? module = Head;
        while (module is not null)
        {
            action(module);
            module = module.Next;
        }
    }

    private static void RegisterModule(BaseModule module)
    {
        module.Next = Head;
        Head = module;
        module.Activate();
    }

    internal static void RegisterAllCommands(SlashCommandsExtension slashCommandsExtension, ulong? serverId) => ForEachModuleDo(module => module.RegisterCommands(slashCommandsExtension, serverId));
    internal static void RegisterAllFileLocks() => ForEachModuleDo(module => module.RegisterFileLocks());
    internal static void DisposeAllFileLocks() => ForEachModuleDo(module => module.DisposeFileLocks());
    internal static void StartAllGlobalTimers() => ForEachModuleDo(module => module.StartGlobalTimers());
    internal static void StartAllProcesses() => ForEachModuleDo(module => module.StartProcesses());
    internal static void StopAllTimers() => ForEachModuleDo(module => module.StopTimers());
    internal static void StopAllProcesses() => ForEachModuleDo(module => module.StopProcesses());
    protected static void BroadcastModuleEvent(ModuleEvent evt) => ForEachModuleDo(module => module.HandleModuleEvent(evt));
    internal abstract void Activate();
    internal abstract void RegisterCommands(SlashCommandsExtension slashCommandsExtension, ulong? serverId);

    internal virtual void RegisterFileLocks()
    { }

    internal virtual void DisposeFileLocks()
    { }

    internal virtual void StartGlobalTimers()
    { }

    internal virtual void StartProcesses()
    { }

    internal virtual void StopTimers()
    { }

    internal virtual void StopProcesses()
    { }

    protected virtual void HandleModuleEvent(ModuleEvent evt)
    { }
}
