using System;

using AzzyBot.Bot.Utilities.Enums;

using DSharpPlus.Commands.ContextChecks;

namespace AzzyBot.Bot.Commands.Checks;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class FeatureAvailableCheckAttribute(AzuraCastFeatures feature) : ContextCheckAttribute
{
    public AzuraCastFeatures Feature { get; } = feature;
}
