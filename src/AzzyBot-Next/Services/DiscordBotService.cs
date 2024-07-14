using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using AzzyBot.Commands.Checks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Settings;
using AzzyBot.Utilities;
using DSharpPlus;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services;

public sealed class DiscordBotService
{
    private readonly ILogger<DiscordBotService> _logger;
    private readonly AzzyBotSettingsRecord _settings;
    private readonly DbActions _db;
    private readonly DiscordClient _client;
    private const string BugReportUrl = "https://github.com/Sella-GH/AzzyBot/issues/new?assignees=Sella-GH&labels=bug&projects=&template=bug_report.yml&title=%5BBUG%5D";
    private const string BugReportMessage = $"Send a [bug report]({BugReportUrl}) to help us fixing this issue!\nPlease include a screenshot of this exception embed and the attached StackTrace file.\nYour Contribution is very welcome.";
    private const string ErrorChannelNotConfigured = $"**If you're seeing this message then I am not configured correctly!**\nTell your server admin to run */config modify-core*\n\n{BugReportMessage}";

    public DiscordBotService(AzzyBotSettingsRecord settings, DbActions dbActions, DiscordBotServiceHost botServiceHost, ILogger<DiscordBotService> logger)
    {
        ArgumentNullException.ThrowIfNull(botServiceHost, nameof(botServiceHost));

        _logger = logger;
        _settings = settings;
        _db = dbActions;
        _client = botServiceHost.Client;
    }

    public bool CheckIfClientIsConnected
        => _client.IsConnected;

    public async Task<DiscordChannel?> GetDiscordChannelAsync(ulong channelId)
    {
        try
        {
            return await _client.GetChannelAsync(channelId);
        }
        catch (NotFoundException)
        {
            _logger.ChannelNotFound(channelId);
            return null;
        }
    }

    public DiscordGuild? GetDiscordGuild(ulong guildId = 0)
    {
        if (guildId is 0)
            guildId = _settings.ServerId;

        return GetDiscordGuilds.Select(g => g.Value).FirstOrDefault(g => g.Id == guildId);
    }

    public IReadOnlyDictionary<ulong, DiscordGuild> GetDiscordGuilds
        => _client.Guilds;

    public async Task<DiscordMember?> GetDiscordMemberAsync(ulong guildId, ulong userId)
    {
        DiscordGuild? guild = GetDiscordGuild(guildId);
        DiscordMember? member = null;

        if (guild is not null)
            member = await guild.GetMemberAsync(userId);

        return member;
    }

    public DiscordRole? GetDiscordRole(ulong guildId, ulong roleId)
    {
        DiscordGuild? guild = GetDiscordGuild(guildId);
        DiscordRole? role = null;

        if (guild is not null)
            role = guild.GetRole(roleId);

        return role;
    }

