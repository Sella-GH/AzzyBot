using AzzyBot.Utilities.Enums;
using DSharpPlus.Commands.ContextChecks;

namespace AzzyBot.Commands.Checks;

public sealed class ModuleActivatedCheckAttribute(AzzyModules module) : ContextCheckAttribute
{
    public AzzyModules Module { get; } = module;
}
