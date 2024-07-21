using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzzyBot.Bot.Commands.Autocompletes;
using AzzyBot.Bot.Commands.Choices;
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Utilities;
using AzzyBot.Core.Logging;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class AdminCommands
{
    [Command("admin"), RequireGuild, RequireApplicationOwner, RequirePermissions(DiscordPermissions.None, DiscordPermissions.Administrator)]
    public sealed class AdminGroup(DbActions dbActions, DiscordBotService botService, DiscordBotServiceHost botServiceHost, ILogger<AdminGroup> logger)
    {
        private readonly DbActions _dbActions = dbActions;
        private readonly DiscordBotService _botService = botService;
        private readonly DiscordBotServiceHost _botServiceHost = botServiceHost;
        private readonly ILogger<AdminGroup> _logger = logger;

        [Command("change-bot-status"), Description("Change the global bot status according to your likes.")]
        public async ValueTask ChangeStatusAsync
        (
            CommandContext context,
            [Description("Choose the activity type which the bot should have."), SlashChoiceProvider<BotActivityProvider>] int activity = 1,
            [Description("Choose the status type which the bot should have."), SlashChoiceProvider<BotStatusProvider>] int status = 2,
            [Description("Enter a custom doing which is added after the activity type."), MinMaxLength(0, 128)] string doing = "Music",
            [Description("Enter a custom url. Only usable when having activity type streaming or watching!")] Uri? url = null,
            [Description("Reset the bot status to default."), SlashChoiceProvider<BooleanYesNoStateProvider>] int reset = 0
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(ChangeStatusAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            await _botServiceHost.SetBotStatusAsync(status, activity, doing, url, reset == 1);

            if (reset is 1)
            {
                await context.EditResponseAsync("Bot status has been reset!");
            }
            else
            {
                await context.EditResponseAsync("Bot status has been updated!");
            }
        }

        [Command("get-joined-server"), Description("Displays all servers the bot is in.")]
        public async ValueTask GetJoinedGuildsAsync(CommandContext context, [Description("Select the server you want to get more information about."), SlashAutoCompleteProvider<GuildsAutocomplete>] string? serverId = null)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(GetJoinedGuildsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            IReadOnlyDictionary<ulong, DiscordGuild> guilds = _botService.GetDiscordGuilds;
            if (guilds.Count == 0)
            {
                await context.EditResponseAsync("I am not in any server.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(serverId))
            {
                if (!ulong.TryParse(serverId, out ulong guildIdValue))
                {
                    await context.EditResponseAsync("Invalid server id.");
                    return;
                }

                DiscordGuild? guild = _botService.GetDiscordGuild(guildIdValue);
                if (guild is null)
                {
                    _logger.DiscordItemNotFound(nameof(DiscordGuild), guildIdValue);
                    await context.EditResponseAsync("Server not found.");
                    return;
                }

                DiscordEmbed embed = EmbedBuilder.BuildGuildAddedEmbed(guild, true);
                await context.EditResponseAsync(embed);

                return;
            }

            // TODO This is not suitable for more than a few houndred servers
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine("I am in the following servers:");
            foreach (KeyValuePair<ulong, DiscordGuild> guild in guilds)
            {
                stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"- {guild.Value.Name} ({guild.Key})");
            }

            await context.EditResponseAsync(stringBuilder.ToString());
        }

        [Command("remove-joined-server"), Description("Removes the bot from a server.")]
        public async ValueTask RemoveJoinedGuildAsync(CommandContext context, [Description("Select the server you want to remove."), SlashAutoCompleteProvider<GuildsAutocomplete>] string serverId)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(RemoveJoinedGuildAsync), context.User.GlobalName);

            if (!ulong.TryParse(serverId, out ulong guildIdValue))
            {
                await context.RespondAsync("Invalid server id.");
                return;
            }

            await context.DeferResponseAsync();

            DiscordGuild? guild = _botService.GetDiscordGuild(guildIdValue);
            if (guild is null)
            {
                _logger.DiscordItemNotFound(nameof(DiscordGuild), guildIdValue);
                await context.EditResponseAsync("Server not found.");
                return;
            }

            await guild.LeaveAsync();

            await context.EditResponseAsync($"I left **{guild.Name}** ({guild.Id}).");
        }

        [Command("send-bot-wide-message"), Description("Sends a message to all servers the bot is in.")]
        public async ValueTask SendBotWideMessageAsync(CommandContext context, [Description("The message you want to send."), MinMaxLength(1, 2000)] string message)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(SendBotWideMessageAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            IReadOnlyDictionary<ulong, DiscordGuild> guilds = _botService.GetDiscordGuilds;
            IReadOnlyList<GuildEntity> guildsEntities = await _dbActions.GetGuildsAsync(true);

            foreach (KeyValuePair<ulong, DiscordGuild> guild in guilds)
            {
                GuildEntity? guildEntity = guildsEntities.FirstOrDefault(g => g.UniqueId == guild.Key);
                if (guildEntity is null)
                {
                    _logger.DatabaseGuildNotFound(guild.Key);
                    continue;
                }

                if (guildEntity.ConfigSet && guildEntity.Preferences.AdminNotifyChannelId is not 0)
                {
                    await _botService.SendMessageAsync(guildEntity.Preferences.AdminNotifyChannelId, message);
                }
                else
                {
                    await guild.Value.Owner.SendMessageAsync(message);
                }
            }

            await context.EditResponseAsync("Message sent to all servers.");
        }
    }
}
