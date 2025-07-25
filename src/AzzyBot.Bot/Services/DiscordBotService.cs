﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using AzzyBot.Bot.Commands.Checks;
using AzzyBot.Bot.Resources;
using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Helpers;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Helpers;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using DSharpPlus;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Services;

public sealed class DiscordBotService(ILogger<DiscordBotService> logger, IOptions<AzzyBotSettings> settings, DbActions dbActions, DiscordClient client)
{
    private readonly ILogger<DiscordBotService> _logger = logger;
    private readonly AzzyBotSettings _settings = settings.Value;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordClient _client = client;

    private bool CheckIfClientIsConnected
        => _client.AllShardsConnected;

    public async Task<bool> CheckChannelPermissionsAsync(DiscordMember member, ulong channelId, DiscordPermissions permissions)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelId);

        DiscordChannel? channel = await GetDiscordChannelAsync(channelId);
        if (channel is null)
        {
            _logger.DiscordItemNotFound(nameof(DiscordChannel), channelId);
            return false;
        }

        return channel.PermissionsFor(member).HasAllPermissions(permissions);
    }

    public async Task CheckPermissionsAsync(DiscordGuild guild, ulong[] channelIds)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(channelIds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelIds.Length);

        DiscordMember? member = await GetDiscordMemberAsync(guild.Id);
        if (member is null)
        {
            _logger.DiscordItemNotFound(nameof(DiscordMember), guild.Id);
            return;
        }

        await CheckPermissionsCoreAsync(guild, member, channelIds);
    }

    public async Task CheckPermissionsAsync(GuildEntity guildEntity)
    {
        ArgumentNullException.ThrowIfNull(guildEntity);

        DiscordGuild? guild = GetDiscordGuild(guildEntity.UniqueId);
        if (guild is null)
        {
            _logger.DatabaseGuildNotFound(guildEntity.UniqueId);
            return;
        }

        DiscordMember? member = await GetDiscordMemberAsync(guild.Id);
        if (member is null)
        {
            _logger.DiscordItemNotFound(nameof(DiscordMember), guild.Id);
            return;
        }

        List<ulong> channels = [];
        if (guildEntity.UniqueId == _settings.ServerId)
        {
            channels.Add(_settings.ErrorChannelId);
            channels.Add(_settings.NotificationChannelId);
        }

        if (guildEntity.Preferences.AdminNotifyChannelId is not 0)
            channels.Add(guildEntity.Preferences.AdminNotifyChannelId);

        if (guildEntity.AzuraCast is not null)
        {
            if (guildEntity.AzuraCast.Preferences.NotificationChannelId is not 0)
                channels.Add(guildEntity.AzuraCast.Preferences.NotificationChannelId);

            if (guildEntity.AzuraCast.Preferences.OutagesChannelId is not 0)
                channels.Add(guildEntity.AzuraCast.Preferences.OutagesChannelId);

            foreach (AzuraCastStationPreferencesEntity station in guildEntity.AzuraCast.Stations.Select(s => s.Preferences))
            {
                if (station.FileUploadChannelId is not 0)
                    channels.Add(station.FileUploadChannelId);

                if (station.RequestsChannelId is not 0)
                    channels.Add(station.RequestsChannelId);
            }
        }

        await _dbActions.UpdateGuildAsync(guildEntity.UniqueId, true);

        await CheckPermissionsCoreAsync(guild, member, channels);
    }

    public async Task CheckPermissionsAsync(IReadOnlyList<GuildEntity> guilds)
    {
        ArgumentNullException.ThrowIfNull(guilds);

        foreach (GuildEntity guild in guilds)
        {
            await CheckPermissionsAsync(guild);
        }
    }

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

        return GetDiscordGuilds.Select(static g => g.Value).FirstOrDefault(g => g.Id == guildId);
    }

    public IReadOnlyDictionary<ulong, DiscordGuild> GetDiscordGuilds
        => _client.Guilds;

    public async Task<DiscordMember?> GetDiscordMemberAsync(ulong guildId, ulong userId = 0)
    {
        DiscordGuild? guild = await _client.GetGuildsAsync().FirstOrDefaultAsync(g => g.Id == guildId);
        DiscordMember? member = null;

        if (guild is not null)
            member = await guild.GetMemberAsync((userId is not 0) ? userId : _client.CurrentUser.Id);

        return member;
    }

    public async Task LogExceptionAsync(Exception ex, DateTimeOffset timestamp, SlashCommandContext? ctx = null, string? info = null)
    {
        ArgumentNullException.ThrowIfNull(ex);

        _logger.ExceptionOccurred(ex);

        // Handle the special case when it's a command exception
        string timestampString = timestamp.ToString("yyyy-MM-dd HH:mm:ss:fffffff", CultureInfo.InvariantCulture);
        DiscordEmbed embed;
        if (ctx is not null)
        {
            DiscordMessage? discordMessage = await AcknowledgeExceptionAsync(ctx);
            string? message = discordMessage?.JumpLink.ToString();
            string guild = $"{ctx.Guild?.Name} ({ctx.Guild?.Id})";
            string discordUser = $"{ctx.User.GlobalName} ({ctx.User.Id})";
            string commandName = ctx.Command.FullName;
            Dictionary<string, string> commandOptions = new(ctx.Command.Parameters.Count);
            ProcessOptions(ctx.Arguments, commandOptions);

            embed = CreateExceptionEmbed(ex, timestampString, info, guild, message, discordUser, commandName, commandOptions);
        }
        else
        {
            embed = CreateExceptionEmbed(ex, timestampString, info);
        }

        try
        {
            string jsonDump = JsonSerializer.Serialize(new(ex, info), JsonSerializationSourceGen.Default.SerializableExceptionsRecord);
            timestampString = timestampString.Replace(" ", "_", StringComparison.OrdinalIgnoreCase).Replace(":", "-", StringComparison.OrdinalIgnoreCase);
            string fileName = $"AzzyBotException_{timestampString}.json";
            string tempFilePath = await FileOperations.CreateTempFileAsync(jsonDump, fileName);

            bool messageSent = await SendMessageAsync(_settings.ErrorChannelId, embeds: [embed], filePaths: [tempFilePath]);
            if (!messageSent)
                _logger.UnableToSendMessage("Error message was not sent");

            FileOperations.DeleteFile(tempFilePath);
        }
        catch (Exception e) when (e is IOException or NotSupportedException or SecurityException or UnauthorizedAccessException)
        {
            _logger.UnableToLogException(e.ToString());
        }
    }

    public async Task RespondToChecksExceptionAsync(ChecksFailedException ex, SlashCommandContext context)
    {
        if (!CheckIfClientIsConnected)
        {
            _logger.BotNotConnected();
            return;
        }

        ArgumentNullException.ThrowIfNull(ex);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Guild);

        await using DiscordMessageBuilder builder = new();
        builder.WithAllowedMention(RoleMention.All);

        ContextCheckFailedData? moduleActivatedCheck = ex.Errors.FirstOrDefault(static e => e.ContextCheckAttribute is ModuleActivatedCheckAttribute);
        if (moduleActivatedCheck is not null)
        {
            string reply = string.Empty;
            switch (moduleActivatedCheck.ErrorMessage)
            {
                case CheckMessages.AzuraCastIsNull:
                    reply = "The AzuraCast module is not activated, you are unable to use commands from it.";
                    break;

                case CheckMessages.GuildIsNull:
                    reply = "Your server is not registered within the bot.";
                    break;

                case CheckMessages.LegalsNotAccepted:
                    reply = GeneralStrings.LegalsNotAccepted;
                    break;

                case CheckMessages.ModuleNotFound:
                    reply = "The requested module was not found, what have you done?";
                    break;
            }

            builder.WithContent(reply);
            await context.EditResponseAsync(builder);
            return;
        }

        AzuraCastEntity? azuraCast = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadPrefs: true, loadStations: true, loadStationPrefs: true);
        if (azuraCast is null)
        {
            _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
            return;
        }

        ContextCheckFailedData? azuraCastOnlineCheck = ex.Errors.FirstOrDefault(static e => e.ContextCheckAttribute is AzuraCastOnlineCheckAttribute);
        if (azuraCastOnlineCheck is not null)
        {
            builder.WithContent($"The AzuraCast instance is currently offline!\nPlease contact <@&{azuraCast.Preferences.InstanceAdminRoleId}>.");
            await context.EditResponseAsync(builder);
            return;
        }

        ContextCheckFailedData? azuraCastDiscordPermCheck = ex.Errors.FirstOrDefault(static e => e.ContextCheckAttribute is AzuraCastDiscordPermCheckAttribute);
        if (azuraCastDiscordPermCheck is not null)
        {
            string message = "You don't have the required permissions to execute this command!\nPlease contact {0}.";
            string[] info = azuraCastDiscordPermCheck.ErrorMessage.Split(':');

            if (info.Length is 0 && azuraCastDiscordPermCheck.ErrorMessage is "Instance")
            {
                message = message.Replace("{0}", $"<@&{azuraCast.Preferences.InstanceAdminRoleId}>", StringComparison.OrdinalIgnoreCase);
            }
            else if (info.Length is 2)
            {
                AzuraCastStationEntity? station = azuraCast.Stations.FirstOrDefault(s => s.StationId == Convert.ToInt32(info[1], CultureInfo.InvariantCulture));
                if (station is null)
                {
                    _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, azuraCast.Id, Convert.ToInt32(info[1], CultureInfo.InvariantCulture));
                    return;
                }

                if (info[0] is "Station")
                {
                    message = message.Replace("{0}", $"<@&{station.Preferences.StationAdminRoleId}>", StringComparison.OrdinalIgnoreCase);
                }
                else if (info[0] is "DJ")
                {
                    message = message.Replace("{0}", $"<@&{((station.Preferences.StationDjRoleId is 0) ? station.Preferences.StationAdminRoleId : station.Preferences.StationDjRoleId)}>", StringComparison.OrdinalIgnoreCase);
                }
            }

            builder.WithContent(message);
            await context.EditResponseAsync(builder);
            return;
        }

        ContextCheckFailedData? azuraCastFeatureCheck = ex.Errors.FirstOrDefault(static e => e.ContextCheckAttribute is FeatureAvailableCheckAttribute);
        if (azuraCastFeatureCheck is not null)
        {
            builder.WithContent($"This feature is not activated on this station! Please inform <@&{azuraCastFeatureCheck.ErrorMessage}>.");
            await context.EditResponseAsync(builder);
            return;
        }

        ContextCheckFailedData? azuraCastDiscordChannelCheck = ex.Errors.FirstOrDefault(static e => e.ContextCheckAttribute is AzuraCastDiscordChannelCheckAttribute);
        if (azuraCastDiscordChannelCheck is not null)
        {
            if (ulong.TryParse(azuraCastDiscordChannelCheck.ErrorMessage, out ulong channelId) && channelId is not 0)
            {
                builder.WithContent($"This command is only usable in: <#{channelId}>");
                await context.EditResponseAsync(builder);
                return;
            }

            builder.WithContent("This command is unable to use in this channel!");
            await context.EditResponseAsync(builder);
            return;
        }

        await AcknowledgeExceptionAsync(context);
    }

    public async Task<bool> SendMessageAsync(ulong channelId, string? content = null, IReadOnlyList<DiscordEmbed>? embeds = null, IReadOnlyList<string>? filePaths = null, IMention[]? mentions = null)
    {
        if (!CheckIfClientIsConnected)
        {
            _logger.BotNotConnected();
            return false;
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelId);

        await using DiscordMessageBuilder builder = new();

        if (!string.IsNullOrWhiteSpace(content))
            builder.WithContent(content);

        if (embeds?.Count > 0 && embeds.Count <= 10)
            builder.AddEmbeds(embeds);

        if (mentions is not null)
            builder.WithAllowedMentions(mentions);

        List<FileStream> streams = new(10);
        if (filePaths?.Count > 0 && filePaths.Count <= 10)
        {
            const int maxFileSize = FileSizes.DiscordFileSize;
            long allFileSize = 0;

            foreach (string path in filePaths)
            {
                FileInfo fileInfo = new(path);
                if (fileInfo.Length > maxFileSize || allFileSize > maxFileSize)
                    break;

                allFileSize += fileInfo.Length;

                FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.None);
                streams.Add(stream);
                builder.AddFile(Path.GetFileName(path), stream);
            }
        }

        DiscordChannel? channel = await GetDiscordChannelAsync(channelId);
        if (channel is null)
        {
            _logger.UnableToSendMessage($"{nameof(channel)} is null");
        }
        else
        {
            DiscordMember? dMember = await GetDiscordMemberAsync(channel.Guild.Id);
            if (dMember is null)
            {
                _logger.UnableToSendMessage($"Bot is not a member of server: {channel.Guild.Name} ({channel.Guild.Id})");
                return false;
            }

            if (!channel.PermissionsFor(dMember).HasAllPermissions([DiscordPermission.SendMessages, DiscordPermission.ViewChannel]))
            {
                _logger.UnableToSendMessage($"Bot has no permission to send messages in channel: {channel.Name} ({channel.Id})");
                return false;
            }

            await channel.SendMessageAsync(builder);
        }

        if (streams.Count > 0)
        {
            foreach (FileStream stream in streams)
            {
                await stream.DisposeAsync();
            }

            foreach (string path in filePaths!)
            {
                FileOperations.DeleteFile(path);
            }
        }

        return true;
    }

    public async Task SetBotStatusAsync(int status, int type, string doing, Uri? url = null, bool reset = false)
    {
        if (reset)
        {
            await _client.UpdateStatusAsync(new DiscordActivity("Music", DiscordActivityType.ListeningTo), DiscordUserStatus.Online);
            return;
        }

        DiscordActivity activity = SetBotStatusActivity(type, doing, url);
        DiscordUserStatus newStatus = SetBotStatusUserStatus(status);

        await _client.UpdateStatusAsync(activity, newStatus);
    }

    public static DiscordActivity SetBotStatusActivity(int type, string doing, Uri? url)
    {
        DiscordActivityType activityType = (Enum.IsDefined(typeof(DiscordActivityType), type)) ? (DiscordActivityType)type : DiscordActivityType.ListeningTo;
        if (activityType is DiscordActivityType.Streaming && url is null)
            activityType = DiscordActivityType.Playing;

        DiscordActivity activity = new(doing, activityType);
        if (activityType is DiscordActivityType.Streaming && url is not null && (url.Host.Contains("twitch", StringComparison.OrdinalIgnoreCase) || url.Host.Contains("youtube", StringComparison.OrdinalIgnoreCase)))
            activity.StreamUrl = url.OriginalString;

        return activity;
    }

    public static DiscordUserStatus SetBotStatusUserStatus(int status)
        => (Enum.IsDefined(typeof(DiscordUserStatus), status)) ? (DiscordUserStatus)status : DiscordUserStatus.Online;

    private static async Task<DiscordMessage?> AcknowledgeExceptionAsync(SlashCommandContext ctx)
    {
        await using DiscordMessageBuilder builder = new()
        {
            Content = $"An unexpected error occurred. Our team has been notified and is working on a fix.\nJoin our support server for more information: {UriStrings.DiscordSupportServer}"
        };

        switch (ctx.Interaction.ResponseState)
        {
            case DiscordInteractionResponseState.Unacknowledged:
                await ctx.RespondAsync(builder);
                return null;

            case DiscordInteractionResponseState.Deferred:
                return await ctx.EditResponseAsync(builder);

            case DiscordInteractionResponseState.Replied:
                return await ctx.FollowupAsync(builder);

            default:
                throw new InvalidOperationException("Unknown response state");
        }
    }

    private static void ProcessOptions(IReadOnlyDictionary<CommandParameter, object?> parameters, Dictionary<string, string> commandParameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        foreach (KeyValuePair<CommandParameter, object?> kvp in parameters)
        {
            string name = kvp.Key.Name;
            string value = kvp.Value?.ToString() ?? "undefined";

            if (!string.IsNullOrEmpty(name) && value is not "0" or "undefined")
                commandParameters.Add(name, value);
        }
    }

    private async Task CheckPermissionsCoreAsync(DiscordGuild guild, DiscordMember member, IEnumerable<ulong> channelIds)
    {
        List<ulong> channelNotAccessible = [];
        foreach (ulong channelId in channelIds)
        {
            DiscordChannel? channel = await GetDiscordChannelAsync(channelId);
            if (channel is null)
            {
                _logger.DiscordItemNotFound(nameof(DiscordChannel), channelId);
                continue;
            }

            if (!channel.PermissionsFor(member).HasAllPermissions([DiscordPermission.SendMessages, DiscordPermission.ViewChannel]))
                channelNotAccessible.Add(channelId);
        }

        if (channelNotAccessible.Count is 0)
            return;

        StringBuilder builder = new();
        builder.AppendLine(CultureInfo.InvariantCulture, $"I don't have the required permissions in server **{guild.Name}** to send messages in channel(s):");
        foreach (ulong channelId in channelNotAccessible)
        {
            DiscordChannel? dChannel = await GetDiscordChannelAsync(channelId);
            if (dChannel is null)
            {
                _logger.DiscordItemNotFound(nameof(DiscordChannel), channelId);
                continue;
            }

            builder.AppendLine(CultureInfo.InvariantCulture, $"- {dChannel.Mention}");
        }

        builder.AppendLine("Please review your permission set.");

        DiscordMember owner = await guild.GetGuildOwnerAsync();
        await owner.SendMessageAsync(builder.ToString());
    }

    private DiscordEmbedBuilder CreateExceptionEmbed(Exception ex, string timestamp, string? jsonMessage = null, string? guild = null, string? message = null, string? userMention = null, string? commandName = null, Dictionary<string, string>? commandOptions = null)
    {
        ArgumentNullException.ThrowIfNull(ex);
        ArgumentNullException.ThrowIfNull(timestamp);

        string os = HardwareStats.GetSystemOs;
        string arch = HardwareStats.GetSystemOsArch;
        string botName = SoftwareStats.GetAppName;
        string botVersion = SoftwareStats.GetAppVersion;
        string botIconUrl = _client.CurrentUser.AvatarUrl;

        DiscordEmbedBuilder builder = new()
        {
            Title = ex.GetType().Name,
            Description = (ex.Message.Length <= 4096) ? ex.Message : "Description too big for embed.",
            Color = DiscordColor.Red
        };

        if (!string.IsNullOrEmpty(jsonMessage))
            builder.AddField("Advanced Error", jsonMessage);

        builder.AddField("Timestamp", timestamp);

        if (!string.IsNullOrEmpty(ex.Source))
            builder.AddField("Source", ex.Source);

        if (guild is not null)
            builder.AddField("Guild", guild);

        if (message is not null)
            builder.AddField("Message", message);

        if (userMention is not null)
            builder.AddField("User", userMention);

        if (!string.IsNullOrEmpty(commandName))
            builder.AddField("Command", commandName);

        if (commandOptions?.Count > 0)
        {
            StringBuilder sb = new();
            foreach (KeyValuePair<string, string> kvp in commandOptions)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"**{kvp.Key}**: {kvp.Value}");
            }

            builder.AddField("Options", sb.ToString());
        }

        builder.AddField("OS", os);
        builder.AddField("Arch", arch);
        builder.WithAuthor(botName, UriStrings.BugReportUri, botIconUrl);
#if DEBUG || DOCKER_DEBUG
        builder.WithFooter($"Version: {botVersion} / {Environments.Development.ToUpperInvariant()}");
#else
        builder.WithFooter($"Version: {botVersion} / {Environments.Production.ToUpperInvariant()}");
#endif

        return builder;
    }
}
