using System;
using AzzyBot.Bot.Utilities.Enums;
using DSharpPlus.Commands.ContextChecks;

namespace AzzyBot.Bot.Commands.Checks;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class AzuraCastDiscordPermCheckAttribute(AzuraCastDiscordPerm[] perms) : ContextCheckAttribute
{
    public AzuraCastDiscordPerm[] Perms { get; private init; } = perms;
}
