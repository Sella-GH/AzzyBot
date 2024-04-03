using System;
using DSharpPlus.SlashCommands;

namespace AzzyBot.Modules.MusicStreaming;

internal sealed class MusicStreamingModule : BaseModule
{
    internal override void RegisterCommands(SlashCommandsExtension slashCommandsExtension, ulong? serverId) => slashCommandsExtension.RegisterCommands<MusicStreamingCommands>(serverId);

    protected override void HandleModuleEvent(ModuleEvent evt)
    {
        switch (evt.Type)
        {
            case ModuleEventType.GetMusicStreamingInactivity:
                evt.ResultBool = MusicStreamingSettings.AutoDisconnect;
                break;

            case ModuleEventType.GetMusicStreamingInactivityTime:
                evt.ResultInt = MusicStreamingSettings.AutoDisconnectTime;
                break;

            case ModuleEventType.GetMusicStreamingLyrics:
                evt.ResultBool = MusicStreamingSettings.ActivateLyrics;
                break;
        }
    }

    internal override async void StartProcesses()
    {
        if (GetAzzyBotName() is "AzzyBot-Docker")
            return;

        if (!await MusicStreamingLavalinkHandler.CheckIfJavaIsInstalledAsync())
            throw new InvalidOperationException("You have to install Java/OpenJDK Runtime 17 or 21 first!");

        if (!await MusicStreamingLavalinkHandler.StartLavalinkAsync())
            throw new InvalidOperationException("Lavalink failed to start!");
    }

    internal override async void StopProcesses()
    {
        if (GetAzzyBotName() is "AzzyBot-Docker")
            return;

        await MusicStreamingLavalinkHandler.StopLavalinkAsync();
    }

    internal override void Activate() => ModuleStates.ActivateMusicStreaming();

    internal static string GetAzzyBotName()
    {
        ModuleEvent evt = new(ModuleEventType.GetAzzyBotName);
        BroadcastModuleEvent(evt);
        return evt.ResultString;
    }
}
