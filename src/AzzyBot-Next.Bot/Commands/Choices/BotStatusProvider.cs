using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;

namespace AzzyBot.Bot.Commands.Choices;

public sealed class BotStatusProvider : IChoiceProvider
{
    private readonly IReadOnlyDictionary<string, object> _botStatus = new Dictionary<string, object>()
    {
        ["Offline"] = 0,
        ["Online"] = 1,
        ["Idle"] = 2,
        ["Do Not Disturb"] = 4
    };

    public ValueTask<IReadOnlyDictionary<string, object>> ProvideAsync(CommandParameter parameter) => ValueTask.FromResult(_botStatus);
}
