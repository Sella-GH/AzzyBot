using System.Collections.Generic;
using System.Threading.Tasks;

using AzzyBot.Bot.Localization;

using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;

namespace AzzyBot.Bot.Commands.Choices;

public sealed class BotStatusProvider : IChoiceProvider
{
    private static readonly IEnumerable<DiscordApplicationCommandOptionChoice> BotStatusChoices =
    [
        new("Offline", 0, CommandChoiceLocalizer.GenerateTranslations(nameof(BotStatusProvider), "Offline")),
        new("Online", 1, CommandChoiceLocalizer.GenerateTranslations(nameof(BotStatusProvider), "Online")),
        new("Idle", 2, CommandChoiceLocalizer.GenerateTranslations(nameof(BotStatusProvider), "Idle")),
        new("Do Not Disturb", 4, CommandChoiceLocalizer.GenerateTranslations(nameof(BotStatusProvider), "Dnd")),
    ];

    public ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
        => ValueTask.FromResult(BotStatusChoices);
}
