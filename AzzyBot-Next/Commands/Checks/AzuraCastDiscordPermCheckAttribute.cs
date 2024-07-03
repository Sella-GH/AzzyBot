using DSharpPlus.Commands.ContextChecks;

namespace AzzyBot;

public sealed class AzuraCastDiscordPermCheckAttribute : ContextCheckAttribute
{
    public AzuraCastDiscordPerm Perm { get; set; }
}
