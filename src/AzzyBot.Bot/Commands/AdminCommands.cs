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
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Helpers;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class AdminCommands
{
    [Command("admin"), RequireGuild, RequireApplicationOwner, RequirePermissions(DiscordPermissions.None, DiscordPermissions.Administrator)]
    public sealed class AdminGroup(ILogger<AdminGroup> logger, AzzyBotSettingsRecord settings, DbActions dbActions, DiscordBotService botService)
    {
        private readonly ILogger<AdminGroup> _logger = logger;
        private readonly AzzyBotSettingsRecord _settings = settings;
        private readonly DbActions _dbActions = dbActions;
        private readonly DiscordBotService _botService = botService;

        [Command("change-bot-status"), Description("Change the global bot status according to your likes.")]
        public async ValueTask ChangeStatusAsync
        (
            SlashCommandContext context,
            [Description("Choose the activity type which the bot should have."), SlashChoiceProvider<BotActivityProvider>] int activity,
            [Description("Choose the status type which the bot should have."), SlashChoiceProvider<BotStatusProvider>] int status,
            [Description("Enter a custom doing which is added after the activity type."), MinMaxLength(0, 128)] string doing,
            [Description("Enter a custom url. Only usable when having activity type streaming or watching!")] Uri? url = null,
            [Description("Reset the bot status to default."), SlashChoiceProvider<BooleanYesNoStateProvider>] int reset = 0
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

        [Command("get-joined-server"), Description("Displays all servers the bot is in.")]
        public async ValueTask GetJoinedGuildsAsync
        (
            SlashCommandContext context,
            [Description("Select the server you want to get more information about."), SlashAutoCompleteProvider<GuildsAutocomplete>] string? serverId = null
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

            if (!string.IsNullOrWhiteSpace(serverId))
            {
                if (!ulong.TryParse(serverId, out ulong guildIdValue))
                {
                    await context.EditResponseAsync(GeneralStrings.GuildIdInvalid);
                    return;
                }

                if (!guilds.TryGetValue(guildIdValue, out DiscordGuild? guild))
                {
                    _logger.DiscordItemNotFound(nameof(DiscordGuild), guildIdValue);
                    await context.EditResponseAsync(GeneralStrings.GuildNotFound);
                    return;
                }

                DiscordEmbed embed = await EmbedBuilder.BuildGuildAddedEmbedAsync(guild, true);
                await context.EditResponseAsync(embed);

                return;
            }

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

        [Command("remove-joined-server"), Description("Removes the bot from a server.")]
        public async ValueTask RemoveJoinedGuildAsync
        (
            SlashCommandContext context,
            [Description("Select the server you want to remove."), SlashAutoCompleteProvider<GuildsAutocomplete>] string serverId
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

        [Command("send-bot-wide-message"), Description("Sends a message to all servers the bot is in.")]
        public async ValueTask SendBotWideMessageAsync
        (
            SlashCommandContext context,
            [Description("The message you want to send."), MinMaxLength(1, 2000)] string message
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

            string newMessage = message.Replace("\\n", Environment.NewLine, StringComparison.OrdinalIgnoreCase);
            IAsyncEnumerable<GuildEntity> guildsEntities = _dbActions.GetGuildsAsync(true);
            foreach (DiscordGuild guild in guilds.Values)
            {
                GuildEntity? guildEntity = await guildsEntities.Where(e => e.UniqueId == guild.Id).FirstOrDefaultAsync();
                if (guildEntity is null)
                {
                    _logger.DatabaseGuildNotFound(guild.Id);
                    continue;
                }

                if (guildEntity.ConfigSet && guildEntity.Preferences.AdminNotifyChannelId is not 0)
                {
                    await _botService.SendMessageAsync(guildEntity.Preferences.AdminNotifyChannelId, newMessage);
                }
                else
                {
                    DiscordMember owner = await guild.GetGuildOwnerAsync();
                    await owner.SendMessageAsync(newMessage);
                }
            }

            await context.EditResponseAsync(GeneralStrings.MessageSentToAll);
        }

        [Command("view-logs"), Description("View the logs of the bot.")]
        public async ValueTask ViewLogsAsync
        (
            SlashCommandContext context,
            [Description("The log file you want to read."), SlashAutoCompleteProvider<AzzyViewLogsAutocomplete>] string? logfile = null
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

            await using FileStream fileStream = new(logfile, FileMode.Open, FileAccess.Read);
            await using DiscordMessageBuilder builder = new();
            builder.WithContent($"Here are the logs from **{dateTime}**.");
            builder.AddFile($"{dateTime}.log", fileStream);
            await context.EditResponseAsync(builder);
        }
    }
}
