using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AzzyBot.ExceptionHandling;
using AzzyBot.Modules.AzuraCast.Settings;
using AzzyBot.Modules.ClubManagement.Settings;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Settings;
using AzzyBot.Modules.MusicStreaming.Settings;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Modules;

internal abstract class BaseSettings
{
    internal static bool ActivateAzuraCast { get; private set; }
    internal static bool ActivateClubManagement { get; private set; }
    internal static bool ActivateMusicStreaming { get; private set; }
    protected static readonly IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "appsettings.json"), true, false);
    protected static IConfiguration? Config { get; private set; }

    private static void SetDevConfig()
    {
        if (CoreAzzyStatsGeneral.GetBotName != "AzzyBot-Dev")
            return;

        builder.Sources.Clear();
        builder.AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "appsettings.development.json"), true, false);
    }

    internal static async Task LoadSettingsAsync()
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
            await Console.Error.WriteLineAsync("Core settings aren't loaded");
            Environment.Exit(1);
        }

        if (ActivateAzuraCast && !await AcSettings.LoadAzuraCastAsync())
            throw new InvalidOperationException("AzuraCast settings can't be loaded");

        if (ActivateClubManagement && ActivateAzuraCast && !CmSettings.LoadClubManagement())
            throw new InvalidOperationException("ClubManagement settings can't be loaded");

        if (ActivateMusicStreaming && ActivateAzuraCast && !await MsSettings.LoadMusicStreamingAsync())
            throw new InvalidOperationException("MusicStreaming settings can't be loaded");
    }

    internal static bool CheckIfChannelsExist(DiscordGuild guild)
    {
        bool core = CoreDiscordCommands.CheckIfChannelExists(guild, CoreSettings.ErrorChannelId);
        bool azuraCast = true;
        bool clubManagement = true;

        if (ActivateAzuraCast)
        {
            List<ulong> azuraCastChannels = CoreDiscordCommands.CheckIfChannelsExist(guild, [AcSettings.MusicRequestsChannelId, AcSettings.OutagesChannelId]);
            if (azuraCastChannels.Count > 0)
            {
                azuraCast = false;

                foreach (ulong channel in azuraCastChannels)
                {
                    ExceptionHandler.LogMessage(LogLevel.Error, $"Channel with ID **{channel}** does not exist in guild with ID **{guild.Id}** and name **{guild.Name}**!");
                }
            }
        }

        if (ActivateClubManagement)
            clubManagement = CoreDiscordCommands.CheckIfChannelExists(guild, CmSettings.ClubNotifyChannelId);

        return core && azuraCast && clubManagement;
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

            switch (property.PropertyType)
            {
                case Type t when t == typeof(string):
                    if (excludedStrings?.Contains(property.Name) == true)
                        continue;

                    if (string.IsNullOrWhiteSpace(value as string))
                        failed.Add(property.Name);

                    break;

                case Type t when t == typeof(ulong) || t == typeof(int):
                    if (excludedInts?.Contains(property.Name) == true)
                        continue;

                    if (Convert.ToInt64(value, CultureInfo.InvariantCulture) == 0)
                        failed.Add(property.Name);

                    break;

                case Type t when t == typeof(TimeSpan):
                    if (value is TimeSpan timeSpan && timeSpan == TimeSpan.Zero)
                        failed.Add(property.Name);

                    break;
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
