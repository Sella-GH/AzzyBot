using System.Threading.Tasks;
using AzzyBot.Commands.Attributes;
using AzzyBot.Logging;
using AzzyBot.Modules.Core.Autocomplete;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace AzzyBot.Modules.Core;

internal sealed class CoreCommands : ApplicationCommandModule
{
    [SlashCommandGroup("azzy", "General commands for the bot")]
    [SlashRequireGuild]
    internal sealed class AzzyCommandGroup : ApplicationCommandModule
    {
        [SlashCommand("help", "Shows you all available commands with their descriptions and the options")]
        internal static async Task AzzyHelpCommandAsync(InteractionContext ctx, [Autocomplete(typeof(AzzyHelpAutocomplete))][Option("command", "Shows a detailed overview of the selected command")] string command = "")
        {
            LoggerBase.LogInfo(LoggerBase.GetLogger, "AzzyHelpCommand requsted", null);
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (string.IsNullOrWhiteSpace(command))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(CoreEmbedBuilder.BuildAzzyHelpEmbed(ctx.Client.CurrentUser.Username, ctx.Client.CurrentUser.AvatarUrl, CoreAzzyHelp.GetCommandsAndDescriptions(ctx.Member))));
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(CoreEmbedBuilder.BuildAzzyHelpCommandEmbed(ctx.Client.CurrentUser.Username, ctx.Client.CurrentUser.AvatarUrl, CoreAzzyHelp.GetSingleCommandDetails(ctx.Member, command))));
            }
        }
    }

    [SlashCommandGroup("core", "Core admin Commands")]
    [SlashRequireGuild]
    [RequireUserRole]
    internal sealed class CoreCommandGroup : ApplicationCommandModule
    {
        [SlashCommandGroup("info", "Info commands")]
        internal sealed class CoreInfoCommandGroup : ApplicationCommandModule
        {
            [SlashCommand("azzy", "Shows basic information about Azzy")]
            internal static async Task CoreInfoAzzyCommandAsync(InteractionContext ctx)
            {
                LoggerBase.LogInfo(LoggerBase.GetLogger, "CoreInfoAzzyCommand requsted", null);
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(await CoreEmbedBuilder.BuildInfoAzzyEmbedAsync(ctx.Client.CurrentUser.Username, ctx.Client.CurrentUser.AvatarUrl)));
            }
        }

        [SlashCommandGroup("ping", "Ping commands")]
        internal sealed class CorePingCommandGroup : ApplicationCommandModule
        {
            [SlashCommand("azzy", "Pings Azzy and returns general information")]
            internal static async Task CorePingAzzyCommandAsync(InteractionContext ctx)
            {
                LoggerBase.LogInfo(LoggerBase.GetLogger, "CorePingAzzyCommand requested", null);
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(await CoreEmbedBuilder.BuildAzzyStatsEmbedAsync(ctx.Client.CurrentUser.Username, ctx.Client.CurrentUser.AvatarUrl, ctx.Client.Ping)));
            }
        }
    }
}
