using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzzyBot.Commands.Autocompletes;
using AzzyBot.Commands.Choices;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Services;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

internal sealed class AdminCommands
{
    [Command("admin")]
    [RequireGuild]
    [RequireApplicationOwner]
    internal sealed class Admin(DbActions dbActions, DiscordBotService botService, DiscordBotServiceHost botServiceHost, ILogger<Admin> logger)
    {
        private readonly DbActions _dbActions = dbActions;
        private readonly DiscordBotService _botService = botService;
        private readonly DiscordBotServiceHost _botServiceHost = botServiceHost;
        private readonly ILogger<Admin> _logger = logger;

        [Command("change-bot-status")]
        public async ValueTask CoreChangeStatusAsync(CommandContext context, [SlashChoiceProvider<BotActivityProvider>] int activity, [SlashChoiceProvider<BotStatusProvider>] int status, string doing, string? url = null)
        {
            _logger.CommandRequested(nameof(CoreChangeStatusAsync), context.User.GlobalName);

            await context.DeferResponseAsync();
            await _botServiceHost.SetBotStatusAsync(status, activity, doing, url);
            await context.EditResponseAsync("Bot status has been updated!");
        }

        [Command("get-debug-guilds")]
        public async ValueTask AdminGetDebugGuildsAsync(CommandContext context)
        {
            _logger.CommandRequested(nameof(AdminGetDebugGuildsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            List<GuildsEntity> dbGuilds = await _dbActions.GetGuildEntitiesWithDebugAsync();
            if (dbGuilds.Count == 0)
            {
                await context.EditResponseAsync("No debug guilds found.");
                return;
            }

            Dictionary<ulong, DiscordGuild> clientGuilds = _botService.GetDiscordGuilds();
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine("I found the following Debug guilds:");
            foreach (GuildsEntity guild in dbGuilds.Where(g => clientGuilds.ContainsKey(g.UniqueId)))
            {
                stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"- {clientGuilds[guild.UniqueId].Name}");
            }

            await context.EditResponseAsync(stringBuilder.ToString());
        }

        [Command("remove-debug-guild")]
        public async ValueTask AdminRemoveDebugGuildsAsync(CommandContext context, [SlashAutoCompleteProvider<GuildsAutocomplete>] string guildId = "")
        {
            _logger.CommandRequested(nameof(AdminRemoveDebugGuildsAsync), context.User.GlobalName);

            if (!ulong.TryParse(guildId, out ulong guildIdValue))
            {
                await context.RespondAsync("Invalid guild ID.");
                return;
            }

            await context.DeferResponseAsync();

            GuildsEntity? guildEntity = await _dbActions.GetGuildEntityAsync(guildIdValue);
            if (guildEntity is null)
            {
                await context.EditResponseAsync("Guild not found in the database.");
                return;
            }

            if (!guildEntity.IsDebugAllowed)
            {
                await context.EditResponseAsync("Guild is not a debug guild.");
                return;
            }

            await _dbActions.SetGuildEntityAsync(guildIdValue, 0, false);

            await context.EditResponseAsync($"{await _botService.GetDiscordGuildAsync(guildIdValue)} removed from debug guilds.");
        }

        [Command("set-debug-guild")]
        public async ValueTask AdminSetDebugGuildsAsync(CommandContext context, [SlashAutoCompleteProvider<GuildsAutocomplete>] string guildId = "")
        {
            _logger.CommandRequested(nameof(AdminSetDebugGuildsAsync), context.User.GlobalName);

            if (!ulong.TryParse(guildId, out ulong guildIdValue))
            {
                await context.RespondAsync("Invalid guild ID.");
                return;
            }

            await context.DeferResponseAsync();

            GuildsEntity? guildEntity = await _dbActions.GetGuildEntityAsync(guildIdValue);
            if (guildEntity is null)
            {
                await context.EditResponseAsync("Guild not found in the database.");
                return;
            }

            if (guildEntity.IsDebugAllowed)
            {
                await context.EditResponseAsync("Guild is already a debug guild.");
                return;
            }

            await _dbActions.SetGuildEntityAsync(guildIdValue, 0, true);

            await context.EditResponseAsync($"{await _botService.GetDiscordGuildAsync(guildIdValue)} added to debug guilds.");
        }
    }
}
