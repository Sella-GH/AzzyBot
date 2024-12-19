using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Bot.Commands.Autocompletes;
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Helpers;
using AzzyBot.Bot.Utilities.Records;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities.Records;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class CoreCommands
{
    [Command("core"), RequireGuild]
    public sealed class CoreGroup(ILogger<CoreGroup> logger, IOptions<AzzyBotSettings> settings, DbActions dbActions, DiscordBotService botService)
    {
        private readonly ILogger<CoreGroup> _logger = logger;
        private readonly AzzyBotSettings _settings = settings.Value;
        private readonly DbActions _dbActions = dbActions;
        private readonly DiscordBotService _botService = botService;

        [Command("force-channel-permissions-check"), Description("Forces a check of the permissions for the bot in the necessary channel."), RequirePermissions(BotPermissions = [], UserPermissions = [DiscordPermission.ManageChannels])]
        public async ValueTask ForceChannelPermissionsCheckAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _logger.CommandRequested(nameof(ForceChannelPermissionsCheckAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            GuildEntity? guild = await _dbActions.GetGuildAsync(_settings.ServerId, loadEverything: true);
            if (guild is null)
            {
                await context.RespondAsync(GeneralStrings.GuildNotFound);
                return;
            }

            await context.EditResponseAsync("I initiated a check of the permissions for the bot, please wait a little for the result.");

            await _botService.CheckPermissionsAsync([guild]);
        }

        [Command("help"), Description("Gives you an overview about all the available commands.")]
        public async ValueTask HelpAsync
        (
            SlashCommandContext context,
            [Description("The command you want to get more information about."), SlashAutoCompleteProvider<AzzyHelpAutocomplete>] string? command = null
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Client.CurrentApplication.Owners);
            ArgumentNullException.ThrowIfNull(context.Guild);
            ArgumentNullException.ThrowIfNull(context.Member);

            _logger.CommandRequested(nameof(HelpAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            IEnumerable<DiscordUser> botOwners = context.Client.CurrentApplication.Owners;
            ulong guildId = context.Guild.Id;
            DiscordMember member = context.Member;

            bool adminServer = botOwners.Any(u => u.Id == context.User.Id && member.Permissions.HasPermission(DiscordPermission.Administrator) && guildId == _settings.ServerId);
            bool approvedDebug = guildId == _settings.ServerId;
            List<DiscordEmbed> embeds = new(10);
            if (string.IsNullOrWhiteSpace(command))
            {
                foreach (KeyValuePair<string, List<AzzyHelpRecord>> kvp in AzzyHelp.GetAllCommands(context.Extension.Commands, adminServer, approvedDebug, member))
                {
                    if (embeds.Count is 10)
                        break;

                    DiscordEmbed embed = EmbedBuilder.BuildAzzyHelpEmbed(kvp.Value);
                    embeds.Add(embed);
                }
            }
            else
            {
                AzzyHelpRecord helpCommand = AzzyHelp.GetSingleCommand(context.Extension.Commands, command, adminServer, approvedDebug, member);
                DiscordEmbed embed = EmbedBuilder.BuildAzzyHelpEmbed(helpCommand);
                embeds.Add(embed);
            }

            AzuraCastEntity? ac = await _dbActions.GetAzuraCastAsync(context.Guild.Id);
            if (embeds.Count is not 10 && ac is null)
            {
                DiscordEmbed embed = EmbedBuilder.BuildAzzyHelpSetupEmbed();
                embeds.Add(embed);
            }

            await using DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbeds(embeds);

            await context.EditResponseAsync(messageBuilder);
        }

        [Command("stats")]
        public sealed class CoreStats(IOptions<AppStats> stats, ILogger<CoreStats> logger)
        {
            private readonly AppStats _stats = stats.Value;
            private readonly ILogger<CoreStats> _logger = logger;

            [Command("hardware"), Description("Shows information about the hardware side of the bot.")]
            public async ValueTask HardwareStatsAsync(SlashCommandContext context)
            {
                ArgumentNullException.ThrowIfNull(context);
                ArgumentNullException.ThrowIfNull(context.Guild);

                _logger.CommandRequested(nameof(HardwareStatsAsync), context.User.GlobalName);

                await context.DeferResponseAsync();

                Uri avaUrl = new(context.Client.CurrentUser.AvatarUrl);
                DiscordEmbed embed = await EmbedBuilder.BuildAzzyHardwareStatsEmbedAsync(avaUrl, context.Client.GetConnectionLatency(context.Guild.Id).Milliseconds);

                await context.EditResponseAsync(embed);
            }

            [Command("info"), Description("Shows information about the bot and it's components.")]
            public async ValueTask InfoStatsAsync(SlashCommandContext context)
            {
                ArgumentNullException.ThrowIfNull(context);

                _logger.CommandRequested(nameof(InfoStatsAsync), context.User.GlobalName);

                await context.DeferResponseAsync();

                Uri avaUrl = new(context.Client.CurrentUser.AvatarUrl);
                string dspVersion = context.Client.VersionString.Split('+')[0];
                DiscordEmbed embed = EmbedBuilder.BuildAzzyInfoStatsEmbed(avaUrl, dspVersion, _stats.Commit, _stats.CompilationDate, _stats.LocCs);

                await context.EditResponseAsync(embed);
            }

            [Command("ping"), Description("Ping the bot and get the latency to discord.")]
            public async ValueTask PingAsync(SlashCommandContext context)
            {
                ArgumentNullException.ThrowIfNull(context);
                ArgumentNullException.ThrowIfNull(context.Guild);

                _logger.CommandRequested(nameof(PingAsync), context.User.GlobalName);

                await context.RespondAsync($"Pong! {context.Client.GetConnectionLatency(context.Guild.Id).Milliseconds} ms");
            }
        }
    }
}
