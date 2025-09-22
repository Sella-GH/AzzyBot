using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Structs;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.Modules;

public sealed class CoreService(ILogger<CoreService> logger, DbActions dbActions, DiscordBotService botService)
{
    private readonly ILogger<CoreService> _logger = logger;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    public async Task<Dictionary<GuildEntity, AzzyInactiveGuildStruct>> CheckUnusedGuildsAsync()
    {
        IEnumerable<GuildEntity> guilds = await _dbActions.ReadGuildsAsync(loadGuildPrefs: true);
        if (!guilds.Any())
            return [];

        HashSet<GuildEntity> noLegals = [.. guilds.Where(g => !g.LegalsAccepted)];
        HashSet<GuildEntity> noConfig = [.. guilds.Where(g => !g.ConfigSet)];
        HashSet<GuildEntity> allCombined = [.. noConfig.Union(noLegals)];

        Dictionary<GuildEntity, AzzyInactiveGuildStruct> victims = [];
        foreach (GuildEntity guild in allCombined)
        {
            DiscordGuild? dGuild = _botService.GetDiscordGuild(guild.UniqueId);
            if (dGuild is null)
                continue;

            AzzyInactiveGuildStruct guildStruct = new(guild: dGuild, config: noConfig.Contains(guild), legals: noLegals.Contains(guild));
            victims.Add(guild, guildStruct);
        }

        return victims;
    }

    public async Task NotifyUnusedGuildsAsync(Dictionary<GuildEntity, AzzyInactiveGuildStruct> guilds)
    {
        ArgumentNullException.ThrowIfNull(guilds);
        ArgumentOutOfRangeException.ThrowIfLessThan(guilds.Count, 1);

        foreach (KeyValuePair<GuildEntity, AzzyInactiveGuildStruct> guild in guilds)
        {
            DiscordEmbed embed = EmbedBuilder.BuildAzzyInactiveGuildEmbed(guild.Value.NoConfig, guild.Value.NoLegals, guild.Value.Guild);

            bool result = false;
            if (guild.Key.Preferences.AdminNotifyChannelId is not 0)
                result = await _botService.SendMessageAsync(guild.Key.Preferences.AdminNotifyChannelId, embeds: [embed]);

            if (!result)
                result = await _botService.SendMessageToOwnerAsync(guild.Key.UniqueId, embeds: [embed]);

            if (!result)
            {
                _logger.LogError($"Unable to notify admins or owner of unused guild {guild.Key.UniqueId}");
                continue;
            }

            await _dbActions.UpdateGuildAsync(guild.Key.UniqueId, lastReminder: true);
            _logger.LogWarning($"Notified guild {guild.Key.UniqueId} of being unused.");
        }
    }
}
