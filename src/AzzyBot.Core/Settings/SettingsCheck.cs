using System;
using System.Collections.Generic;
using System.Linq;
using AzzyBot.Core.Utilities;

namespace AzzyBot.Core.Settings;

public static class SettingsCheck
{
    public static int CheckSettings<T>(T? settings, IEnumerable<string>? excluded = null, bool isClass = false)
    {
        if (settings is null)
            throw new InvalidOperationException("Settings is null");

        int missingSettings = 0;
        void LogAndIncrement(string settingName)
        {
            Console.Error.WriteLine("{0} has to be filled out!", settingName);
            missingSettings++;
        }

        if (settings is ISettings settingsInterface)
        {
            foreach (KeyValuePair<string, object?> kvp in settingsInterface.GetProperties())
            {
                if (excluded?.Contains(kvp.Key) is true)
                    continue;

                switch (kvp.Value)
                {
                    case int i when i is 0:
                    case ulong ul when ul is 0:
                    case string str when string.IsNullOrWhiteSpace(str):
                    case TimeSpan ts when ts == TimeSpan.Zero:
                        LogAndIncrement(kvp.Key);
                        break;

                    case ISettings nestedSettings:
                        missingSettings += CheckSettings(nestedSettings, excluded, true);
                        break;
                }
            }
        }
        else
        {
            throw new InvalidOperationException($"Settings object does not implement {nameof(ISettings)} interface");
        }

        if (missingSettings is 0 || isClass)
            return missingSettings;

        if (!SoftwareStats.GetAppName.Contains("Docker", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Press any key to continue");
            Console.ReadKey();
        }

        Environment.Exit(1);

        return 0;
    }
}

public interface ISettings
{
    public Dictionary<string, object?> GetProperties();
}
