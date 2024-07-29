using AzzyBot.Bot.Utilities.Enums;
using DSharpPlus.Commands.ContextChecks;

namespace AzzyBot.Bot.Commands.Checks;

public sealed class ModuleActivatedCheckAttribute(AzzyModules module) : ContextCheckAttribute
{
    public AzzyModules Module { get; } = module;
}
