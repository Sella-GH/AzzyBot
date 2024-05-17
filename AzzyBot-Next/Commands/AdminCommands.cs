﻿using System.Collections.Generic;
using System.ComponentModel;
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
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

internal sealed class AdminCommands
{
    [Command("admin"), RequireGuild, RequireApplicationOwner, RequirePermissions(DiscordPermissions.None, DiscordPermissions.Administrator)]
    internal sealed class Admin(DbActions dbActions, DiscordBotService botService, DiscordBotServiceHost botServiceHost, ILogger<Admin> logger)
    {
        private readonly DbActions _dbActions = dbActions;
        private readonly DiscordBotService _botService = botService;
        private readonly DiscordBotServiceHost _botServiceHost = botServiceHost;
        private readonly ILogger<Admin> _logger = logger;

        [Command("change-bot-status"), Description("Change the global bot status according to your likes.")]
        public async ValueTask CoreChangeStatusAsync
            (
            CommandContext context,
            [Description("Choose the activity type which the bot should have."), SlashChoiceProvider<BotActivityProvider>] int activity,
            [Description("Choose the status type which the bot should have."), SlashChoiceProvider<BotStatusProvider>] int status,
            [Description("Enter a custom doing which is added after the activity type."), MinMaxLength(0, 128)] string doing,
            [Description("Enter a custom url. Only usable when having activity type streaming or watching!")] string? url = null,
            [Description("Reset the status to default.")] bool reset = false
            )
        {
            _logger.CommandRequested(nameof(CoreChangeStatusAsync), context.User.GlobalName);

            await context.DeferResponseAsync();
            await _botServiceHost.SetBotStatusAsync(status, activity, doing, url, reset);
            await context.EditResponseAsync("Bot status has been updated!");
        }

        [Command("get-debug-servers"), Description("Displays all servers which can execute debug commands.")]
        public async ValueTask AdminGetDebugGuildsAsync(CommandContext context)
        {
            _logger.CommandRequested(nameof(AdminGetDebugGuildsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            List<GuildsEntity> dbGuilds = await _dbActions.GetGuildEntitiesWithDebugAsync();
            if (dbGuilds.Count == 0)
            {
                await context.EditResponseAsync("No debug servers found.");
                return;
            }

            Dictionary<ulong, DiscordGuild> clientGuilds = _botService.GetDiscordGuilds();
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine("I found the following Debug servers:");
            foreach (GuildsEntity guild in dbGuilds.Where(g => clientGuilds.ContainsKey(g.UniqueId)))
            {
                stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"- {clientGuilds[guild.UniqueId].Name}");
            }

            await context.EditResponseAsync(stringBuilder.ToString());
        }

        [Command("remove-debug-server"), Description("Removes the permission to execute debug commands from a server.")]
        public async ValueTask AdminRemoveDebugGuildsAsync(CommandContext context, [Description("Select the server you want to remove."), SlashAutoCompleteProvider<GuildsAutocomplete>] string serverId = "")
        {
            _logger.CommandRequested(nameof(AdminRemoveDebugGuildsAsync), context.User.GlobalName);

            if (!ulong.TryParse(serverId, out ulong guildIdValue))
            {
                await context.RespondAsync("Invalid server id.");
                return;
            }

            await context.DeferResponseAsync();

            GuildsEntity? guildEntity = await _dbActions.GetGuildEntityAsync(guildIdValue);
            if (guildEntity is null)
            {
                await context.EditResponseAsync("Server not found in the database.");
                return;
            }

            if (!guildEntity.IsDebugAllowed)
            {
                await context.EditResponseAsync("Server is not specified as debug.");
                return;
            }

            await _dbActions.SetGuildEntityAsync(guildIdValue, 0, false);

            await context.EditResponseAsync($"{await _botService.GetDiscordGuildAsync(guildIdValue)} removed from debug servers.");
        }

        [Command("set-debug-server"), Description("Adds the permission to execute debug commands to a server.")]
        public async ValueTask AdminSetDebugGuildsAsync(CommandContext context, [Description("Select the server you want to add."), SlashAutoCompleteProvider<GuildsAutocomplete>] string serverId = "")
        {
            _logger.CommandRequested(nameof(AdminSetDebugGuildsAsync), context.User.GlobalName);

            if (!ulong.TryParse(serverId, out ulong guildIdValue))
            {
                await context.RespondAsync("Invalid server id.");
                return;
            }

            await context.DeferResponseAsync();

            GuildsEntity? guildEntity = await _dbActions.GetGuildEntityAsync(guildIdValue);
            if (guildEntity is null)
            {
                await context.EditResponseAsync("Server not found in the database.");
                return;
            }

            if (guildEntity.IsDebugAllowed)
            {
                await context.EditResponseAsync("Server is already specified as debug.");
                return;
            }

            await _dbActions.SetGuildEntityAsync(guildIdValue, 0, true);

            await context.EditResponseAsync($"{await _botService.GetDiscordGuildAsync(guildIdValue)} added to debug servers.");
        }
    }
}