using System;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Services;
using AzzyBot.Utilities.Encryption;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

internal sealed class DebugCommands
{
    [Command("debug")]
    [RequirePermissions(DiscordPermissions.None, DiscordPermissions.Administrator)]
    internal sealed class Debug(WebRequestService webRequestService, ILogger<Debug> logger)
    {
        private readonly ILogger<Debug> _logger = logger;
        private readonly WebRequestService _webRequestService = webRequestService;

        [Command("encrypt-decrypt")]
        public async ValueTask DebugEncryptDecryptAsync(CommandContext context, string text)
        {
            _logger.CommandRequested(nameof(DebugEncryptDecryptAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            string encrypted = Crypto.Encrypt(text);
            string decrypted = Crypto.Decrypt(encrypted);

            await context.EditResponseAsync($"Original: {text}\nEncrypted: {encrypted}\nDecrypted: {decrypted}");
        }

        [Command("trigger-exception")]
        public async ValueTask DebugTriggerExceptionAsync(CommandContext context, bool beforeOrAfterDefer = true, bool afterReply = false)
        {
            _logger.CommandRequested(nameof(DebugTriggerExceptionAsync), context.User.GlobalName);

            if (beforeOrAfterDefer)
                await context.DeferResponseAsync();

            if (afterReply && !beforeOrAfterDefer)
                await context.RespondAsync("This is a debug reply");

            if (afterReply && beforeOrAfterDefer)
                await context.EditResponseAsync("This is a debug reply edit");

            throw new InvalidOperationException("This is a debug exception");
        }

        [Command("webservice-tests")]
        public async ValueTask DebugWebServiceTestsAsync(CommandContext context, Uri url)
        {
            _logger.CommandRequested(nameof(DebugWebServiceTestsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();
            await _webRequestService.GetWebAsync(url);
            await context.EditResponseAsync($"Web service test for *{url}* was successful!");
        }
    }
}
