using System.Collections.Generic;
using System.Threading.Tasks;

using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;

namespace AzzyBot.Bot.Commands.Choices;

public sealed class AzuraExportPlaylistProvider : IChoiceProvider
{
    private static readonly IEnumerable<DiscordApplicationCommandOptionChoice> ExportProviders =
    [
        new("M3U", "m3u"),
        new("PLS", "pls")
    ];

    public ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
        => ValueTask.FromResult(ExportProviders);
}
