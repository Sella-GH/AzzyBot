using System;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Services;
using AzzyBot.Utilities.Encryption;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

internal sealed class DebugCommands
{
    [Command("debug")]
    internal sealed class Debug(WebRequestService webRequestService, ILogger<Debug> logger)
    {
        private readonly ILogger<Debug> _logger = logger;
        private readonly WebRequestService _webRequestService = webRequestService;

        [Command("encrypt-decrypt")]
        public async ValueTask DebugEncryptDecryptAsync(SlashCommandContext context, string text)
        {
            _logger.CommandRequested(nameof(DebugEncryptDecryptAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            string encrypted = Crypto.Encrypt(text);
            string decrypted = Crypto.Decrypt(encrypted);

            await context.EditResponseAsync($"Original: {text}\nEncrypted: {encrypted}\nDecrypted: {decrypted}");
        }

        [Command("trigger-exception")]
        public async ValueTask DebugTriggerExceptionAsync(SlashCommandContext context)
        {
            _logger.CommandRequested(nameof(DebugTriggerExceptionAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            throw new InvalidOperationException("This is a debug exception");
        }

        [Command("webservice-tests")]
        public async ValueTask DebugWebServiceTestsAsync(SlashCommandContext context, Uri url)
        {
            _logger.CommandRequested(nameof(DebugWebServiceTestsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();
            await _webRequestService.GetWebAsync(url);
            await context.EditResponseAsync($"Web service test for *{url}* was successful!");
        }
    }
}
