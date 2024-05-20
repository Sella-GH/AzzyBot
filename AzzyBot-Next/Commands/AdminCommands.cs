using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class AdminCommands
{
    [Command("admin"), RequireGuild, RequireApplicationOwner, RequirePermissions(DiscordPermissions.None, DiscordPermissions.Administrator)]
    public sealed class AdminGroup(DiscordBotServiceHost botServiceHost, ILogger<AdminGroup> logger)
    {
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
            [Description("Reset the status to default.")] bool reset = false
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(ChangeStatusAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            await _botServiceHost.SetBotStatusAsync(status, activity, doing, url, reset);

            if (reset)
            {
                await context.EditResponseAsync("Bot status has been reset!");
            }
            else
            {
                await context.EditResponseAsync("Bot status has been updated!");
            }
        }

        [Command("debug-servers")]
        public sealed class AdminDebugServers(DbActions dbActions, DiscordBotService botService, ILogger<AdminDebugServers> logger)
        {
            private readonly DbActions _dbActions = dbActions;
            private readonly DiscordBotService _botService = botService;
            private readonly ILogger<AdminDebugServers> _logger = logger;

            [Command("add-server"), Description("Adds the permission to execute debug commands to a server.")]
            public async ValueTask AddDebugGuildsAsync(CommandContext context, [Description("Select the server you want to add."), SlashAutoCompleteProvider<GuildsAutocomplete>] string serverId = "")
            {
                ArgumentNullException.ThrowIfNull(context, nameof(context));

                _logger.CommandRequested(nameof(AddDebugGuildsAsync), context.User.GlobalName);

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

            [Command("get-servers"), Description("Displays all servers which can execute debug commands.")]
            public async ValueTask GetDebugGuildsAsync(CommandContext context)
            {
                ArgumentNullException.ThrowIfNull(context, nameof(context));

                _logger.CommandRequested(nameof(GetDebugGuildsAsync), context.User.GlobalName);

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

            [Command("remove-server"), Description("Removes the permission to execute debug commands from a server.")]
            public async ValueTask RemoveDebugGuildsAsync(CommandContext context, [Description("Select the server you want to remove."), SlashAutoCompleteProvider<GuildsAutocomplete>] string serverId = "")
            {
                ArgumentNullException.ThrowIfNull(context, nameof(context));

                _logger.CommandRequested(nameof(RemoveDebugGuildsAsync), context.User.GlobalName);

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
        }
    }
}
