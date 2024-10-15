#pragma warning disable S125 // Sections of code should not be commented out
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Resources;
using System.Threading.Tasks;
using DSharpPlus.Commands.Processors.SlashCommands.Localization;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Localization;

public sealed class CommandLocalizer(ILogger<CommandLocalizer> logger) : IInteractionLocalizer
{
    private readonly ILogger<CommandLocalizer> _logger = logger;

    public ValueTask<IReadOnlyDictionary<DiscordLocale, string>> TranslateAsync(string fullSymbolName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullSymbolName);

        bool topLevelCommand = false;
        bool isParameter = false;
        string symbolName = string.Empty;
        string[] parts;

        // TODO: Rework this so it's not confused when having 2 nested command groups
        if (!fullSymbolName.Contains("parameters", StringComparison.OrdinalIgnoreCase))
        {
            parts = fullSymbolName.Split('.');
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
        }
        else
        {
            parts = fullSymbolName.Split("parameters");
            parts = parts[1].Split('.');
            symbolName = parts[1];
            isParameter = true;
        }

        if (string.IsNullOrWhiteSpace(symbolName))
            throw new InvalidOperationException("Symbol name cannot be null or whitespace.");

        // Use this as reference for the LCID:
        // https://learn.microsoft.com/en-us/openspecs/office_standards/ms-oe376/6c085406-a698-4e12-9d4d-c3b0ee3dbc4a
        Dictionary<DiscordLocale, string> locales = new(1);
        if (fullSymbolName.EndsWith(".name", StringComparison.OrdinalIgnoreCase))
        {
            ResourceManager resources = (!isParameter) ? new(typeof(CommandNames)) : new(typeof(CommandParamNames));
            locales.Add(DiscordLocale.de, resources.GetString(symbolName, new CultureInfo(1031)) ?? string.Empty);
        }
        else if (fullSymbolName.EndsWith(".description", StringComparison.OrdinalIgnoreCase) && (!topLevelCommand || CheckIfCommandIsExcluded(fullSymbolName)))
        {
            ResourceManager resources = (!isParameter) ? new(typeof(CommandDescriptions)) : new(typeof(CommandParamDescriptions));
            locales.Add(DiscordLocale.de, resources.GetString(symbolName, new CultureInfo(1031)) ?? string.Empty);
        }

        _logger.LogWarning(fullSymbolName);
        _logger.LogWarning(symbolName);
        foreach (KeyValuePair<DiscordLocale, string> locale in locales)
        {
            _logger.LogWarning($"{locale.Key}: {locale.Value} ({locale.Value.Length})");
        }

        return new ValueTask<IReadOnlyDictionary<DiscordLocale, string>>(locales);
    }

    [SuppressMessage("Minor Code Smell", "S3267:Loops should be simplified with \"LINQ\" expressions", Justification = "Linq is about 8x slower than this.")]
    private static bool CheckIfCommandIsExcluded(string fullSymbolName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullSymbolName);

        string[] excludedCommands = ["core.stats"];
        foreach (string excluded in excludedCommands)
        {
            if (fullSymbolName.EndsWith($"{excluded}.description", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

#pragma warning restore S125 // Sections of code should not be commented out
