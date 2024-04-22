using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Modules;
using AzzyBot.Modules.AzuraCast.Strings;
using AzzyBot.Modules.ClubManagement.Strings;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Strings;
using AzzyBot.Modules.MusicStreaming.Strings;

namespace AzzyBot.Strings;

internal abstract class BaseStringBuilder
{
    [SuppressMessage("Roslynator", "RCS1208:Reduce 'if' nesting", Justification = "Code Style")]
    internal static async Task LoadStringsAsync()
    {
        if (!await CoreStringBuilder.LoadCoreStringsAsync())
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "Core strings can't be loaded", null);
            await AzzyBot.BotShutdownAsync();
        }

        if (ModuleStates.AzuraCast && !await AcStringBuilder.LoadAzuraCastStringsAsync())
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "AzuraCast strings can't be loaded", null);
            await AzzyBot.BotShutdownAsync();
        }

        if (ModuleStates.ClubManagement && !await CmStringBuilder.LoadClubManagementStringsAsync())
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "ClubManagement strings can't be loaded", null);
            await AzzyBot.BotShutdownAsync();
        }

        if (ModuleStates.MusicStreaming && !await MsStringBuilder.LoadMusicStreamingStringsAsync())
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "MusicStreaming strings can't be loaded", null);
            await AzzyBot.BotShutdownAsync();
        }
    }

    protected static bool CheckStrings<T>(T instance)
    {
        // Get all Fields of the type
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        List<string> failed = [];

        // Loop through all fields and check if they are null or whitespace
        // If yes add them to the list
        foreach (FieldInfo field in fields)
        {
            if (field.FieldType != typeof(string))
                continue;

            string? value = field.GetValue(instance) as string;

            // Cut the string to only get the full name of the property
            if (string.IsNullOrWhiteSpace(value))
                failed.Add(field.Name.Substring(1, field.Name.IndexOf('>', StringComparison.OrdinalIgnoreCase) - 1));
        }

        if (failed.Count == 0)
            return true;

        foreach (string item in failed)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, $"Field {item} has to be filled out!", null);
        }

        if (CoreMisc.CheckIfLinuxOs())
            return false;

        LoggerBase.LogInfo(LoggerBase.GetLogger, "Press any key to acknowledge...", null);
        Console.ReadKey();

        return false;
    }

    protected static string BuildString(string template, string parameterName, double parameterValue) => template.Replace(parameterName, parameterValue.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase);
    protected static string BuildString(string template, string parameterName, int parameterValue) => template.Replace(parameterName, parameterValue.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase);
    protected static string BuildString(string template, string parameterName, string parameterValue) => template.Replace(parameterName, parameterValue, StringComparison.InvariantCultureIgnoreCase);
}
