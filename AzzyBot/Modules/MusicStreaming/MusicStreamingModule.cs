using DSharpPlus.SlashCommands;

namespace AzzyBot.Modules.MusicStreaming;

internal sealed class MusicStreamingModule : BaseModule
{
    internal override void RegisterCommands(SlashCommandsExtension slashCommandsExtension, ulong? serverId) => slashCommandsExtension.RegisterCommands<MusicStreamingCommands>(serverId);
    internal override void Activate() => ModuleStates.ActivateMusicStreaming();
}
