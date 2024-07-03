using System;
using DSharpPlus.Commands.ContextChecks;

namespace AzzyBot;

public class AzuraCastDiscordPermCheckAttribute : ContextCheckAttribute
{
    public AzuraCastDiscordPerm Perm { get; set; }
}
