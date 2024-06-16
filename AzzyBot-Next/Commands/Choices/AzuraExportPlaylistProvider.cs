using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;

namespace AzzyBot.Commands.Choices;

public sealed class AzuraExportPlaylistProvider : IChoiceProvider
{
    private readonly IReadOnlyDictionary<string, object> _exportProvider = new Dictionary<string, object>()
    {
        ["M3U"] = 0,
        ["PLS"] = 1
    };

    public ValueTask<IReadOnlyDictionary<string, object>> ProvideAsync(CommandParameter parameter) => ValueTask.FromResult(_exportProvider);
}
