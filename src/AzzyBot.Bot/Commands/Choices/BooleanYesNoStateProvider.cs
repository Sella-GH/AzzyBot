using System.Collections.Generic;
using System.Threading.Tasks;

using AzzyBot.Bot.Localization;

using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;

namespace AzzyBot.Bot.Commands.Choices;

public sealed class BooleanYesNoStateProvider : IChoiceProvider
{
    private static readonly IEnumerable<DiscordApplicationCommandOptionChoice> BooleanStates =
    [
        new("Yes", 1, CommandChoiceLocalizer.GenerateTranslations(nameof(BooleanYesNoStateProvider), "Yes")),
        new("No", 2, CommandChoiceLocalizer.GenerateTranslations(nameof(BooleanYesNoStateProvider), "No")),
    ];

    public ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
    => ValueTask.FromResult(BooleanStates);
}
