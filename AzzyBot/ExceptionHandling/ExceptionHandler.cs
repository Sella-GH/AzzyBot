using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Settings;
using AzzyBot.Modules.Core.Strings;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;

namespace AzzyBot.ExceptionHandling;

/// <summary>
/// Handles exceptions and logs them.
/// </summary>
internal static class ExceptionHandler
{
    /// <summary>
    /// Acknowledges the error in the interaction context.
    /// </summary>
    /// <returns>The ID of the message acknowledging the error.</returns>
    private static async Task<ulong> AcknowledgeErrorAsync(InteractionContext ctx)
    {
        DiscordMessage message = await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(CoreStringBuilder.GetExceptionHandlingErrorDiscovered((await CoreDiscordCommands.GetMemberAsync(CoreSettings.OwnerUserId, ctx.Guild)).Mention)).AddMention(UserMention.All));
        return message.Id;
    }

    /// <summary>
    /// Builds an embed containing relevant information about the occured error.
    /// </summary>
    /// <param name="ex">The exception name.</param>
    /// <param name="exMessage">The exception message.</param>
    /// <param name="timestamp">The timestamp of the error.</param>
    /// <param name="jsonMessage">The JSON message if the error happened during a web request.</param>
    /// <param name="discordMessage">The complete URL of the discord message that caused the error.</param>
    /// <param name="user">The discord mention tag of the user who caused the error.</param>
    /// <param name="commandName">The name of the command that caused the error.</param>
    /// <param name="options">The selected options by the user of the command that caused the error.</param>
    /// <returns>The built Discord embed.</returns>
    private static DiscordEmbed BuildErrorEmbed(string ex, string exMessage, string timestamp, string jsonMessage = "", string discordMessage = "", string user = "", string commandName = "", List<(string, string)>? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exMessage, nameof(exMessage));
        ArgumentException.ThrowIfNullOrWhiteSpace(timestamp, nameof(timestamp));

        const string bugReportUrl = "https://github.com/Sella-GH/AzzyBot/issues/new?assignees=Sella-GH&labels=bug&projects=&template=bug_report.yml&title=%5BBUG%5D";

        DiscordEmbedBuilder builder = new()
        {
            Color = DiscordColor.Red,
            Title = "Error occured"
        };

        if (!string.IsNullOrWhiteSpace(discordMessage))
            builder.AddField("Message", discordMessage);

        if (!string.IsNullOrWhiteSpace(user))
            builder.AddField("User", user);

        if (!string.IsNullOrWhiteSpace(commandName))
            builder.AddField("Slash Command", commandName);

        if (options is not null && options.Count != 0)
        {
            string values = string.Empty;
            foreach ((string, string) option in options)
            {
                values += $"**{option.Item1}**: {option.Item2}\n";
            }

            builder.AddField("Options", values);
        }

        builder.AddField("Timestamp", timestamp).AddField("Exception", ex).AddField("Exception Message", exMessage);

        if (!string.IsNullOrWhiteSpace(jsonMessage))
            builder.AddField("Advanced Error Message", jsonMessage);

        builder.AddField("Bug report", $"Send a [bug report]({bugReportUrl}) to help us fixing this issue!\nYour Contribution is very welcome.");

        return builder;
    }

    private static readonly Action<ILogger, string, Exception?> LogDebug = LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1000, "Debug"), "{Message}");
    private static readonly Action<ILogger, string, Exception?> LogInfo = LoggerMessage.Define<string>(LogLevel.Information, new EventId(1001, "Info"), "{Message}");
    private static readonly Action<ILogger, string, Exception?> LogWarn = LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1002, "Warning"), "{Message}");
    private static readonly Action<ILogger, string, Exception?> LogErr = LoggerMessage.Define<string>(LogLevel.Error, new EventId(1003, "Error"), "{Message}");
    private static readonly Action<ILogger, string, Exception?> LogCrit = LoggerMessage.Define<string>(LogLevel.Critical, new EventId(1004, "Crit"), "{Message}");

    /// <summary>
    /// Logs an error asynchronously.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="jsonMessage">The JSON message if the error happened during a web request.</param>
    /// <exception cref="InvalidOperationException">Throws when the exception message could not be sent.</exception>
    /// <exception cref="IOException">Throws when the temp file for the StackTrace could not be created or deleted.</exception>
    internal static async Task LogErrorAsync(Exception ex, string jsonMessage = "")
    {
        string stackTrace = ex.Message;
        if (ex.StackTrace is not null)
            stackTrace += $"\n{ex.StackTrace}";

        string message = ex.Message;
        string timestamp = DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        try
        {
            string tempFilePath = await CoreFileOperations.CreateTempFileAsync(stackTrace, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-StackTrace.log");
            if (string.IsNullOrWhiteSpace(tempFilePath))
                throw new IOException("Couldn't create temp file for StackTrace!");

            if (!await AzzyBot.SendMessageAsync(CoreSettings.ErrorChannelId, $"<@!{CoreSettings.OwnerUserId}> new error dropped in!", BuildErrorEmbed(ex.GetType().Name, message, timestamp, jsonMessage), tempFilePath, true))
                throw new InvalidOperationException("Exception message couldn't be sent!");

            if (!CoreFileOperations.DeleteTempFile(tempFilePath))
                throw new IOException($"{tempFilePath} couldn't be deleted!");
        }
        catch (IOException e)
        {
            throw new IOException("Error happened while deleting temp file!", e);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Error happened while sending error message!", e);
        }
    }

    /// <summary>
    /// Logs an error asynchronously in an interaction context.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="ctx">The interaction context.</param>
    /// <param name="jsonMessage">The JSON message if the error happened during a web request.</param>
    /// <exception cref="InvalidOperationException">Throws when the exception message could not be sent.</exception>
    /// <exception cref="IOException">Throws when the temp file for the StackTrace could not be created or deleted.</exception>
    internal static async Task LogErrorAsync(Exception ex, InteractionContext ctx, string jsonMessage = "")
    {
        ulong messageId = await AcknowledgeErrorAsync(ctx);

        string stackTrace = ex.Message;
        if (ex.StackTrace is not null)
            stackTrace += $"\n{ex.StackTrace}";

        string message = $"https://discord.com/channels/{ctx.Guild.Id}/{ctx.Channel.Id}/{messageId}";
        string user = ctx.Member.Mention;
        string slashCommandName = ctx.QualifiedName;
        List<(string, string)> slashCommandOptions = [];
        ProcessOptions(ctx.Interaction.Data.Options, slashCommandOptions);

        string timestamp = ctx.Interaction.CreationTimestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        string exMessage = ex.Message;

        LogErr(ctx.Client.Logger, ex.ToString(), null);

        try
        {
            string tempFilePath = await CoreFileOperations.CreateTempFileAsync(stackTrace, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-StackTrace.log");
            if (string.IsNullOrWhiteSpace(tempFilePath))
                throw new IOException("Couldn't create temp file for StackTrace!");

            if (!await AzzyBot.SendMessageAsync(CoreSettings.ErrorChannelId, $"<@{CoreSettings.OwnerUserId}> new error dropped in!", BuildErrorEmbed(ex.GetType().Name, exMessage, timestamp, jsonMessage, message, user, slashCommandName, slashCommandOptions), tempFilePath, true))
                throw new InvalidOperationException("Exception message couldn't be sent!");

            if (!CoreFileOperations.DeleteTempFile(tempFilePath))
                throw new IOException($"{tempFilePath} couldn't be deleted!");
        }
        catch (IOException e)
        {
            throw new IOException("Error happened while deleting temp file!", e);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Error happened while sending error message!", e);
        }
    }

    /// <summary>
    /// Logs an error asynchronously in an autocomplete context.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="ctx">The autocomplete context.</param>
    /// <param name="jsonMessage">The JSON message if the error happened during a web request.</param>
    /// <exception cref="InvalidOperationException">Throws when the exception message could not be sent.</exception>
    /// <exception cref="IOException">Throws when the temp file for the StackTrace could not be created or deleted.</exception>
    internal static async Task LogErrorAsync(Exception ex, AutocompleteContext ctx, string jsonMessage = "")
    {
        string stackTrace = ex.Message;
        if (ex.StackTrace is not null)
            stackTrace += $"\n{ex.StackTrace}";

        string user = ctx.Member.Mention;
        List<(string, string)> slashCommandOptions = [];
        ProcessOptions(ctx.Interaction.Data.Options, slashCommandOptions);

        string timestamp = ctx.Interaction.CreationTimestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        string exMessage = ex.Message;

        LogErr(ctx.Client.Logger, ex.ToString(), null);

        try
        {
            string tempFilePath = await CoreFileOperations.CreateTempFileAsync(stackTrace, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-StackTrace.log");
            if (string.IsNullOrWhiteSpace(tempFilePath))
                throw new IOException("Couldn't create temp file for StackTrace!");

            if (!await AzzyBot.SendMessageAsync(CoreSettings.ErrorChannelId, $"<@{CoreSettings.OwnerUserId}> new error dropped in!", BuildErrorEmbed(ex.GetType().Name, exMessage, timestamp, jsonMessage, string.Empty, user, string.Empty, slashCommandOptions), tempFilePath, true))
                throw new InvalidOperationException("Exception message couldn't be sent!");

            if (!CoreFileOperations.DeleteTempFile(tempFilePath))
                throw new IOException($"{tempFilePath} couldn't be deleted!");
        }
        catch (IOException e)
        {
            throw new IOException("Error happened while deleting temp file!", e);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Error happened while sending error message!", e);
        }
    }

    /// <summary>
    /// Processes the options of a command and writes them into a given list.
    /// </summary>
    /// <param name="options">The options of the command.</param>
    /// <param name="slashCommandOptions">The processed options of the command.</param>
    private static void ProcessOptions(IEnumerable<DiscordInteractionDataOption> options, List<(string, string)> slashCommandOptions)
    {
        if (options is null)
            return;

        foreach (DiscordInteractionDataOption option in options)
        {
            if (!string.IsNullOrWhiteSpace(option.Name) && option.Value is not null)
            {
                slashCommandOptions.Add((option.Name, option.Value.ToString() ?? "undefined"));
            }
            else if (option.Options is not null)
            {
                ProcessOptions(option.Options, slashCommandOptions);
            }
        }
    }

    /// <summary>
    /// Logs a message.
    /// </summary>
    /// <param name="level">The log level of the message.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="jsonMessage">The JSON message if the error happened during a web request.</param>
    /// <exception cref="ArgumentOutOfRangeException">Throws when the log level is not in the <seealso cref="LogLevel"/> enum.</exception>
    internal static bool LogMessage(LogLevel level, string message, string jsonMessage = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message, nameof(message));

        ILogger<BaseDiscordClient> client = AzzyBot.GetDiscordClientLogger;

        switch (level)
        {
            case LogLevel.Debug:
                LogDebug(client, message, null);
                break;

            case LogLevel.Information:
                LogInfo(client, message, null);
                break;

            case LogLevel.Warning:
                LogWarn(client, message, null);
                break;

            case LogLevel.Error:
                LogErr(client, message, null);
                break;

            case LogLevel.Critical:
                LogCrit(client, message, null);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(level), level.ToString(), "Value is not defined");
        }

        if (!string.IsNullOrWhiteSpace(jsonMessage))
            LogErr(client, jsonMessage, null);

        return true;
    }
}
