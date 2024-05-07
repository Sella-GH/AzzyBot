using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Settings;
using AzzyBot.Utilities;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services;

internal sealed class DiscordBotService
{
    private readonly ILogger<DiscordBotService> _logger;
    private readonly AzzyBotSettings _settings;
    private readonly DiscordShardedClient _shardedClient;

    [SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = "Otherwise it throws CS9124")]
    public DiscordBotService(AzzyBotSettings settings, ILogger<DiscordBotService> logger, DiscordBotServiceHost botServiceHost)
    {
        _settings = settings;
        _logger = logger;
        _shardedClient = botServiceHost._shardedClient;
    }

    internal async Task<bool> LogExceptionAsync(Exception ex, DateTime timestamp, string? info = null)
    {
        string exMessage = ex.Message;
        string stackTrace = ex.StackTrace ?? string.Empty;
        string exInfo = (string.IsNullOrWhiteSpace(stackTrace)) ? exMessage : $"{exMessage}\n{stackTrace}";
        string timestampString = timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        _logger.LogCritical("{Ex}", ex.ToString());

        try
        {
            string tempFilePath = await FileOperations.CreateTempFileAsync(exInfo, $"StackTrace_{timestampString}.log");

            const string message = "A new error happend!";
            DiscordEmbed embed = EmbedBuilder.CreateExceptionEmbed(ex, timestampString, info);
            bool messageSent = await SendMessageAsync(_settings?.ErrorChannelId ?? 0, message, [embed], [tempFilePath]);

            if (!messageSent)
                _logger.UnableToSendMessage("Error message was not sent");

            FileOperations.DeleteTempFilePath(tempFilePath);

            return true;
        }
        catch (IOException e)
        {
            _logger.UnableToLogException(e.ToString());
        }
        catch (SecurityException e)
        {
            _logger.UnableToLogException(e.ToString());
        }

        return false;
    }

    internal async Task<bool> LogExceptionAsync(Exception ex, DateTime timestamp, CommandContext ctx, string? info = null)
    {
        DiscordMessage discordMessage = await AcknowledgeExceptionAsync(ctx);
        DiscordUser discordUser = ctx.User;
        string exMessage = ex.Message;
        string stackTrace = ex.StackTrace ?? string.Empty;
        string exInfo = (string.IsNullOrWhiteSpace(stackTrace)) ? exMessage : $"{exMessage}\n{stackTrace}";
        string timestampString = timestamp.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        string commandName = ctx.Command.FullName;
        Dictionary<string, string> commandOptions = [];
        ProcessOptions(ctx.Arguments, commandOptions);

        _logger.LogCritical("{Ex}", ex.ToString());

        try
        {
            string tempFilePath = await FileOperations.CreateTempFileAsync(exInfo, $"StackTrace_{timestampString}.log");

            const string message = "A new error happend!";
            DiscordEmbed embed = EmbedBuilder.CreateExceptionEmbed(ex, timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), info, discordMessage, discordUser, commandName, commandOptions);
            bool messageSent = await SendMessageAsync(_settings.ErrorChannelId, message, [embed], [tempFilePath]);

            if (!messageSent)
                _logger.UnableToSendMessage("Error message was not sent");

            FileOperations.DeleteTempFilePath(tempFilePath);

            return true;
        }
        catch (IOException e)
        {
            _logger.UnableToLogException(e.ToString());
        }
        catch (SecurityException e)
        {
            _logger.UnableToLogException(e.ToString());
        }

        return true;
    }

    internal async Task<bool> SendMessageAsync(ulong channelId, string content = "", List<DiscordEmbed>? embeds = null, List<string>? filePaths = null, IMention[]? mentions = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelId, nameof(channelId));

        await using DiscordMessageBuilder builder = new();

        if (!string.IsNullOrWhiteSpace(content))
            builder.WithContent(content);

        if (embeds?.Count > 0 && embeds.Count <= 10)
            builder.AddEmbeds(embeds);

        if (mentions is not null)
            builder.WithAllowedMentions(mentions);

        List<FileStream> streams = [];
        if (filePaths?.Count > 0 && filePaths.Count <= 10)
        {
            const long maxFileSize = 26214400; // 25 MB
            long allFileSize = 0;

            foreach (string path in filePaths)
            {
                FileInfo fileInfo = new(path);
                if (fileInfo.Length > maxFileSize || allFileSize > maxFileSize)
                    break;

                allFileSize += fileInfo.Length;

                FileStream stream = new(path, FileMode.Open, FileAccess.Read);
                streams.Add(stream);
                builder.AddFile(Path.GetFileName(path), stream);
            }
        }

        DiscordChannel? channel = null;
        foreach (KeyValuePair<int, DiscordClient> kvp in _shardedClient.ShardClients)
        {
            await foreach (DiscordGuild guild in kvp.Value.GetGuildsAsync())
            {
                if (guild.Id == _settings.ServerId)
                    channel = await kvp.Value.GetChannelAsync(channelId);
            }
        }

        DiscordMessage? message = null;
        if (channel is null)
        {
            _logger.UnableToSendMessage($"{nameof(channel)} is null");
        }
        else
        {
            message = await channel.SendMessageAsync(builder);
        }

        if (streams.Count > 0 && filePaths?.Count > 0)
        {
            foreach (FileStream stream in streams)
            {
                await stream.DisposeAsync();
            }

            foreach (string path in filePaths)
            {
                FileOperations.DeleteTempFilePath(path);
            }
        }

        return message is not null;
    }

    private static async Task<DiscordMessage> AcknowledgeExceptionAsync(CommandContext ctx)
    {
        DiscordMember? member = ctx.Guild?.Owner;
        string errorMessage = "Ooops something went wrong!\n\nPlease inform the owner of this server.";
        if (member is not null)
            errorMessage = errorMessage.Replace("the owner of this server", member.Mention, StringComparison.Ordinal);

        await using DiscordMessageBuilder builder = new()
        {
            Content = errorMessage
        };
        builder.WithAllowedMention(UserMention.All);

        return await ctx.EditResponseAsync(builder);
    }

    private static void ProcessOptions(IReadOnlyDictionary<CommandParameter, object?> paramaters, Dictionary<string, string> commandParameters)
    {
        if (paramaters is null)
            return;

        foreach (KeyValuePair<CommandParameter, object?> pair in paramaters)
        {
            string name = pair.Key.Name;
            string value = pair.Value?.ToString() ?? "undefined";

            if (!string.IsNullOrWhiteSpace(name) && value is not "undefined")
            {
                commandParameters.Add(name, value);
            }
        }
    }
}
