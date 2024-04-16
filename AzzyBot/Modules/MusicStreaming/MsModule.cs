using System;
using AzzyBot.Modules.MusicStreaming.Settings;
using DSharpPlus.SlashCommands;

namespace AzzyBot.Modules.MusicStreaming;

internal sealed class MsModule : BaseModule
{
    internal override void RegisterCommands(SlashCommandsExtension slashCommandsExtension, ulong? serverId) => slashCommandsExtension.RegisterCommands<MsCommands>(serverId);

    protected override void HandleModuleEvent(ModuleEvent evt)
    {
        switch (evt.Type)
        {
            case ModuleEventType.GetMusicStreamingInactivity:
                evt.ResultBool = MsSettings.AutoDisconnect;
                break;

            case ModuleEventType.GetMusicStreamingInactivityTime:
                evt.ResultInt = MsSettings.AutoDisconnectTime;
                break;

            case ModuleEventType.GetMusicStreamingLyrics:
                evt.ResultBool = MsSettings.ActivateLyrics;
                break;
        }
    }

    internal override async void StartProcesses()
    {
        if (GetAzzyBotName() is "AzzyBot-Docker")
            return;

        if (!await MsLavalinkHandler.CheckIfJavaIsInstalledAsync())
            throw new InvalidOperationException("You have to install Java/OpenJDK Runtime 17 or 21 first!");

        if (!await MsLavalinkHandler.StartLavalinkAsync())
            throw new InvalidOperationException("Lavalink failed to start!");
    }

    internal override async void StopProcesses()
    {
        if (GetAzzyBotName() is "AzzyBot-Docker")
            return;

        await MsLavalinkHandler.StopLavalinkAsync();
    }

    internal override void Activate() => ModuleStates.ActivateMusicStreaming();

    internal static string GetAzzyBotName()
    {
        ModuleEvent evt = new(ModuleEventType.GetAzzyBotName);
        BroadcastModuleEvent(evt);
        return evt.ResultString;
    }
}
