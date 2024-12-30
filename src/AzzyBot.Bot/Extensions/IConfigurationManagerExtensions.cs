using System.IO;

using Microsoft.Extensions.Configuration;

namespace AzzyBot.Bot.Extensions;

public static class IConfigurationManagerExtensions
{
    public static void AddAppConfiguration(this IConfigurationManager configurationManager, bool isDev, string settingsFile)
    {
        configurationManager.AddJsonFile(settingsFile);

        if (isDev)
            return;

        settingsFile = Path.Combine("Modules", "Core", "Files", "AppStats.json");

        configurationManager.AddJsonFile(settingsFile);
    }
}
