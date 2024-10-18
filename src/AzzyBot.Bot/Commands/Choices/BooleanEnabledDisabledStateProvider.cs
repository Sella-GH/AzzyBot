using System.Collections.Generic;
using System.Threading.Tasks;
using AzzyBot.Bot.Localization;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;

namespace AzzyBot.Bot.Commands.Choices;

public sealed class BooleanEnableDisableStateProvider : IChoiceProvider
{
    private static readonly IEnumerable<DiscordApplicationCommandOptionChoice> BooleanStates =
    [
        new("Enable", 1, CommandChoiceLocalizer.GenerateTranslations(nameof(BooleanEnableDisableStateProvider), "Enable")),
        new("Disable", 2, CommandChoiceLocalizer.GenerateTranslations(nameof(BooleanEnableDisableStateProvider), "Disable")),
    ];

    public ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
        => ValueTask.FromResult(BooleanStates);
}
