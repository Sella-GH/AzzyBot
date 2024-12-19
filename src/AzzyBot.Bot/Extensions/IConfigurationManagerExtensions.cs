using System.IO;
using Microsoft.Extensions.Configuration;

namespace AzzyBot.Bot.Extensions;

public static class IConfigurationManagerExtensions
{
    public static void AddAppConfiguration(this IConfigurationManager configurationManager, bool isDev, bool isDocker)
    {
        string settingsFile = "AzzyBotSettings.json";
        if (isDev)
        {
            settingsFile = "AzzyBotSettings-Dev.json";
        }
        else if (isDocker)
        {
            settingsFile = "AzzyBotSettings-Docker.json";
        }

        string path = Path.Combine("Settings", settingsFile);

        configurationManager.AddJsonFile(path);

        if (isDev)
            return;

        path = Path.Combine("Modules", "Core", "Files", "AppStats.json");

        configurationManager.AddJsonFile(path);
    }
}
