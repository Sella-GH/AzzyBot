using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;

namespace AzzyBot.Commands.Choices;

public sealed class BooleanEnableDisableStateProvider : IChoiceProvider
{
    private readonly IReadOnlyDictionary<string, object> _booleanStates = new Dictionary<string, object>()
    {
        ["Enable"] = 1,
        ["Disable"] = 2
    };

    public ValueTask<IReadOnlyDictionary<string, object>> ProvideAsync(CommandParameter parameter) => ValueTask.FromResult(_booleanStates);
}
