using System;
using System.Threading.Tasks;
using AzzyBot.Commands.Choices;
using AzzyBot.Logging;
using AzzyBot.Services;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

internal sealed class CoreCommands
{
    [Command("core")]
    internal sealed class Core(DiscordBotServiceHost discordBotServiceHost, ILogger<CoreCommands> logger)
    {
        private readonly ILogger<CoreCommands> _logger = logger;
        private readonly DiscordBotServiceHost _discordBotServiceHost = discordBotServiceHost;

        [Command("change-bot-status")]
        public async ValueTask CoreChangeStatusAsync(SlashCommandContext context, [SlashChoiceProvider<BotActivityProvider>] int activity, [SlashChoiceProvider<BotStatusProvider>] int status, string doing, string? url = null)
        {
            _logger.CommandRequested(nameof(CoreChangeStatusAsync), context.User.GlobalName);

            await context.DeferResponseAsync();
            await _discordBotServiceHost.SetBotStatusAsync(status, activity, doing, url);
            await context.EditResponseAsync("Bot status has been updated!");
        }

        //[Command("info")]
        //public static async ValueTask CoreInfoAsync(SlashCommandContext context)
        //{
        //    await context.DeferResponseAsync();
        //}

        [Command("ping")]
        public static ValueTask CorePingAsync(SlashCommandContext context) => context.RespondAsync($"Pong! {context.Client.Ping}ms");
    }

    [Command("debug")]
    internal sealed class Debug
    {
        [Command("trigger-exception")]
        public static async ValueTask TriggerExceptionAsync(SlashCommandContext context)
        {
            await context.DeferResponseAsync();

            throw new InvalidOperationException("This is a debug exception");
        }
    }
}
