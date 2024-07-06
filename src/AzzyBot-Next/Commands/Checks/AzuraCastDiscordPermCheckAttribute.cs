using DSharpPlus.Commands.ContextChecks;

namespace AzzyBot;

public sealed class AzuraCastDiscordPermCheckAttribute(AzuraCastDiscordPerm[] perms) : ContextCheckAttribute
{
    public AzuraCastDiscordPerm[] Perms { get; private init; } = perms;
}
