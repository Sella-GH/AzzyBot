using System.Threading.Tasks;
using AzzyBot.Commands.Autocompletes;
using AzzyBot.Commands.Choices;
using AzzyBot.Logging;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

internal sealed class AdminCommands
{
    //[Command("admin")]
    //[RequireApplicationOwner]
    //internal sealed class Admin(ILogger<Admin> logger)
    //{
    //    private readonly ILogger<Admin> _logger = logger;

    //    [Command("debug")]
    //    public async ValueTask AdminDebugAsync(CommandContext context, [SlashChoiceProvider<ViewAddRemove>] int command = 0, [SlashAutoCompleteProvider<GuildsAutocomplete>] ulong guildId = 0)
    //    {
    //        _logger.CommandRequested(nameof(AdminDebugAsync), context.User.GlobalName);
    //    }
    //}
}
