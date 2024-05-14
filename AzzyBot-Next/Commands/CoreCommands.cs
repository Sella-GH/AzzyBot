using System.Threading.Tasks;
using AzzyBot.Commands.Choices;
using AzzyBot.Logging;
using AzzyBot.Services;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

internal sealed class CoreCommands
{
    [Command("core")]
    internal sealed class Core(DiscordBotServiceHost discordBotServiceHost, ILogger<Core> logger)
    {
        private readonly ILogger<Core> _logger = logger;
        private readonly DiscordBotServiceHost _discordBotServiceHost = discordBotServiceHost;

        [Command("change-bot-status")]
        public async ValueTask CoreChangeStatusAsync(CommandContext context, [SlashChoiceProvider<BotActivityProvider>] int activity, [SlashChoiceProvider<BotStatusProvider>] int status, string doing, string? url = null)
        {
            _logger.CommandRequested(nameof(CoreChangeStatusAsync), context.User.GlobalName);

            await context.DeferResponseAsync();
            await _discordBotServiceHost.SetBotStatusAsync(status, activity, doing, url);
            await context.EditResponseAsync("Bot status has been updated!");
        }

        //[Command("info")]
        //public static async ValueTask CoreInfoAsync(CommandContext context)
        //{
        //    await context.DeferResponseAsync();
        //}

        [Command("ping")]
        public static ValueTask CorePingAsync(CommandContext context) => context.RespondAsync($"Pong! {context.Client.Ping}ms");
    }
}
