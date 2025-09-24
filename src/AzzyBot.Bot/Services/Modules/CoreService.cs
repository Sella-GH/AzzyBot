using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Helpers;
using AzzyBot.Bot.Utilities.Structs;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Services.Modules;

public sealed class CoreService(ILogger<CoreService> logger, IOptions<AzzyBotSettings> settings, DbActions dbActions, DiscordBotService botService)
{
    private readonly ILogger<CoreService> _logger = logger;
    private readonly AzzyBotSettings _settings = settings.Value;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    public async Task<IReadOnlyDictionary<GuildEntity, AzzyInactiveGuildStruct>> CheckUnusedGuildsAsync()
    {
        _logger.LogWarning("Checking for unused guilds...");

        IEnumerable<GuildEntity> guilds = await _dbActions.ReadGuildsAsync(loadGuildPrefs: true);
        if (!guilds.Any())
        {
            _logger.LogError("No guilds found in database.");
            return new Dictionary<GuildEntity, AzzyInactiveGuildStruct>();
        }

        HashSet<GuildEntity> noLegals = [.. guilds.Where(g => !g.LegalsAccepted && g.UniqueId != _settings.ServerId)];
        HashSet<GuildEntity> noConfig = [.. guilds.Where(g => !g.ConfigSet && g.UniqueId != _settings.ServerId)];
        HashSet<GuildEntity> allCombined = [.. noConfig.Union(noLegals)];

        _logger.LogWarning($"Found {noLegals.Count} guilds without legals accepted.");
        _logger.LogWarning($"Found {noConfig.Count} guilds without config set.");
        _logger.LogWarning($"Found {allCombined.Count} guilds without legals or config.");

        Dictionary<GuildEntity, AzzyInactiveGuildStruct> victims = [];
        foreach (GuildEntity guild in allCombined)
        {
            DiscordGuild? dGuild = _botService.GetDiscordGuild(guild.UniqueId);
            if (dGuild is null)
            {
                _logger.LogError($"Guild {guild.UniqueId} not found in Discord client. Skipping...");
                continue;
            }

            AzzyInactiveGuildStruct guildStruct = new(guild: dGuild, config: noConfig.Contains(guild), legals: noLegals.Contains(guild));
            victims.Add(guild, guildStruct);

            _logger.LogWarning($"Guild {guild.UniqueId} marked as victim (NoConfig: {guildStruct.NoConfig}, NoLegals: {guildStruct.NoLegals})");
        }

        await _dbActions.UpdateAzzyBotAsync(lastGuildReminder: true);

        return victims;
    }

    public async Task DeleteUnusedGuildsAsync(IReadOnlyDictionary<GuildEntity, AzzyInactiveGuildStruct> guilds)
    {
        ArgumentNullException.ThrowIfNull(guilds);
        ArgumentOutOfRangeException.ThrowIfLessThan(guilds.Count, 1);

        StringBuilder sb = new();
        foreach (KeyValuePair<GuildEntity, AzzyInactiveGuildStruct> guild in guilds)
        {
            sb.AppendLine(GeneralStrings.ReminderForceLeaveAnnouncement);
            if (guild.Value.NoLegals)
                sb.AppendLine(GeneralStrings.ReminderLegals);

            if (guild.Value.NoConfig)
                sb.AppendLine(GeneralStrings.ReminderConfig);

            sb.AppendLine(GeneralStrings.ReminderForceLeaveEnd);

            bool result = false;
            if (guild.Key.Preferences.AdminNotifyChannelId is not 0)
            {
                result = await _botService.SendMessageAsync(guild.Key.Preferences.AdminNotifyChannelId, content: sb.ToString());
                _logger.LogWarning($"Sent unused guild deletion notification to admin channel {guild.Key.Preferences.AdminNotifyChannelId} in guild {guild.Key.UniqueId}.");
            }

            if (!result)
            {
                result = await _botService.SendMessageToOwnerAsync(guild.Key.UniqueId, content: sb.ToString());
                _logger.LogWarning($"Sent unused guild deletion notification to owner of guild {guild.Key.UniqueId}.");
            }

            if (!result)
            {
                _logger.LogError($"Unable to notify admins or owner of unused guild about leaving: {guild.Value.Guild.Name} ({guild.Value.Guild.Id})");
                continue;
            }

            await guild.Value.Guild.LeaveAsync();
            sb.Clear();

            _logger.LogWarning($"Deleted unused guild {guild.Value.Guild.Name} ({guild.Value.Guild.Id}) from database.");
        }
    }

    [SuppressMessage("Style", "IDE0045:Convert to conditional expression", Justification = "We do not nest ternary expressions.")]
    public async Task NotifyUnusedGuildsAsync(IReadOnlyDictionary<GuildEntity, AzzyInactiveGuildStruct> guilds)
    {
        ArgumentNullException.ThrowIfNull(guilds);
        ArgumentOutOfRangeException.ThrowIfLessThan(guilds.Count, 1);

        foreach (KeyValuePair<GuildEntity, AzzyInactiveGuildStruct> guild in guilds)
        {
            DateTimeOffset leaveDate;
            if (guild.Key.ReminderLeaveDate != DateTimeOffset.MinValue)
            {
                leaveDate = guild.Key.ReminderLeaveDate;
            }
            else
            {
                leaveDate = (guild.Value.NoLegals) ? DateTimeOffset.UtcNow.AddDays(3) : DateTimeOffset.UtcNow.AddDays(7);
            }

            DiscordEmbed embed = EmbedBuilder.BuildAzzyInactiveGuildEmbed(guild.Value.NoConfig, guild.Value.NoLegals, guild.Value.Guild, leaveDate);

            bool result = false;
            if (guild.Key.Preferences.AdminNotifyChannelId is not 0)
            {
                result = await _botService.SendMessageAsync(guild.Key.Preferences.AdminNotifyChannelId, embeds: [embed]);
                _logger.LogWarning($"Sent unused guild notification to admin channel {guild.Key.Preferences.AdminNotifyChannelId} in guild {guild.Key.UniqueId}.");
            }

            if (!result)
            {
                result = await _botService.SendMessageToOwnerAsync(guild.Key.UniqueId, embeds: [embed]);
                _logger.LogWarning($"Sent unused guild notification to owner of guild {guild.Key.UniqueId}.");
            }

            if (!result)
            {
                _logger.LogError($"Unable to notify admins or owner of unused guild {guild.Key.UniqueId}");
                continue;
            }

            await _dbActions.UpdateGuildAsync(guild.Key.UniqueId, reminderLeaveDate: leaveDate);
            _logger.LogWarning($"Notified guild {guild.Key.UniqueId} of being unused.");
        }
    }
}
