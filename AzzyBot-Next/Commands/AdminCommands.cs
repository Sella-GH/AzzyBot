using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using AzzyBot.Commands.Autocompletes;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Services;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

internal sealed class AdminCommands
{
    [Command("admin")]
    [RequireApplicationOwner]
    internal sealed class Admin(DiscordBotService botService, DbActions dbActions, ILogger<Admin> logger)
    {
        private readonly DiscordBotService _botService = botService;
        private readonly DbActions _dbActions = dbActions;
        private readonly ILogger<Admin> _logger = logger;

        [Command("get-debug-guilds")]
        public async ValueTask AdminGetDebugGuildsAsync(SlashCommandContext context)
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
            foreach (GuildsEntity guild in dbGuilds)
            {
                if (clientGuilds.TryGetValue(guild.UniqueId, out DiscordGuild? discordGuild))
                    stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"- {discordGuild.Name}");
            }

            await context.EditResponseAsync(stringBuilder.ToString());
        }

        [Command("remove-debug-guild")]
        public async ValueTask AdminRemoveDebugGuildsAsync(SlashCommandContext context, [SlashAutoCompleteProvider<GuildsAutocomplete>] string guildId = "")
        {
            _logger.CommandRequested(nameof(AdminRemoveDebugGuildsAsync), context.User.GlobalName);

            if (!ulong.TryParse(guildId, out ulong guildIdValue))
            {
                await context.RespondAsync("Invalid guild ID.", true);
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
        public async ValueTask AdminSetDebugGuildsAsync(SlashCommandContext context, [SlashAutoCompleteProvider<GuildsAutocomplete>] string guildId = "")
        {
            _logger.CommandRequested(nameof(AdminSetDebugGuildsAsync), context.User.GlobalName);

            if (!ulong.TryParse(guildId, out ulong guildIdValue))
            {
                await context.RespondAsync("Invalid guild ID.", true);
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
