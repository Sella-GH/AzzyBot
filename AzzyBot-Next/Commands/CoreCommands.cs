using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Settings;
using AzzyBot.Utilities;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

internal sealed class CoreCommands
{
    [Command("core")]
    [RequireGuild]
    internal sealed class Core(AzzyBotSettingsRecord settings, DbActions dbActions, ILogger<Core> logger)
    {
        private readonly AzzyBotSettingsRecord _settings = settings;
        private readonly DbActions _dbActions = dbActions;
        private readonly ILogger<Core> _logger = logger;

        [Command("help"), Description("Gives an overview about all the available commands.")]
        public async ValueTask CoreHelpAsync(CommandContext context)
        {
            _logger.CommandRequested(nameof(CoreHelpAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            IEnumerable<DiscordUser> botOwners = context.Client.CurrentApplication.Owners ?? throw new InvalidOperationException("Invalid bot owners");
            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Invalid guild id");
            DiscordMember member = context.Member ?? throw new InvalidOperationException("Invalid member");
            GuildsEntity guild = await _dbActions.GetGuildEntityAsync(guildId);

            bool adminServer = false;
            foreach (DiscordUser user in botOwners)
            {
                if (user.Id == context.User.Id && member.Permissions.HasPermission(DiscordPermissions.Administrator) && guildId == _settings.ServerId)
                {
                    adminServer = true;
                    break;
                }
            }

            bool approvedDebug = guild.IsDebugAllowed || guildId == _settings.ServerId;

            await using DiscordMessageBuilder messageBuilder = new();
            List<DiscordEmbed> embeds = AzzyHelp.GetCommands(adminServer, approvedDebug, member);
            messageBuilder.AddEmbeds(embeds);

            await context.EditResponseAsync(messageBuilder);
        }

        //[Command("info")]
        //public static async ValueTask CoreInfoAsync(CommandContext context)
        //{
        //    await context.DeferResponseAsync();
        //}

        [Command("ping"), Description("Ping the bot and get the latency to discord.")]
        public async ValueTask CorePingAsync(CommandContext context)
        {
            _logger.CommandRequested(nameof(CorePingAsync), context.User.GlobalName);

            await context.RespondAsync($"Pong! {context.Client.Ping}ms");
        }
    }
}
