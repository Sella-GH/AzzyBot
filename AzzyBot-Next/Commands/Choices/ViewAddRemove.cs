using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;

namespace AzzyBot.Commands.Choices;

internal sealed class ViewAddRemove : IChoiceProvider
{
    private readonly IReadOnlyDictionary<string, object> _botStatus = new Dictionary<string, object>()
    {
        ["View"] = 0,
        ["Add"] = 1,
        ["Remove"] = 2
    };

    public ValueTask<IReadOnlyDictionary<string, object>> ProvideAsync(CommandParameter parameter) => ValueTask.FromResult(_botStatus);
}
