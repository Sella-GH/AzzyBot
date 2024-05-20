using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Services;
using AzzyBot.Utilities.Encryption;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class DebugCommands
{
    [Command("debug"), RequireGuild, RequirePermissions(DiscordPermissions.None, DiscordPermissions.Administrator)]
    public sealed class DebugGroup(WebRequestService webRequestService, ILogger<DebugGroup> logger)
    {
        private readonly ILogger<DebugGroup> _logger = logger;
        private readonly WebRequestService _webRequestService = webRequestService;

        [Command("encrypt-decrypt"), Description("Test the encryption and decryption features of the bot.")]
        public async ValueTask DebugEncryptDecryptAsync(CommandContext context, [Description("Enter the text which should be encrypted and decrypted again."), MinMaxLength(0, 1000)] string text)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(DebugEncryptDecryptAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            string encrypted = Crypto.Encrypt(text);
            string decrypted = Crypto.Decrypt(encrypted);

            await context.EditResponseAsync($"Original: {text}\nEncrypted: {encrypted}\nDecrypted: {decrypted}");
        }

        [Command("trigger-exception"), Description("Triggers an InvalidOperationException to test if the error reporting system works.")]
        public async ValueTask DebugTriggerExceptionAsync
            (
            CommandContext context,
            [Description("Enable to defer the message before throwing the exception.")] bool throwAfterDefering = true,
            [Description("Enable to throw the exception after a reply was already made.")] bool afterReply = false
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(DebugTriggerExceptionAsync), context.User.GlobalName);

            if (throwAfterDefering)
                await context.DeferResponseAsync();

            if (afterReply && !throwAfterDefering)
                await context.RespondAsync("This is a debug reply");

            if (afterReply && throwAfterDefering)
                await context.EditResponseAsync("This is a debug reply edit");

            throw new InvalidOperationException("This is a debug exception");
        }

        [Command("webservice-tests"), Description("Test if the bot is able to resolve connections to external websites.")]
        public async ValueTask DebugWebServiceTestsAsync(CommandContext context, [Description("Enter a valid url like the following: https://google.com")] Uri url)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(DebugWebServiceTestsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            await _webRequestService.GetWebAsync(url);

            await context.EditResponseAsync($"Web service test for *{url}* was successful!");
        }
    }
}
