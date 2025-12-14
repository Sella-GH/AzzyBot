using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AzzyBot.Bot.Commands.Autocompletes;
using AzzyBot.Bot.Commands.Choices;
using AzzyBot.Bot.Localization;
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Helpers;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands.Localization;
using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class AdminCommands
{
    [Command("admin"), RequireGuild, RequireApplicationOwner, RequirePermissions(botPermissions: [], userPermissions: [DiscordPermission.Administrator]), InteractionLocalizer<CommandLocalizer>]
    public sealed class AdminGroup(ILogger<AdminGroup> logger, IOptions<AzzyBotSettings> settings, DbActions dbActions, DiscordBotService botService)
    {
        private readonly ILogger<AdminGroup> _logger = logger;
        private readonly AzzyBotSettings _settings = settings.Value;
        private readonly DbActions _dbActions = dbActions;
        private readonly DiscordBotService _botService = botService;

        [Command("change-bot-status"), Description("Change the global bot status according to your likes."), InteractionLocalizer<CommandLocalizer>]
        public async ValueTask ChangeStatusAsync
        (
            SlashCommandContext context,
            [Description("Choose the activity type which the bot should have."), SlashChoiceProvider<BotActivityProvider>, InteractionLocalizer<CommandLocalizer>] int activity,
            [Description("Choose the status type which the bot should have."), SlashChoiceProvider<BotStatusProvider>, InteractionLocalizer<CommandLocalizer>] int status,
            [Description("Enter a custom doing which is added after the activity type."), MinMaxLength(0, 128), InteractionLocalizer<CommandLocalizer>] string doing,
            [Description("Enter a custom url. Only usable when having activity type streaming or watching!"), InteractionLocalizer<CommandLocalizer>] Uri? url = null,
            [Description("Reset the bot status to default."), SlashChoiceProvider<BooleanYesNoStateProvider>, InteractionLocalizer<CommandLocalizer>] int reset = 0
        )
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(ChangeStatusAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            await _botService.SetBotStatusAsync(status, activity, doing, url, reset is 1);

            if (reset is 1)
            {
                await context.EditResponseAsync(GeneralStrings.BotStatusReset);
            }
            else
            {
                await context.EditResponseAsync(GeneralStrings.BotStatusChanged);
            }
        }

        [Command("get-joined-server"), Description("Displays all servers the bot is in."), InteractionLocalizer<CommandLocalizer>]
        public async ValueTask GetJoinedGuildsAsync
        (
            SlashCommandContext context,
            [Description("Select the server you want to get more information about."), SlashAutoCompleteProvider<GuildsAutocomplete>, InteractionLocalizer<CommandLocalizer>] string? serverId = null
        )
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(GetJoinedGuildsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            IReadOnlyDictionary<ulong, DiscordGuild> guilds = _botService.GetDiscordGuilds;
            if (guilds.Count is 0)
            {
                await context.EditResponseAsync(GeneralStrings.NoGuildFound);
                return;
            }

            if (string.IsNullOrWhiteSpace(serverId))
            {
                // If no server id is provided, show all servers the bot is in.
                const string tooManyServers = "... and more!";
                StringBuilder stringBuilder = new();
                stringBuilder.AppendLine("I am in the following servers:");
                foreach (DiscordGuild guild in guilds.Values)
                {
                    if (stringBuilder.Length + tooManyServers.Length > 2000)
                    {
                        stringBuilder.AppendLine(tooManyServers);
                        break;
                    }

                    stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"- {guild.Name} ({guild.Id})");
                }

                await context.EditResponseAsync(stringBuilder.ToString());
            }
            else if (!ulong.TryParse(serverId, out ulong guildIdValue))
            {
                // If an invalid server id is provided error out
                await context.EditResponseAsync(GeneralStrings.GuildIdInvalid);
            }
            else if (guildIdValue is not 0)
            {
                // If a valid server id is provided, show detailed information about that server
                if (!guilds.TryGetValue(guildIdValue, out DiscordGuild? guild))
                {
                    _logger.DiscordItemNotFound(nameof(DiscordGuild), guildIdValue);
                    await context.EditResponseAsync(GeneralStrings.GuildNotFound);
                    return;
                }

                DiscordEmbed embed = await EmbedBuilder.BuildGuildAddedEmbedAsync(guild, true);
                await context.EditResponseAsync(embed);
            }
        }

        [Command("remove-joined-server"), Description("Removes the bot from a server."), InteractionLocalizer<CommandLocalizer>]
        public async ValueTask RemoveJoinedGuildAsync
        (
            SlashCommandContext context,
            [Description("Select the server you want to remove."), SlashAutoCompleteProvider<GuildsAutocomplete>, InteractionLocalizer<CommandLocalizer>] string serverId
        )
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(RemoveJoinedGuildAsync), context.User.GlobalName);

            if (!ulong.TryParse(serverId, out ulong guildIdValue))
            {
                await context.RespondAsync(GeneralStrings.GuildIdInvalid, true);
                return;
            }

            await context.DeferResponseAsync();

            DiscordGuild? guild = _botService.GetDiscordGuild(guildIdValue);
            if (guild is null)
            {
                _logger.DiscordItemNotFound(nameof(DiscordGuild), guildIdValue);
                await context.EditResponseAsync(GeneralStrings.GuildNotFound);
                return;
            }

            if (guild.Id == _settings.ServerId)
            {
                await context.EditResponseAsync(GeneralStrings.CanNotLeaveServer);
                return;
            }

            await guild.LeaveAsync();

            await context.EditResponseAsync($"I left **{guild.Name}** ({guild.Id}).");
        }

        [Command("reset-legals"), Description("Resets the legals for all guilds where the bot is in.")]
        public async ValueTask ResetLegalsAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(ResetLegalsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            await _dbActions.UpdateGuildsLegalsAsync();

            IReadOnlyDictionary<ulong, DiscordGuild> guilds = _botService.GetDiscordGuilds;
            if (guilds.Count is 0)
            {
                await context.EditResponseAsync(GeneralStrings.NoGuildFound);
                return;
            }

            await SendMessageAsync(guilds, GeneralStrings.LegalsReset);

            await context.EditResponseAsync(GeneralStrings.MessageSentToAll);
        }

        [Command("send-bot-wide-message"), Description("Sends a message to all servers the bot is in."), InteractionLocalizer<CommandLocalizer>]
        public async ValueTask SendBotWideMessageAsync
        (
            SlashCommandContext context,
            [Description("The message you want to send."), MinMaxLength(1, 2000), InteractionLocalizer<CommandLocalizer>] string message
        )
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(SendBotWideMessageAsync), context.User.GlobalName);

            if (string.IsNullOrWhiteSpace(message))
            {
                await context.RespondAsync(GeneralStrings.AdminBotWideMessageEmpty);
                return;
            }

            await context.DeferResponseAsync();

            IReadOnlyDictionary<ulong, DiscordGuild> guilds = _botService.GetDiscordGuilds;
            if (guilds.Count is 0)
            {
                await context.EditResponseAsync(GeneralStrings.NoGuildFound);
                return;
            }

            await SendMessageAsync(guilds, message);

            await context.EditResponseAsync(GeneralStrings.MessageSentToAll);
        }

        [Command("view-logs"), Description("View the logs of the bot."), InteractionLocalizer<CommandLocalizer>]
        public async ValueTask ViewLogsAsync
        (
            SlashCommandContext context,
            [Description("The log file you want to read."), SlashAutoCompleteProvider<AzzyViewLogsAutocomplete>, InteractionLocalizer<CommandLocalizer>] string? logfile = null
        )
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(ViewLogsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            string dateTime;
            if (!string.IsNullOrWhiteSpace(logfile))
            {
                dateTime = Path.GetFileNameWithoutExtension(logfile).Split("_")[1];
            }
            else
            {
                dateTime = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                logfile = FileOperations.GetFilesInDirectory("Logs", true).First();
            }

            await using FileStream fileStream = new(logfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            await using DiscordMessageBuilder builder = new();
            builder.WithContent($"Here are the logs from **{dateTime}**.");
            builder.AddFile($"{dateTime}.log", fileStream);
            await context.EditResponseAsync(builder);
        }

        private async ValueTask SendMessageAsync(IReadOnlyDictionary<ulong, DiscordGuild> guilds, string message)
        {
            const string dmAddition = "\n\nYou receive this message directly because you haven't provided a notification channel in your server.";
            string newMessage = message.Replace("\\n", Environment.NewLine, StringComparison.OrdinalIgnoreCase);
            IReadOnlyList<GuildEntity> dbGuilds = await _dbActions.ReadGuildsAsync(loadGuildPrefs: true);
            foreach (DiscordGuild guild in guilds.Values)
            {
                GuildEntity? guildEntity = dbGuilds.FirstOrDefault(e => e.UniqueId == guild.Id);
                if (guildEntity is null)
                {
                    _logger.DatabaseGuildNotFound(guild.Id);
                    continue;
                }

                // If there is a notification channel set we use that
                // Otherwise we send a DM to the owner
                if (guildEntity.ConfigSet && guildEntity.Preferences.AdminNotifyChannelId is not 0)
                {
                    await _botService.SendMessageAsync(guildEntity.Preferences.AdminNotifyChannelId, newMessage);
                }
                else
                {
                    DiscordMember owner = await guild.GetGuildOwnerAsync();
                    string ownerMessage = newMessage;
                    if (newMessage.Length < 2000 - dmAddition.Length)
                        ownerMessage += dmAddition;

                    await owner.SendMessageAsync(ownerMessage);
                }
            }

            if (_settings.BotAnnouncementChannelId is not 0)
            {
                await _botService.SendMessageAsync(_settings.BotAnnouncementChannelId, newMessage);
                _logger.BotWideMessageSent(guilds.Count + 1);
            }
            else
            {
                _logger.BotWideMessageSent(guilds.Count);
            }
        }
    }
}
