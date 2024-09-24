using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Core.Utilities;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class AzzyViewLogsAutocomplete : IAutoCompleteProvider
{
    public ValueTask<IReadOnlyDictionary<string, object>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        Dictionary<string, object> results = new(25);
        string search = context.UserInput;

        foreach (string file in FileOperations.GetFilesInDirectory("Logs").OrderByDescending(static f => f))
        {
            if (results.Count is 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && !file.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            FileInfo fileInfo = new(file);
            results.Add($"{fileInfo.Name} ({Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2)} MB)", file);
        }

        return new ValueTask<IReadOnlyDictionary<string, object>>(results);
    }
}
