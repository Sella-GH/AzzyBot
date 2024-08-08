using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities;
using AzzyBot.Core.Logging;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.EventListeners;

public static partial class EventListener
{
    public static async Task OnGuildDownloadCompletedAsync(DiscordClient c, GuildDownloadCompletedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(c, nameof(c));
        ArgumentNullException.ThrowIfNull(e, nameof(e));

        IServiceProvider sp = c.ServiceProvider;
        ILogger logger = sp.GetRequiredService<ILogger>();
        AzzyBotSettingsRecord settings = sp.GetRequiredService<AzzyBotSettingsRecord>();

        if (!e.Guilds.ContainsKey(settings.ServerId))
        {
            logger.NotInHomeGuild(settings.ServerId);
            Environment.Exit(0);

            return;
        }

        DbActions dbActions = sp.GetRequiredService<DbActions>();
        DiscordBotService botService = sp.GetRequiredService<DiscordBotService>();

        DiscordEmbed embed;
        IEnumerable<DiscordGuild> addedGuilds = await dbActions.AddGuildsAsync(e.Guilds);
        if (addedGuilds.Any())
        {
            foreach (DiscordGuild guild in addedGuilds)
            {
                DiscordMember owner = await guild.GetGuildOwnerAsync();
                await owner.SendMessageAsync(NewGuildText.Replace("%GUILD%", guild.Name, StringComparison.OrdinalIgnoreCase));
                embed = await EmbedBuilder.BuildGuildAddedEmbedAsync(guild);
                await botService.SendMessageAsync(settings.ServerId, embeds: [embed]);
            }
        }

        IEnumerable<ulong> removedGuilds = await dbActions.DeleteGuildsAsync(e.Guilds);
        if (removedGuilds.Any())
        {
            foreach (ulong guild in removedGuilds)
            {
                embed = EmbedBuilder.BuildGuildRemovedEmbed(guild);
                await botService.SendMessageAsync(settings.ServerId, embeds: [embed]);
            }
        }

        IAsyncEnumerable<GuildEntity> guilds = dbActions.GetGuildsAsync(loadEverything: true);
        await botService.CheckPermissionsAsync(guilds);
    }
}
