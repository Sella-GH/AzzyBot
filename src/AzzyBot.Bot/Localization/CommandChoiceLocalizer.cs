using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;

namespace AzzyBot.Bot.Localization;

public static class CommandChoiceLocalizer
{
    public static Dictionary<string, string> GenerateTranslations(string typeName, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Dictionary<string, string> locales = new(1);
        ResourceManager resources = new(typeof(CommandChoices));
        locales.Add("de", resources.GetString($"{typeName}.{value}", new CultureInfo(1031)) ?? string.Empty);

        return locales;
    }
}
