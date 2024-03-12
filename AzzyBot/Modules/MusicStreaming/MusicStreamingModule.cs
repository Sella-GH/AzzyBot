using AzzyBot.Settings.MusicStreaming;
using DSharpPlus.SlashCommands;

namespace AzzyBot.Modules.MusicStreaming;

internal sealed class MusicStreamingModule : BaseModule
{
    internal override void RegisterCommands(SlashCommandsExtension slashCommandsExtension, ulong? serverId) => slashCommandsExtension.RegisterCommands<MusicStreamingCommands>(serverId);

    protected override void HandleModuleEvent(ModuleEvent evt)
    {
        switch (evt.Type)
        {
            case ModuleEventType.LavalinkPassword:
                evt.ResultString = MusicStreamingSettings.LavalinkPassword;
                break;
        }
    }

    internal override void Activate() => ModuleStates.ActivateMusicStreaming();
}
