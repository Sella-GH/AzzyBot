using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using AzzyBot.Logging;
using AzzyBot.Utilities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services;

internal abstract class BaseService
{
    protected static void CheckSettings<T>(T? type, ILogger logger, List<string>? excluded = null)
    {
        if (type is null)
            throw new InvalidOperationException("Settings can't be checked");

        PropertyInfo[] properties = type.GetType().GetProperties();

        List<string> missingSettings = [];
        foreach (PropertyInfo property in properties)
        {
            object? value = property.GetValue(type);

            switch (property.PropertyType)
            {
                case Type t when t.Equals(typeof(string)):
                    if (excluded?.Contains(property.Name) == true)
                        break;

                    if (string.IsNullOrWhiteSpace((string?)value))
                        missingSettings.Add(property.Name);

                    break;

                case Type t when t.Equals(typeof(ulong)) || t.Equals(typeof(int)):
                    if (excluded?.Contains(property.Name) == true)
                        break;

                    if (Convert.ToInt64(value, CultureInfo.InvariantCulture) is 0)
                        missingSettings.Add(property.Name);

                    break;

                case Type t when t.Equals(typeof(TimeSpan)):
                    if (excluded?.Contains(property.Name) == true)
                        break;

                    if (value is TimeSpan timespan && timespan.Equals(TimeSpan.Zero))
                        missingSettings.Add(property.Name);

                    break;
            }
        }

        if (missingSettings.Count == 0)
            return;

        foreach (string missingSetting in missingSettings)
        {
            logger.SettingNotFilled(missingSetting);
        }

        if (!AzzyStatsGeneral.GetBotName.Contains("Docker", StringComparison.Ordinal))
            Console.ReadKey();

        Environment.Exit(1);
    }
}
