﻿using System.Collections.Generic;
using System.Threading.Tasks;

using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;

namespace AzzyBot.Bot.Commands.Choices;

public sealed class BotActivityProvider : IChoiceProvider
{
    private static readonly IEnumerable<DiscordApplicationCommandOptionChoice> BotActivity =
    [
        new("Playing", 0),
        new("Streaming", 1),
        new("Listening To", 2),
        new("Watching", 3),
        new("Competing", 5)
    ];

    public ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
        => ValueTask.FromResult(BotActivity);
}
