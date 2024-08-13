using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using AzzyBot.Bot.Commands.Checks;
using AzzyBot.Bot.Settings;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services;

public sealed class DiscordBotService(ILogger<DiscordBotService> logger, AzzyBotSettingsRecord settings, DbActions dbActions, DiscordClient client)
{
    private readonly ILogger<DiscordBotService> _logger = logger;
    private readonly AzzyBotSettingsRecord _settings = settings;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordClient _client = client;
    private const string BugReportUrl = "https://github.com/Sella-GH/AzzyBot/issues/new?assignees=Sella-GH&labels=bug&projects=&template=bug_report.yml&title=%5BBUG%5D";
    private const string BugReportMessage = $"Send a [bug report]({BugReportUrl}) to help us fixing this issue!\nPlease include a screenshot of this exception embed and the attached StackTrace file.\nYour Contribution is very welcome.";
    private const string ErrorChannelNotConfigured = $"**If you're seeing this message then I am not configured correctly!**\nTell your server admin to run */config modify-core*\n\n{BugReportMessage}";

    public bool CheckIfClientIsConnected
    => _client.AllShardsConnected;

    public async Task<bool> CheckChannelPermissionsAsync(DiscordMember member, ulong channelId, DiscordPermissions permissions)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelId, nameof(channelId));

        DiscordChannel? channel = await GetDiscordChannelAsync(channelId);
        if (channel is null)
        {
            _logger.DiscordItemNotFound(nameof(DiscordChannel), channelId);
            return false;
        }

        return channel.PermissionsFor(member).HasPermission(permissions);
    }

    public async Task CheckPermissionsAsync(DiscordGuild guild, ulong[] channelIds)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));
        ArgumentNullException.ThrowIfNull(channelIds, nameof(channelIds));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelIds.Length, nameof(channelIds));

        DiscordMember? member = await GetDiscordMemberAsync(guild.Id);
        if (member is null)
        {
            _logger.DiscordItemNotFound(nameof(DiscordMember), guild.Id);
            return;
        }

        List<ulong> channels = new(channelIds.Length);
        List<ulong> channelNotAccessible = new(channelIds.Length);
        foreach (ulong channelId in channelIds)
        {
            channels.Add(channelId);
            if (!await CheckChannelPermissionsAsync(member, channelId, DiscordPermissions.AccessChannels | DiscordPermissions.SendMessages))
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

    public async Task CheckPermissionsAsync(IAsyncEnumerable<GuildEntity> guilds)
    {
        ArgumentNullException.ThrowIfNull(guilds, nameof(guilds));

        DiscordMember? member;
        List<ulong> channels = [];
        List<ulong> channelNotAccessible = [];
        await foreach (GuildEntity guild in guilds)
        {
            if (guild.UniqueId == _settings.ServerId)
            {
                channels.Add(_settings.ErrorChannelId);
                channels.Add(_settings.NotificationChannelId);
            }

            member = await GetDiscordMemberAsync(guild.UniqueId);
            if (member is null)
            {
                _logger.DiscordItemNotFound(nameof(DiscordMember), guild.UniqueId);
                continue;
            }

            if (guild.Preferences.AdminNotifyChannelId is not 0)
                channels.Add(guild.Preferences.AdminNotifyChannelId);

            if (guild.Preferences.ErrorChannelId is not 0)
                channels.Add(guild.Preferences.ErrorChannelId);

            if (guild.AzuraCast is not null)
            {
                if (guild.AzuraCast.Preferences.NotificationChannelId is not 0)
                    channels.Add(guild.AzuraCast.Preferences.NotificationChannelId);

                if (guild.AzuraCast.Preferences.OutagesChannelId is not 0)
                    channels.Add(guild.AzuraCast.Preferences.OutagesChannelId);

                foreach (AzuraCastStationEntity station in guild.AzuraCast.Stations)
                {
                    if (station.Preferences.FileUploadChannelId is not 0)
                        channels.Add(station.Preferences.FileUploadChannelId);

                    if (station.Preferences.RequestsChannelId is not 0)
                        channels.Add(station.Preferences.RequestsChannelId);
                }
            }

            foreach (ulong channelId in channels)
            {
                DiscordChannel? channel = await GetDiscordChannelAsync(channelId);
                if (channel is null)
                {
                    _logger.DiscordItemNotFound(nameof(DiscordChannel), channelId);
                    continue;
                }

                if (!channel.PermissionsFor(member).HasPermission(DiscordPermissions.AccessChannels | DiscordPermissions.SendMessages))
                    channelNotAccessible.Add(channelId);
            }

            if (channelNotAccessible.Count is 0)
            {
                channels.Clear();
                continue;
            }

            DiscordGuild? dGuild = GetDiscordGuild(guild.UniqueId);
            if (dGuild is null)
            {
                _logger.DiscordItemNotFound(nameof(DiscordGuild), guild.UniqueId);
                continue;
            }

            StringBuilder builder = new();
            builder.AppendLine(CultureInfo.InvariantCulture, $"I don't have the required permissions in server **{dGuild.Name}** to send messages in channel(s):");
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

            DiscordMember owner = await dGuild.GetGuildOwnerAsync();
            await owner.SendMessageAsync(builder.ToString());
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
        DiscordGuild? guild = GetDiscordGuild(guildId);
        DiscordMember? member = null;

        if (guild is not null)
            member = await guild.GetMemberAsync((userId is not 0) ? userId : _client.CurrentUser.Id);

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

    public async Task<bool> LogExceptionAsync(Exception ex, DateTime timestamp, SlashCommandContext? ctx = null, ulong guildId = 0, string? info = null)
    {
        ArgumentNullException.ThrowIfNull(ex, nameof(ex));

        _logger.ExceptionOccured(ex);

        string exMessage = ex.Message;
        string stackTrace = ex.StackTrace ?? string.Empty;
        string exInfo = (string.IsNullOrWhiteSpace(stackTrace)) ? exMessage : $"{exMessage}\n{stackTrace}";
        string timestampString = timestamp.ToString("yyyy-MM-dd_HH-mm-ss-fffffff", CultureInfo.InvariantCulture);
        ulong errorChannelId = _settings.ErrorChannelId;
        bool errorChannelConfigured = true;

        //
        // Checks if the guild is the main guild
        // If not look if the guild has an error channel set
        // Otherwise it will use the first channel it can see
        // However if nothing is present, send to debug server
        // If there's no guild, take the current channel
        //

        if (guildId != _settings.ServerId && guildId is not 0)
        {
            GuildPreferencesEntity? guildPrefs = await _dbActions.GetGuildPreferencesAsync(guildId);
            if (guildPrefs is null)
            {
                _logger.DatabaseGuildPreferencesNotFound(guildId);
                return false;
            }

            if (guildPrefs.ErrorChannelId is not 0)
                errorChannelId = guildPrefs.ErrorChannelId;

            if (errorChannelId == _settings.ErrorChannelId)
            {
                DiscordChannel? dChannel = await GetFirstDiscordChannelAsync(guildId);
                if (dChannel is null)
                {
                    _logger.DiscordItemNotFound(nameof(DiscordChannel), guildId);
                    return false;
                }

                errorChannelId = dChannel.Id;
                errorChannelConfigured = false;
            }
        }

        // Handle the special case when it's a command exception
        DiscordEmbed embed;
        if (ctx is not null)
        {
            DiscordMessage? discordMessage = await AcknowledgeExceptionAsync(ctx);
            DiscordUser discordUser = ctx.User;
            string commandName = ctx.Command.FullName;
            Dictionary<string, string> commandOptions = new(ctx.Command.Parameters.Count);
            ProcessOptions(ctx.Arguments, commandOptions);

            embed = CreateExceptionEmbed(ex, timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), info, discordMessage, discordUser, commandName, commandOptions);
        }
        else
        {
            embed = CreateExceptionEmbed(ex, timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), info);
        }

        try
        {
            string tempFilePath = await FileOperations.CreateTempFileAsync(exInfo, $"StackTrace_{timestampString}.log");

            bool messageSent = await SendMessageAsync(errorChannelId, (errorChannelConfigured) ? BugReportMessage : ErrorChannelNotConfigured, [embed], [tempFilePath]);
            if (!messageSent)
                _logger.UnableToSendMessage("Error message was not sent");

            FileOperations.DeleteFile(tempFilePath);

            return true;
        }
        catch (Exception e) when (e is IOException or SecurityException or UnauthorizedAccessException)
        {
            _logger.UnableToLogException(e.ToString());
        }

        return false;
    }

    public async Task RespondToChecksExceptionAsync(ChecksFailedException ex, SlashCommandContext context)
    {
        if (!CheckIfClientIsConnected)
        {
            _logger.BotNotConnected();
            return;
        }

        ArgumentNullException.ThrowIfNull(ex, nameof(ex));
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        await using DiscordMessageBuilder builder = new();
        builder.WithAllowedMention(RoleMention.All);

        ContextCheckFailedData? moduleActivatedCheck = ex.Errors.FirstOrDefault(static e => e.ContextCheckAttribute is ModuleActivatedCheckAttribute);
        if (moduleActivatedCheck is not null)
        {
            builder.WithContent("This module is not activated, you are unable to use commands from it.");
            await context.EditResponseAsync(builder);
            return;
        }

        AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id, loadPrefs: true, loadStations: true, loadStationPrefs: true);
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

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelId, nameof(channelId));

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

            if (!channel.PermissionsFor(dMember).HasPermission(DiscordPermissions.AccessChannels | DiscordPermissions.SendMessages))
            {
                _logger.UnableToSendMessage($"Bot has no permission to send messages in channel: {channel.Name} ({channel.Id})");
                return false;
            }

            await channel.SendMessageAsync(builder);
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

        return true;
    }

    public async Task SetBotStatusAsync(int status = 1, int type = 2, string doing = "Music", Uri? url = null, bool reset = false)
    {
        if (reset)
        {
            await _client.UpdateStatusAsync(new DiscordActivity("Music", DiscordActivityType.ListeningTo), DiscordUserStatus.Online);
            return;
        }

        DiscordActivityType activityType = (Enum.IsDefined(typeof(DiscordActivityType), type)) ? (DiscordActivityType)type : DiscordActivityType.ListeningTo;
        if (activityType is DiscordActivityType.Streaming && url is null)
            activityType = DiscordActivityType.Playing;

        DiscordActivity activity = new(doing, activityType);
        if (activityType is DiscordActivityType.Streaming && url is not null && (url.Host.Contains("twitch", StringComparison.OrdinalIgnoreCase) || url.Host.Contains("youtube", StringComparison.OrdinalIgnoreCase)))
            activity.StreamUrl = url.OriginalString;

        DiscordUserStatus userStatus = (Enum.IsDefined(typeof(DiscordUserStatus), status)) ? (DiscordUserStatus)status : DiscordUserStatus.Online;

        await _client.UpdateStatusAsync(activity, userStatus);
    }

    private async Task<DiscordMessage?> AcknowledgeExceptionAsync(SlashCommandContext ctx)
    {
        DiscordGuild? guild = ctx.Guild;
        DiscordMember? owner = null;
        if (guild is null)
        {
            _logger.DiscordItemNotFound(nameof(DiscordGuild), 0);
        }
        else
        {
            owner = await guild.GetGuildOwnerAsync();
        }

        string errorMessage = "Ooops something went wrong!\n\nPlease inform the owner of this server.";
        if (owner is not null)
            errorMessage = errorMessage.Replace("the owner of this server", owner.Mention, StringComparison.OrdinalIgnoreCase);

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

            default:
                throw new InvalidOperationException("Unknown response state");
        }
    }

    private async Task<DiscordChannel?> GetFirstDiscordChannelAsync(ulong guildId)
    {
        DiscordGuild? guild = GetDiscordGuild(guildId);
        DiscordMember? member = await GetDiscordMemberAsync(guildId);

        if (guild is null)
        {
            _logger.DiscordItemNotFound(nameof(DiscordGuild), guildId);
            return null;
        }

        if (member is null)
        {
            _logger.DiscordItemNotFound(nameof(DiscordMember), _client.CurrentUser.Id);
            return null;
        }

        return guild.Channels.FirstOrDefault(c => c.Value.Type is DiscordChannelType.Text && c.Value.PermissionsFor(member).HasPermission(DiscordPermissions.AccessChannels | DiscordPermissions.SendMessages)).Value;
    }

    private static void ProcessOptions(IReadOnlyDictionary<CommandParameter, object?> paramaters, Dictionary<string, string> commandParameters)
    {
        ArgumentNullException.ThrowIfNull(paramaters, nameof(paramaters));

        foreach (KeyValuePair<CommandParameter, object?> kvp in paramaters)
        {
            string name = kvp.Key.Name;
            string value = kvp.Value?.ToString() ?? "undefined";

            if (!string.IsNullOrWhiteSpace(name) && value is not "0" && value is not "undefined")
                commandParameters.Add(name, value);
        }
    }

    private static DiscordEmbedBuilder CreateExceptionEmbed(Exception ex, string timestamp, string? jsonMessage = null, DiscordMessage? message = null, DiscordUser? user = null, string? commandName = null, Dictionary<string, string>? commandOptions = null)
    {
        ArgumentNullException.ThrowIfNull(ex, nameof(ex));
        ArgumentNullException.ThrowIfNull(timestamp, nameof(timestamp));

        string os = HardwareStats.GetSystemOs;
        string arch = HardwareStats.GetSystemOsArch;
        string botName = SoftwareStats.GetAppName;
        string botVersion = SoftwareStats.GetAppVersion;

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
