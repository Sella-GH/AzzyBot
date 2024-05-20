using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using AzzyBot.Utilities;

namespace AzzyBot.Settings;

public static class AzzyBotSettingsCheck
{
    public static int CheckSettings<T>(T? settings, List<string>? excluded = null, bool isClass = false)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));

        PropertyInfo[] properties = settings.GetType().GetProperties();
        int missingSettings = 0;

        void LogAndIncrement(string settingName)
        {
            Console.Error.WriteLine("{0} has to be filled out!", settingName);
            missingSettings++;
        }

        foreach (PropertyInfo property in properties.Where(p => excluded?.Contains(p.Name) == false))
        {
            object? value = property.GetValue(settings);
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

                case Type t when t.IsClass:
                    missingSettings = CheckSettings(value, excluded, true);

                    continue;
            }
        }

        if (missingSettings == 0 || isClass)
            return missingSettings;

        if (!AzzyStatsSoftware.GetBotName.Contains("Docker", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Press any key to continue");
            Console.ReadKey();
        }

        Environment.Exit(1);

        return 0;
    }
}
