using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;

namespace AzzyBot.Commands.Choices;
internal sealed class BotActivityProvider : IChoiceProvider
{
    private readonly IReadOnlyDictionary<string, object> _botActivity = new Dictionary<string, object>()
    {
        ["Playing"] = 0,
        ["Streaming"] = 1,
        ["Listening To"] = 2,
        ["Watching"] = 3,
        ["Competing"] = 5
    };

    public ValueTask<IReadOnlyDictionary<string, object>> ProvideAsync(CommandParameter parameter) => ValueTask.FromResult(_botActivity);
}
