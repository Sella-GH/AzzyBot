﻿using System;
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
    internal sealed class Core(DiscordBotServiceHost discordBotServiceHost, ILogger<Core> logger)
    {
        private readonly ILogger<Core> _logger = logger;
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
    internal sealed class Debug(WebRequestService webRequestService, ILogger<Debug> logger)
    {
        private readonly ILogger<Debug> _logger = logger;
        private readonly WebRequestService _webRequestService = webRequestService;

        [Command("trigger-exception")]
        public async ValueTask DebugTriggerExceptionAsync(SlashCommandContext context)
        {
            _logger.CommandRequested(nameof(DebugTriggerExceptionAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            throw new InvalidOperationException("This is a debug exception");
        }

        [Command("webservice-tests")]
        public async ValueTask DebugWebServiceTestsAsync(SlashCommandContext context, string url)
        {
            _logger.CommandRequested(nameof(DebugWebServiceTestsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

            if (!url.Contains("https://", StringComparison.OrdinalIgnoreCase) || !url.Contains("http://", StringComparison.OrdinalIgnoreCase))
                url = $"http://{url}";

            Uri uri = new(url);
            await _webRequestService.GetWebAsync(uri);

            await context.EditResponseAsync($"Web service test for *{uri}* was successful!");
        }
    }
}
