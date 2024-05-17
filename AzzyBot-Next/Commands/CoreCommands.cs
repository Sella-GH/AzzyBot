using System.ComponentModel;
using System.Threading.Tasks;
using AzzyBot.Logging;
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
    internal sealed class Core(ILogger<Core> logger)
    {
        private readonly ILogger<Core> _logger = logger;

        [Command("help"), Description("Gives an overview about all the available commands.")]
        public async ValueTask CoreHelpAsync(CommandContext context)
        {
            _logger.CommandRequested(nameof(CoreHelpAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            await using DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbeds(AzzyHelp.GetCommands());

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