    public async Task<bool> LogExceptionAsync(Exception ex, DateTime timestamp, ulong guildId = 0, string? info = null)
    {
        ArgumentNullException.ThrowIfNull(ex, nameof(ex));

        _logger.ExceptionOccured(ex);

        string exMessage = ex.Message;
        string stackTrace = ex.StackTrace ?? string.Empty;
        string exInfo = (string.IsNullOrWhiteSpace(stackTrace)) ? exMessage : $"{exMessage}\n{stackTrace}";
        string timestampString = timestamp.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        ulong errorChannelId = _settings.ErrorChannelId;
        bool errorChannelConfigured = true;

        //
        // Checks if the guild is the main guild
        // If not look if the guild has an error channel set
        // Otherwise it will use the first channel it can see
        // However if nothing is present, send to debug server
        // If there's no guild, take the current channel
        //

        if (guildId == _settings.ServerId)
        {
            errorChannelId = _settings.ErrorChannelId;
        }
        else if (guildId is not 0)
        {
            GuildsEntity? guild = await _db.GetGuildAsync(guildId);
            if (guild is null)
            {
                _logger.DatabaseGuildNotFound(guildId);
                return false;
            }

            if (guild.ErrorChannelId is not 0)
                errorChannelId = guild.ErrorChannelId;

            if (errorChannelId == _settings.ErrorChannelId)
            {
                DiscordGuild? dGuild = GetDiscordGuild(guildId);
                DiscordMember? dMember = await GetDiscordMemberAsync(guildId, _client.CurrentUser.Id);
                if (dMember is null)
                {
                    _logger.DiscordItemNotFound(nameof(DiscordMember), guildId);
                    return false;
                }

                errorChannelId = dGuild?.Channels.First(c => c.Value.Type.Equals(DiscordChannelType.Text) && c.Value.PermissionsFor(dMember).HasPermission(DiscordPermissions.AccessChannels | DiscordPermissions.SendMessages)).Value.Id ?? _settings.ErrorChannelId;
                if (errorChannelId is 0)
                {
                    _logger.DiscordItemNotFound(nameof(DiscordChannel), guildId);
                    return false;
                }

                errorChannelConfigured = false;
            }
        }

        try
        {
            string tempFilePath = await FileOperations.CreateTempFileAsync(exInfo, $"StackTrace_{timestampString}.log");

            DiscordEmbed embed = CreateExceptionEmbed(ex, timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), info);
            bool messageSent = await SendMessageAsync(errorChannelId, (errorChannelConfigured) ? BugReportMessage : ErrorChannelNotConfigured, [embed], [tempFilePath]);

            if (!messageSent)
                _logger.UnableToSendMessage("Error message was not sent");

            FileOperations.DeleteFile(tempFilePath);

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

    public async Task<bool> LogExceptionAsync(Exception ex, DateTime timestamp, SlashCommandContext ctx, ulong guildId = 0, string? info = null)
    {
        ArgumentNullException.ThrowIfNull(ex, nameof(ex));
        ArgumentNullException.ThrowIfNull(ctx, nameof(ctx));

        _logger.ExceptionOccured(ex);

        DiscordMessage? discordMessage = await AcknowledgeExceptionAsync(ctx);
        DiscordUser discordUser = ctx.User;
        string exMessage = ex.Message;
        string stackTrace = ex.StackTrace ?? string.Empty;
        string exInfo = (string.IsNullOrWhiteSpace(stackTrace)) ? exMessage : $"{exMessage}\n{stackTrace}";
        string timestampString = timestamp.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        string commandName = ctx.Command.FullName;
        ulong errorChannelId = ctx.Channel.Id;
        bool errorChannelConfigured = true;
        Dictionary<string, string> commandOptions = [];
        ProcessOptions(ctx.Arguments, commandOptions);

        //
        // Checks if the guild is the main guild
        // If not look if the guild has an error channel set
        // Otherwise it will use the first channel it can see
        // However if nothing is present, send to debug server
        // If there's no guild, take the current channel
        //

        if (guildId == _settings.ServerId)
        {
            errorChannelId = _settings.ErrorChannelId;
        }
        else if (guildId is not 0)
        {
            GuildsEntity? guild = await _db.GetGuildAsync(guildId);
            if (guild is null)
            {
                _logger.DatabaseGuildNotFound(guildId);
                return false;
            }

            if (guild.ErrorChannelId is not 0)
                errorChannelId = guild.ErrorChannelId;

            if (errorChannelId is 0)
            {
                DiscordGuild? dGuild = GetDiscordGuild(guildId);
                DiscordMember? dMember = await GetDiscordMemberAsync(guildId, _client.CurrentUser.Id);
                if (dMember is null)
                {
                    _logger.DiscordItemNotFound(nameof(DiscordMember), guildId);
                    return false;
                }

                errorChannelId = dGuild?.Channels.First(c => c.Value.Type.Equals(DiscordChannelType.Text) && c.Value.PermissionsFor(dMember).HasPermission(DiscordPermissions.AccessChannels | DiscordPermissions.SendMessages)).Value.Id ?? _settings.ErrorChannelId;
                if (errorChannelId is 0)
                {
                    _logger.DiscordItemNotFound(nameof(DiscordChannel), guildId);
                    return false;
                }

                errorChannelConfigured = false;
            }
        }
        else if (errorChannelId == ctx.Channel.Id)
        {
            errorChannelConfigured = false;
        }

        try
        {
            string tempFilePath = await FileOperations.CreateTempFileAsync(exInfo, $"StackTrace_{timestampString}.log");

            DiscordEmbed embed = CreateExceptionEmbed(ex, timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), info, discordMessage, discordUser, commandName, commandOptions);
            bool messageSent = await SendMessageAsync(errorChannelId, (errorChannelConfigured) ? BugReportMessage : ErrorChannelNotConfigured, [embed], [tempFilePath]);

            if (!messageSent)
                _logger.UnableToSendMessage("Error message was not sent");

            FileOperations.DeleteFile(tempFilePath);

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

    public async Task RespondToChecksExceptionAsync(ChecksFailedException ex, SlashCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(ex, nameof(ex));
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        if (!CheckIfClientIsConnected)
        {
            _logger.BotNotConnected();
            return;
        }

        ContextCheckFailedData? moduleActivatedCheck = ex.Errors.FirstOrDefault(e => e.ContextCheckAttribute is ModuleActivatedCheckAttribute);
        if (moduleActivatedCheck is not null)
        {
            await context.EditResponseAsync("This module is not activated, you are unable to use commands from it.");
            return;
        }

        AzuraCastEntity? azuraCast = await _db.GetAzuraCastAsync(context.Guild.Id);
        if (azuraCast is null)
        {
            _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
            return;
        }

        ContextCheckFailedData? azuraCastOnlineCheck = ex.Errors.FirstOrDefault(e => e.ContextCheckAttribute is AzuraCastOnlineCheckAttribute);
        if (azuraCastOnlineCheck is not null)
        {
            await context.EditResponseAsync($"The AzuraCast instance is currently offline!\nPlease contact <@&{azuraCast.InstanceAdminRoleId}>.");
            return;
        }

        ContextCheckFailedData? azuraCastDiscordPermCheck = ex.Errors.FirstOrDefault(e => e.ContextCheckAttribute is AzuraCastDiscordPermCheckAttribute);
        if (azuraCastDiscordPermCheck is not null)
        {
            string message = "You don't have the required permissions to execute this command!\nPlease contact {0}.";
            bool splittable = azuraCastDiscordPermCheck.ErrorMessage.Contains(':', StringComparison.OrdinalIgnoreCase);
            string[] info = azuraCastDiscordPermCheck.ErrorMessage.Split(':');

            if (!splittable && azuraCastDiscordPermCheck.ErrorMessage is "Instance")
            {
                message = message.Replace("{0}", $"<@&{azuraCast.InstanceAdminRoleId}>", StringComparison.OrdinalIgnoreCase);
            }
            else if (info.Length is 2)
            {
                AzuraCastStationEntity? station = await _db.GetAzuraCastStationAsync(context.Guild.Id, Convert.ToInt32(info[1], CultureInfo.InvariantCulture));
                if (station is null)
                {
                    _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, azuraCast.Id, Convert.ToInt32(info[1], CultureInfo.InvariantCulture));
                    return;
                }

                if (info[0] is "Admin")
                {
                    message = message.Replace("{0}", $"<@&{station.StationAdminRoleId}>", StringComparison.OrdinalIgnoreCase);
                }
                else if (info[0] is "DJ")
                {
                    message = message.Replace("{0}", $"<@&{station.StationDjRoleId}>", StringComparison.OrdinalIgnoreCase);
                }
            }

            await context.EditResponseAsync(message);
            return;
        }

        await AcknowledgeExceptionAsync(context);
    }

    public async Task<bool> SendMessageAsync(ulong channelId, string? content = null, IReadOnlyList<DiscordEmbed>? embeds = null, IReadOnlyList<string>? filePaths = null, IMention[]? mentions = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelId, nameof(channelId));

        if (!CheckIfClientIsConnected)
        {
            _logger.BotNotConnected();
            return false;
        }

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

        DiscordChannel? channel = await GetDiscordChannelAsync(channelId);
        DiscordMessage? message = null;
        if (channel is null)
        {
            _logger.UnableToSendMessage($"{nameof(channel)} is null");
        }
        else
        {
            DiscordMember? dMember = await GetDiscordMemberAsync(channel.Guild.Id, _client.CurrentUser.Id);
            if (dMember is null)
            {
                _logger.UnableToSendMessage($"Bot is not a member of server: {channel.Guild.Name} ({channel.Guild.Id})");
                return false;
            }

            if (!channel.PermissionsFor(dMember).HasPermission(DiscordPermissions.AccessChannels | DiscordPermissions.SendMessages))
            {
                _logger.UnableToSendMessage($"Bot has no permission to send messages in channel: {channel.Name} ({channel.Id})");
                return false;
            }

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
                FileOperations.DeleteFile(path);
            }
        }

