using System;
using System.Collections.Generic;
using DSharpPlus.Entities;

namespace AzzyBot.Utilities;

internal sealed class EmbedBuilder
{
    internal static DiscordEmbedBuilder CreateExceptionEmbed(Exception ex, string timestamp, string? jsonMessage = null, DiscordMessage? message = null, DiscordUser? user = null, string? commandName = null, Dictionary<string, string>? commandOptions = null)
    {
        ArgumentNullException.ThrowIfNull(ex, nameof(ex));
        ArgumentNullException.ThrowIfNull(timestamp, nameof(timestamp));

        const string bugReportUrl = "https://github.com/Sella-GH/AzzyBot/issues/new?assignees=Sella-GH&labels=bug&projects=&template=bug_report.yml&title=%5BBUG%5D";
        string os = AzzyStatsGeneral.GetOperatingSystem;
        string arch = AzzyStatsGeneral.GetOsArchitecture;
        string botName = AzzyStatsGeneral.GetBotName;
        string botVersion = AzzyStatsGeneral.GetBotVersion;

        DiscordEmbedBuilder builder = new()
        {
            Color = DiscordColor.Red,
            Title = "Exception occurred"
        };

        builder.AddField("Exception", ex.GetType().Name);
        builder.AddField("Description", ex.Message);

        if (!string.IsNullOrWhiteSpace(jsonMessage))
            builder.AddField("Advanced Error", jsonMessage);

        builder.AddField("Timestamp", timestamp);

        if (message is not null)
            builder.AddField("Message", message.JumpLink.ToString());

        if (user is not null)
            builder.AddField("User", user.Mention);

        if (!string.IsNullOrWhiteSpace(commandName))
            builder.AddField("Command", commandName);

        if (commandOptions?.Count > 0)
        {
            string values = string.Empty;
            foreach (KeyValuePair<string, string> kvp in commandOptions)
            {
                values += $"**{kvp.Key}**: {kvp.Value}";
            }

            builder.AddField("Options", values);
        }

        builder.AddField("OS", os);
        builder.AddField("Arch", arch);
        builder.AddField("Bug report", $"Send a [bug report]({bugReportUrl}) to help us fixing this issue!\nPlease include a screenshot of this exception embed and the StackTrace file.\nYour Contribution is very welcome.");
        builder.WithAuthor(botName, bugReportUrl);
        builder.WithFooter($"Version: {botVersion}");

        return builder;
    }
}
