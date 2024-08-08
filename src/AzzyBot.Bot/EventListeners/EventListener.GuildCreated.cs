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
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.EventListeners;

public static partial class EventListener
{
    private const string NewGuildText = "Thank you for adding me to your server **%GUILD%**! Before you can make good use of me, you have to set my settings first.\n\nPlease use the command `config modify-core` for this.\nOnly administrators are able to execute this command right now.";

    public static async Task OnGuildCreatedAsync(DiscordClient c, GuildCreatedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(c, nameof(c));
        ArgumentNullException.ThrowIfNull(e, nameof(e));

        IServiceProvider sp = c.ServiceProvider;
        ILogger logger = sp.GetRequiredService<ILogger>();
        AzzyBotSettingsRecord settings = sp.GetRequiredService<AzzyBotSettingsRecord>();
        DiscordBotService botService = sp.GetRequiredService<DiscordBotService>();
        DbActions dbActions = sp.GetRequiredService<DbActions>();

        logger.GuildCreated(e.Guild.Name);

        await dbActions.AddGuildAsync(e.Guild.Id);
        DiscordMember owner = await e.Guild.GetGuildOwnerAsync();
        await owner.SendMessageAsync(NewGuildText.Replace("%GUILD%", e.Guild.Name, StringComparison.OrdinalIgnoreCase));

        DiscordEmbed embed = await EmbedBuilder.BuildGuildAddedEmbedAsync(e.Guild);
        await botService.SendMessageAsync(settings.NotificationChannelId, embeds: [embed]);
    }
}
