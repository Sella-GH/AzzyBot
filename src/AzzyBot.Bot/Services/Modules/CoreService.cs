using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AzzyBot.Bot.Utilities.Helpers;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.Modules;

public sealed class CoreService(ILogger<CoreService> logger, DbActions dbActions, DiscordBotService botService)
{
    private readonly ILogger<CoreService> _logger = logger;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    public async Task<Dictionary<GuildEntity, string>> CheckUnusedGuildsAsync()
    {
        IEnumerable<GuildEntity> guilds = await _dbActions.ReadGuildsAsync(loadGuildPrefs: true);
        if (!guilds.Any())
            return [];

        HashSet<GuildEntity> noLegals = [.. guilds.Where(g => !g.LegalsAccepted)];
        HashSet<GuildEntity> noConfig = [.. guilds.Where(g => !g.ConfigSet)];
        HashSet<GuildEntity> allCombined = [.. noConfig.Union(noLegals)];

        const int legalThresholdDays = 3;
        const int configThresholdDays = 7;
        Dictionary<GuildEntity, string> victims = [];
        StringBuilder sb = new();
        foreach (GuildEntity guild in allCombined)
        {
            int timeframeDays = 0;
            sb.AppendLine(GeneralStrings.ReminderBegin);

            if (noLegals.Contains(guild))
            {
                timeframeDays = legalThresholdDays;
                sb.Append(GeneralStrings.ReminderLegals);
                sb.Append(' ');
                sb.AppendLine(GeneralStrings.ReminderLegalsFix);
            }

            if (noConfig.Contains(guild))
            {
                // Legals have priority
                if (timeframeDays is 0)
                    timeframeDays = configThresholdDays;

                sb.Append(GeneralStrings.ReminderConfig);
                sb.Append(' ');
                sb.AppendLine(GeneralStrings.ReminderConfigFix);
            }

            sb.AppendLine(GeneralStrings.ReminderForceLeaveThreat.Replace("{%TIMEFRAME%}", timeframeDays.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCulture));

            victims.Add(guild, sb.ToString());
            sb.Clear();
        }

        return victims;
    }

    public async Task NotifyUnusedGuildsAsync(Dictionary<GuildEntity, string> guilds)
    {
        ArgumentNullException.ThrowIfNull(guilds);

        if (guilds.Count is 0)
            return;

        foreach ((GuildEntity guild, string message) in guilds)
        {
            bool result = await _botService.SendMessageAsync(guild.Preferences.AdminNotifyChannelId, message);
            if (!result)
                result = await _botService.SendMessageToOwnerAsync(guild.UniqueId, message);

            if (!result)
            {
                _logger.LogError($"Unable to notify admins or owner of unused guild {guild.UniqueId}");
                continue;
            }

            await _dbActions.UpdateGuildAsync(guild.UniqueId, lastReminder: true);
        }
    }
}
