using System;

using AzzyBot.Bot.Enums;

using DSharpPlus.Commands.ContextChecks;

namespace AzzyBot.Bot.Commands.Checks;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
#pragma warning disable MA0109 // Consider adding an overload with a Span<T> or Memory<T>
public sealed class AzuraCastDiscordPermCheckAttribute(AzuraCastDiscordPerm[] perms) : ContextCheckAttribute
#pragma warning restore MA0109 // Consider adding an overload with a Span<T> or Memory<T>
{
    public AzuraCastDiscordPerm[] Perms { get; private init; } = perms;
}
