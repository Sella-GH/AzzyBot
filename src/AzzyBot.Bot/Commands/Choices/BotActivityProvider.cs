using System.Collections.Generic;
using System.Threading.Tasks;
using AzzyBot.Bot.Localization;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;

namespace AzzyBot.Bot.Commands.Choices;

public sealed class BotActivityProvider : IChoiceProvider
{
    private static readonly IEnumerable<DiscordApplicationCommandOptionChoice> BotActivity =
    [
        new("Playing", 0, CommandChoiceLocalizer.GenerateTranslations(nameof(BotActivityProvider), "Playing")),
        new("Streaming", 1, CommandChoiceLocalizer.GenerateTranslations(nameof(BotActivityProvider), "Streaming")),
        new("Listening To", 2, CommandChoiceLocalizer.GenerateTranslations(nameof(BotActivityProvider), "Listening")),
        new("Watching", 3, CommandChoiceLocalizer.GenerateTranslations(nameof(BotActivityProvider), "Watching")),
        new("Competing", 5, CommandChoiceLocalizer.GenerateTranslations(nameof(BotActivityProvider), "Competing"))
    ];

    public ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
        => ValueTask.FromResult(BotActivity);
}
