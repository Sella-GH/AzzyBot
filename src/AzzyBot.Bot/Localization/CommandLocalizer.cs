using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using System.Threading.Tasks;
using DSharpPlus.Commands.Processors.SlashCommands.Localization;

namespace AzzyBot.Bot.Localization;

public sealed class CommandLocalizer : IInteractionLocalizer
{
    public ValueTask<IReadOnlyDictionary<DiscordLocale, string>> TranslateAsync(string fullSymbolName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullSymbolName, nameof(fullSymbolName));

        bool topLevelCommand = false;
        string symbolName = string.Empty;
        string[] parts = fullSymbolName.Split('.');
        if (parts.Length is 2)
        {
            symbolName = parts[0];
            topLevelCommand = true;
        }
        else if (parts.Length is 3)
        {
            symbolName = parts[1];
        }
        else if (parts.Length is 4)
        {
            symbolName = parts[2];
        }

        // Use this as reference for the LCID:
        // https://learn.microsoft.com/en-us/openspecs/office_standards/ms-oe376/6c085406-a698-4e12-9d4d-c3b0ee3dbc4a
        Dictionary<DiscordLocale, string> locales = new(2);
        if (fullSymbolName.EndsWith(".name", StringComparison.OrdinalIgnoreCase))
        {
            ResourceManager commandNames = new(typeof(CommandNames));
            locales.Add(DiscordLocale.de, commandNames.GetString(symbolName, new CultureInfo(1031)) ?? string.Empty);
            locales.Add(DiscordLocale.en_US, symbolName);
        }
        else if (fullSymbolName.EndsWith(".description", StringComparison.OrdinalIgnoreCase) && !topLevelCommand)
        {
            ResourceManager commandDescriptions = new(typeof(CommandDescriptions));
            locales.Add(DiscordLocale.de, commandDescriptions.GetString(symbolName, new CultureInfo(1031)) ?? string.Empty);
            locales.Add(DiscordLocale.en_US, commandDescriptions.GetString(symbolName, new CultureInfo(1033)) ?? string.Empty);
        }

        return new ValueTask<IReadOnlyDictionary<DiscordLocale, string>>(locales);
    }
}
