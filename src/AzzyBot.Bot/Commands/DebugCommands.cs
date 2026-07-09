using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Quic;
using System.Text;
using System.Threading.Tasks;

using AzzyBot.Bot.Commands.Autocompletes;
using AzzyBot.Bot.Commands.Choices;
using AzzyBot.Bot.Helpers;
using AzzyBot.Bot.Logging;
using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Bot.Structs;
using AzzyBot.Core.Utilities;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services.Interfaces;

using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class DebugCommands
{
    [Command("debug"), RequireGuild, RequirePermissions(botPermissions: [], userPermissions: [DiscordPermission.Administrator])]
    public sealed class DebugGroup(ILogger<DebugGroup> logger, IDbActions dbActions, IWebRequestService webRequestService)
    {
        private readonly ILogger<DebugGroup> _logger = logger;
        private readonly IDbActions _dbActions = dbActions;
        private readonly IWebRequestService _webRequestService = webRequestService;

        [Command("cluster-logging"), Description("Test the logging file rotation feature of the bot.")]
        public async ValueTask DebugClusterLoggingAsync
        (
            SlashCommandContext context,
            [Description("Set the amount of log entries to be written.")] int logAmount = 1000000
        )
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(DebugClusterLoggingAsync), context.User.Username);

            await context.DeferResponseAsync();
            for (int i = 0; i < logAmount; i++)
            {
                _logger.ClusterLoggingTest(i);
            }

            await context.EditResponseAsync("Cluster logging test was successful!");
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Debug test")]
        [Command("database-concurrency"), Description("Test the database concurrency feature of the bot.")]
        public async ValueTask DebugDatabaseConcurrencyAsync
        (
            SlashCommandContext context,
            [Description("Select the Guild to test the concurrency."), SlashAutoCompleteProvider<GuildsAutocomplete>] string serverId
        )
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(DebugDatabaseConcurrencyAsync), context.User.Username);

            if (!ulong.TryParse(serverId, out ulong guildIdValue))
            {
                await context.RespondAsync(GeneralStrings.GuildIdInvalid);
                return;
            }

            await context.DeferResponseAsync();

            StringBuilder sb = new();
            sb.AppendLine(CultureInfo.InvariantCulture, $"GuildId is {serverId}");
            sb.AppendLine("Checking if guild exists.");

            GuildEntity? guild = await _dbActions.ReadGuildAsync(guildIdValue);
            if (guild is null)
            {
                await context.EditResponseAsync(GeneralStrings.GuildNotFound);
                return;
            }

            sb.AppendLine("Guild exists, setting up the test.");

            async Task ChangeGuildValuesAsync(GuildEntity guildEntity, int value)
            {
                if (value is 1)
                {
                    await _dbActions.UpdateGuildAsync(guildEntity.UniqueId, updateLastPermissionCheck: true);
                    sb.AppendLine("Changed LastPermissionCheck to true");
                }
                else if (value is 2)
                {
                    await _dbActions.UpdateGuildAsync(guildEntity.UniqueId, legalsAccepted: true);
                    sb.AppendLine("Changed LegalsAccepted to true");
                }
            }

            IReadOnlyList<Task> tasks = [
                ChangeGuildValuesAsync(guild, 1),
                ChangeGuildValuesAsync(guild, 2)
            ];

            sb.AppendLine("Starting the concurrency test");

            try
            {
                await Task.WhenAll(tasks);
                sb.AppendLine("Concurrency test was successful!");
            }
            catch (Exception ex)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"Concurrency test failed with the following exception: {ex.Message}");
            }

            await context.EditResponseAsync(sb.ToString());
        }

        [Command("encrypt-decrypt"), Description("Test the encryption and decryption features of the bot.")]
        public async ValueTask DebugEncryptDecryptAsync
        (
            SlashCommandContext context,
            [Description("Enter the text which should be encrypted and decrypted again."), MinMaxLength(0, 1000)] string text
        )
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(DebugEncryptDecryptAsync), context.User.Username);

            await context.DeferResponseAsync();

            string encrypted = Crypto.Encrypt(text);
            string decrypted = Crypto.Decrypt(encrypted);

            await context.EditResponseAsync($"Original: {text}\nEncrypted: {encrypted}\nDecrypted: {decrypted}");
        }

        [Command("trigger-exception"), Description("Triggers an InvalidOperationException to test if the error reporting system works.")]
        public async ValueTask DebugTriggerExceptionAsync
        (
            SlashCommandContext context,
            [Description("Enable to defer the message before throwing the exception."), SlashChoiceProvider<BooleanEnableDisableStateProvider>] int throwAfterDefering = 0,
            [Description("Enable to throw the exception after a reply was already made."), SlashChoiceProvider<BooleanEnableDisableStateProvider>] int afterReply = 0
        )
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(DebugTriggerExceptionAsync), context.User.Username);

            if (throwAfterDefering is 1)
                await context.DeferResponseAsync();

            if (afterReply is 1 && throwAfterDefering is not 1)
                await context.RespondAsync("This is a debug reply");

            if (afterReply is 1 && throwAfterDefering is 1)
                await context.EditResponseAsync("This is a debug reply edit");

            throw new InvalidOperationException("This is a debug exception");
        }

        [Command("webservice-tests"), Description("Test if the bot is able to resolve connections to external websites.")]
        public async ValueTask DebugWebServiceTestsAsync
        (
            SlashCommandContext context,
            [Description("Enter a valid url like the following: https://google.com")] Uri url
        )
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(DebugWebServiceTestsAsync), context.User.Username);

            await context.DeferResponseAsync(ephemeral: true);

            AzzyDebugWebRequestStruct req = await _webRequestService.DebugGetWebAsync(url);
            StringBuilder sb = new();
            sb.AppendLine(CultureInfo.InvariantCulture, $"Request Uri: {req.RequestUri}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Method: {req.Method}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"QUIC Support: {QuicConnection.IsSupported}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Version: {req.HttpVersion}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Status Code: {req.StatusCode}");
            foreach (KeyValuePair<string, IEnumerable<string>> header in req.ReqHeaders)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"Request Header: {header.Key} - {string.Join(", ", header.Value)}");
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in req.ResHeaders)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"Response Header: {header.Key} - {string.Join(", ", header.Value)}");
            }

            sb.AppendLine(CultureInfo.InvariantCulture, $"Retries: {req.Retries}");

            if (string.IsNullOrEmpty(req.Content))
            {
                await context.EditResponseAsync(sb.ToString());
                return;
            }

            string fileName = $"WebRequestDebug_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss-fffffff}.txt";
            string filePath = await FileOperations.CreateTempFileAsync(req.Content, fileName);
            await using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            DiscordInteractionResponseBuilder builder = new();
            builder.WithContent(sb.ToString());
            builder.AddFile(fileName, fs, AddFileOptions.CloseStream);
            await context.EditResponseAsync(builder.AsEphemeral());
            if (File.Exists(filePath))
                FileOperations.DeleteFile(filePath);
        }
    }
}
