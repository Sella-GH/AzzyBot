using AzzyBot.Bot.Utilities.Enums;
using DSharpPlus.Commands.ContextChecks;

namespace AzzyBot.Bot.Commands.Checks;

public sealed class FeatureAvailableCheckAttribute(AzuraCastFeatures feature) : ContextCheckAttribute
{
    public AzuraCastFeatures Feature { get; } = feature;
}
