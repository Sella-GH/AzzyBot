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
    protected static int CheckSettings<T>(T? settings, ILogger logger, List<string>? excluded = null, bool isClass = false)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));

        PropertyInfo[] properties = settings.GetType().GetProperties();
        int missingSettings = 0;

        void LogAndIncrement(string settingName)
        {
            logger.SettingNotFilled(settingName);
            missingSettings++;
        }

        foreach (PropertyInfo property in properties)
        {
            if (excluded?.Contains(property.Name) == true)
                continue;

            object? value = property.GetValue(settings);
            Type propertyType = property.PropertyType;

            if (propertyType.IsClass)
            {
                missingSettings = CheckSettings(value, logger, excluded, true);
                continue;
            }

            switch (property.PropertyType)
            {
                case Type t when t.Equals(typeof(string)):
                    if (string.IsNullOrWhiteSpace((string?)value))
                        LogAndIncrement(property.Name);

                    break;

                case Type t when t.Equals(typeof(ulong)) || t.Equals(typeof(int)):
                    if (Convert.ToInt64(value, CultureInfo.InvariantCulture) is 0)
                        LogAndIncrement(property.Name);

                    break;

                case Type t when t.Equals(typeof(TimeSpan)):
                    if (value is TimeSpan timespan && timespan.Equals(TimeSpan.Zero))
                        LogAndIncrement(property.Name);

                    break;
            }
        }

        if (missingSettings == 0 || isClass)
            return missingSettings;

        if (!AzzyStatsGeneral.GetBotName.Contains("Docker", StringComparison.OrdinalIgnoreCase))
        {
            logger.PressAnyKeyToStop();
            Console.ReadKey();
        }

        Environment.Exit(1);

        return 0;
    }
}
