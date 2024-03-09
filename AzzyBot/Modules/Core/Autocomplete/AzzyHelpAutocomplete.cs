using System.Collections.Generic;
using System.Threading.Tasks;
using AzzyBot.Modules.Core.Models;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace AzzyBot.Modules.Core.Autocomplete;

/// <summary>
/// Fills up the autocomplete for the Azzy help command.
/// </summary>
internal sealed class AzzyHelpAutocomplete : IAutocompleteProvider
{
    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
    {
        List<AzzyHelpModel> models = CoreAzzyHelp.GetCommandsAndDescriptions(ctx.Member);
        string? searchTerm = ctx.OptionValue.ToString();

        List<DiscordAutoCompleteChoice> choice = [];

        foreach (AzzyHelpModel model in models)
        {
            if (choice.Count == 25)
                break;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                if (model.Name.Contains(searchTerm, System.StringComparison.InvariantCultureIgnoreCase))
                    choice.Add(new DiscordAutoCompleteChoice($"/{model.Name}", model.Name));
            }
            else
            {
                choice.Add(new DiscordAutoCompleteChoice($"/{model.Name}", model.Name));
            }
        }

        return Task.FromResult<IEnumerable<DiscordAutoCompleteChoice>>(choice);
    }
}
