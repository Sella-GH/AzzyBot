using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Commands.Choices;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

internal sealed class ConfigCommands
{
    [Command("config")]
    internal sealed class Config(IDbContextFactory<AzzyDbContext> dbContextFactory, ILogger<Config> logger)
    {
        private readonly IDbContextFactory<AzzyDbContext> _dbContextFactory = dbContextFactory;
        private readonly ILogger<Config> _logger = logger;

        [Command("set")]
        public async ValueTask ConfigSetAsync(SlashCommandContext context)
        {
            _logger.CommandRequested(nameof(ConfigSetAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            await using AzzyDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            GuildsEntity guild = dbContext.Guilds.Where(g => g.UniqueId == context.Guild!.Id).First();
            AzuraCastEntity azuraCast = dbContext.AzuraCast.Where(a => a.GuildId == guild!.Id).First();
            AzuraCastChecksEntity azuraCastChecks = dbContext.AzuraCastChecks.Where(c => c.AzuraCastId == azuraCast!.Id).First();

            bool azuraCastApiKey = !string.IsNullOrWhiteSpace(azuraCast.ApiKey);
            bool azuraCastApiUrl = !string.IsNullOrWhiteSpace(azuraCast.ApiUrl);
            bool azuraStationId = azuraCast.StationId is not 0;
            DiscordButtonComponent apiKeyButton = new((azuraCastApiKey) ? DiscordButtonStyle.Success : DiscordButtonStyle.Danger, "btn_azuracast_api_key", "Add or change the AzuraCast api key");
            DiscordButtonComponent apiUrlButton = new((azuraCastApiUrl) ? DiscordButtonStyle.Success : DiscordButtonStyle.Danger, "btn_azuracast_api_url", "Add or change the AzuraCast api url");
            DiscordButtonComponent stationIdButton = new((azuraStationId) ? DiscordButtonStyle.Success : DiscordButtonStyle.Danger, "btn_azuracast_station_id", "Add or change the AzuraCast station id");
            DiscordChannelSelectComponent requestsChannel = new("azura_requests_channel", "Select the Music Requests Channel", [DiscordChannelType.Text]);
            DiscordChannelSelectComponent outagesChannel = new("azura_outages_channel", "Select the Outages channel", [DiscordChannelType.Text]);

            await using DiscordMessageBuilder builder = new();
            builder.WithContent("Please fill out all the following options.");
            builder.AddComponents(requestsChannel);
            builder.AddComponents(outagesChannel);

            await context.EditResponseAsync(builder);
        }
    }
}
