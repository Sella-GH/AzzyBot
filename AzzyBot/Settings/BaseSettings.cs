using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using AzzyBot.Modules.Core;
using AzzyBot.Settings.AzuraCast;
using AzzyBot.Settings.ClubManagement;
using AzzyBot.Settings.Core;
using AzzyBot.Settings.MusicStreaming;
using Microsoft.Extensions.Configuration;

namespace AzzyBot.Settings;

internal abstract class BaseSettings
{
    internal static bool ActivateAzuraCast { get; private set; }
    internal static bool ActivateClubManagement { get; private set; }
    internal static bool ActivateMusicStreaming { get; private set; }
    internal static bool ActivateTimers { get; private set; }

    protected static readonly IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("Settings/appsettings.json", true, false);
    protected static IConfiguration? Config { get; private set; }

    private static void SetDevConfig()
    {
        if (CoreAzzyStatsGeneral.GetBotName != "AzzyBot-Dev")
            return;

        builder.Sources.Clear();
        builder.AddJsonFile("Settings/appsettings.development.json", true, false);
    }

    internal static void LoadSettings()
    {
        SetDevConfig();

        Config = builder.Build();

        ActivateAzuraCast = Convert.ToBoolean(Config["AzuraCast:ActivateAzuraCast"], CultureInfo.InvariantCulture);
        ActivateClubManagement = Convert.ToBoolean(Config["ClubManagement:ActivateClubManagement"], CultureInfo.InvariantCulture);
        ActivateMusicStreaming = Convert.ToBoolean(Config["MusicStreaming:ActivateMusicStreaming"], CultureInfo.InvariantCulture);

        if (!CoreSettings.LoadCore())
            throw new InvalidOperationException("Core settings can't be loaded");

        // Ensure core is activated first
        if (!CoreSettings.CoreSettingsLoaded)
        {
            Console.Error.WriteLine("Core settings aren't loaded");
            Console.ReadKey();
            Environment.Exit(1);
        }

        if (ActivateAzuraCast && !AzuraCastSettings.LoadAzuraCast())
            throw new InvalidOperationException("AzuraCast settings can't be loaded");

        if (ActivateClubManagement && !ClubManagementSettings.LoadClubManagement())
            throw new InvalidOperationException("ClubManagement settings can't be loaded");

        if (ActivateMusicStreaming && !MusicStreamingSettings.LoadMusicStreaming())
            throw new InvalidOperationException("MusicStreaming settings can't be loaded");

        if ((ActivateAzuraCast || ActivateClubManagement) && (AzuraCastSettings.AutomaticFileChangeCheck || AzuraCastSettings.AutomaticServerPing || AzuraCastSettings.AutomaticUpdateCheck || ClubManagementSettings.AutomaticClubClosingCheck))
            ActivateTimers = true;
    }

    protected static bool CheckSettings(Type type, List<string>? excludedStrings = null, List<string>? excludedInts = null)
    {
        // Get all Properties of this class
        PropertyInfo[] properties = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Static);

        List<string> failed = [];

        // Loop through all properties and check if they are null, whitespace or 0
        // If yes add them to the list
        foreach (PropertyInfo property in properties)
        {
            object? value = property.GetValue(null);

            if (property.PropertyType == typeof(string))
            {
                if (excludedStrings?.Contains(property.Name) == true)
                    continue;

                if (string.IsNullOrWhiteSpace(value as string))
                    failed.Add(property.Name);
            }
            else if (property.PropertyType == typeof(ulong) || property.PropertyType == typeof(int))
            {
                if (excludedInts?.Contains(property.Name) == true)
                    continue;

                if (Convert.ToInt64(value, CultureInfo.InvariantCulture) == 0)
                    failed.Add(property.Name);
            }
            else if (property.PropertyType == typeof(TimeSpan))
            {
                if (value is TimeSpan timeSpan && timeSpan == TimeSpan.Zero)
                    failed.Add(property.Name);
            }
        }

        if (failed.Count == 0)
            return true;

        foreach (string item in failed)
        {
            Console.Error.WriteLine($"{item} has to be filled out!");
        }

        if (CoreMisc.CheckIfLinuxOs())
            return false;

        Console.Out.WriteLine("Press any key to acknowledge...");
        Console.ReadKey();

        return false;
    }
}
