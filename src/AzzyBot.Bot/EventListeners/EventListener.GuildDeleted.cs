using System;
using System.Threading.Tasks;
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities;
using AzzyBot.Core.Logging;
using AzzyBot.Data;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;

namespace AzzyBot.Bot.EventListeners;

public static partial class EventListener
{
    public static async Task OnGuildDeletedAsync(DiscordClient c, GuildDeletedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(c, nameof(c));
        ArgumentNullException.ThrowIfNull(e, nameof(e));

        IServiceProvider sp = c.ServiceProvider;
        AzzyBotSettingsRecord settings = sp.GetRequiredService<AzzyBotSettingsRecord>();

        if (e.Guild.Id == settings.ServerId)
        {
            c.Logger.RemovedFromHomeGuild(settings.ServerId);
            Environment.Exit(0);

            return;
        }

        if (e.Unavailable)
        {
            c.Logger.GuildUnavailable(e.Guild.Name);
            return;
        }

        c.Logger.GuildDeleted(e.Guild.Name);

        DbActions dbActions = sp.GetRequiredService<DbActions>();
        DiscordBotService botService = sp.GetRequiredService<DiscordBotService>();

        await dbActions.DeleteGuildAsync(e.Guild.Id);

        DiscordEmbed embed = EmbedBuilder.BuildGuildRemovedEmbed(e.Guild.Id, e.Guild);
        await botService.SendMessageAsync(settings.NotificationChannelId, embeds: [embed]);
    }
}
