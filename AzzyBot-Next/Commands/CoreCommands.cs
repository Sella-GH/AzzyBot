﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Commands.Autocompletes;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Settings;
using AzzyBot.Utilities;
using AzzyBot.Utilities.Records;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class CoreCommands
{
    [Command("core"), RequireGuild]
    public sealed class CoreGroup(AzzyBotSettingsRecord settings, DbActions dbActions, ILogger<CoreGroup> logger)
    {
        private readonly AzzyBotSettingsRecord _settings = settings;
        private readonly DbActions _dbActions = dbActions;
        private readonly ILogger<CoreGroup> _logger = logger;

        [Command("help"), Description("Gives an overview about all the available commands.")]
        public async ValueTask HelpAsync
            (
            CommandContext context,
            [Description("The command you want to get more information about."), SlashAutoCompleteProvider<AzzyHelpAutocomplete>] string? command = null
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(HelpAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            IEnumerable<DiscordUser> botOwners = context.Client.CurrentApplication.Owners ?? throw new InvalidOperationException("Invalid bot owners");
            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Invalid guild id");
            DiscordMember member = context.Member ?? throw new InvalidOperationException("Invalid member");
            GuildsEntity guild = await _dbActions.GetGuildAsync(guildId);

            bool adminServer = false;
            foreach (DiscordUser _ in botOwners.Where(u => u.Id == context.User.Id && member.Permissions.HasPermission(DiscordPermissions.Administrator) && guildId == _settings.ServerId))
            {
                adminServer = true;
                break;
            }

            bool approvedDebug = guild.IsDebugAllowed || guildId == _settings.ServerId;
            List<DiscordEmbed> embeds = [];
            if (string.IsNullOrWhiteSpace(command))
            {
                foreach (KeyValuePair<string, List<AzzyHelpRecord>> kvp in AzzyHelp.GetAllCommands(context.Extension.Commands, adminServer, approvedDebug, member))
                {
                    if (embeds.Count == 10)
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

            await using DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbeds(embeds);

            await context.EditResponseAsync(messageBuilder);
        }

        [Command("stats")]
        public sealed class CoreStats(AzzyBotStatsRecord stats, ILogger<CoreStats> logger)
        {
            private readonly AzzyBotStatsRecord _stats = stats;
            private readonly ILogger<CoreStats> _logger = logger;

            [Command("hardware"), Description("Shows information about the hardware side of the bot.")]
            public async ValueTask HardwareStatsAsync(CommandContext context)
            {
                ArgumentNullException.ThrowIfNull(context, nameof(context));

                _logger.CommandRequested(nameof(HardwareStatsAsync), context.User.GlobalName);

                await context.DeferResponseAsync();

                Uri avaUrl = new(context.Client.CurrentUser.AvatarUrl);
                DiscordEmbed embed = await EmbedBuilder.BuildAzzyHardwareStatsEmbedAsync(avaUrl);

                await context.EditResponseAsync(embed);
            }

            [Command("info"), Description("Shows information about the bot and it's components.")]
            public async ValueTask InfoStatsAsync(CommandContext context)
            {
                ArgumentNullException.ThrowIfNull(context, nameof(context));

                _logger.CommandRequested(nameof(InfoStatsAsync), context.User.GlobalName);

                await context.DeferResponseAsync();

                Uri avaUrl = new(context.Client.CurrentUser.AvatarUrl);
                string dspVersion = context.Client.VersionString.Split('+')[0];
                DiscordEmbed embed = EmbedBuilder.BuildAzzyInfoStatsEmbed(avaUrl, dspVersion, _stats.Commit, _stats.CompilationDate, _stats.LocCs);

                await context.EditResponseAsync(embed);
            }

            [Command("ping"), Description("Ping the bot and get the latency to discord.")]
            public async ValueTask PingAsync(CommandContext context)
            {
                ArgumentNullException.ThrowIfNull(context, nameof(context));

                _logger.CommandRequested(nameof(PingAsync), context.User.GlobalName);

                await context.RespondAsync($"Pong! {context.Client.Ping}ms");
            }
        }
    }
}