        return message is not null;
    }

    private static async Task<DiscordMessage?> AcknowledgeExceptionAsync(SlashCommandContext ctx)
    {
        DiscordMember? member = ctx.Guild?.Owner;
        string errorMessage = "Ooops something went wrong!\n\nPlease inform the owner of this server.";
        if (member is not null)
            errorMessage = errorMessage.Replace("the owner of this server", member.Mention, StringComparison.OrdinalIgnoreCase);

        await using DiscordMessageBuilder builder = new()
        {
            Content = errorMessage
        };
        builder.WithAllowedMention(UserMention.All);

        switch (ctx.Interaction.ResponseState)
        {
            case DiscordInteractionResponseState.Unacknowledged:
                await ctx.RespondAsync(builder);
                return null;

            case DiscordInteractionResponseState.Deferred:
                return await ctx.EditResponseAsync(builder);

            case DiscordInteractionResponseState.Replied:
                return await ctx.FollowupAsync(builder);
        }

        return null;
    }

    private static void ProcessOptions(IReadOnlyDictionary<CommandParameter, object?> paramaters, Dictionary<string, string> commandParameters)
    {
        ArgumentNullException.ThrowIfNull(paramaters, nameof(paramaters));

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

    private static DiscordEmbedBuilder CreateExceptionEmbed(Exception ex, string timestamp, string? jsonMessage = null, DiscordMessage? message = null, DiscordUser? user = null, string? commandName = null, Dictionary<string, string>? commandOptions = null)
    {
        ArgumentNullException.ThrowIfNull(ex, nameof(ex));
        ArgumentNullException.ThrowIfNull(timestamp, nameof(timestamp));

        string os = AzzyStatsHardware.GetSystemOs;
        string arch = AzzyStatsHardware.GetSystemOsArch;
        string botName = AzzyStatsSoftware.GetBotName;
        string botVersion = AzzyStatsSoftware.GetBotVersion;

        DiscordEmbedBuilder builder = new()
        {
            Color = DiscordColor.Red
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
            StringBuilder stringBuilder = new();
            foreach (KeyValuePair<string, string> kvp in commandOptions)
            {
                stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"**{kvp.Key}**: {kvp.Value}");
            }

            builder.AddField("Options", stringBuilder.ToString());
        }

        builder.AddField("OS", os);
        builder.AddField("Arch", arch);
        builder.WithAuthor(botName, BugReportUrl);
        builder.WithFooter($"Version: {botVersion}");

        return builder;
    }
}
