using AzzyBot.Utilities.Enums;
using DSharpPlus.Commands.ContextChecks;

namespace AzzyBot.Commands.Checks;

public sealed class AzuraCastDiscordPermCheckAttribute(AzuraCastDiscordPerm[] perms) : ContextCheckAttribute
{
    public AzuraCastDiscordPerm[] Perms { get; private init; } = perms;
}
