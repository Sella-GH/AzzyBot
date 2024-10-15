using System.Collections.Generic;
using System.Threading.Tasks;
using AzzyBot.Bot.Localization;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;

namespace AzzyBot.Bot.Commands.Choices;

public sealed class AzuraExportPlaylistProvider : IChoiceProvider
{
    private static readonly IEnumerable<DiscordApplicationCommandOptionChoice> ExportProviders =
    [
        new("M3U", "m3u", CommandChoiceLocalizer.GenerateTranslations(nameof(AzuraExportPlaylistProvider), "m3u")),
        new("PLS", "pls", CommandChoiceLocalizer.GenerateTranslations(nameof(AzuraExportPlaylistProvider), "pls")),
    ];

    public ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
        => ValueTask.FromResult(ExportProviders);
}
