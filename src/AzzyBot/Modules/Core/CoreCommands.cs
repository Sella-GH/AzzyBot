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

            string userName = ctx.Client.CurrentUser.Username;
            string avatarUrl = ctx.Client.CurrentUser.AvatarUrl;
            DiscordMember member = ctx.Member;
            DiscordEmbed embed;

            if (string.IsNullOrWhiteSpace(command))
            {
                embed = CoreEmbedBuilder.BuildAzzyHelpEmbed(userName, avatarUrl, CoreAzzyHelp.GetCommandsAndDescriptions(member));
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            else
            {
                embed = CoreEmbedBuilder.BuildAzzyHelpCommandEmbed(userName, avatarUrl, CoreAzzyHelp.GetSingleCommandDetails(member, command));
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
        }
    }

    [SlashCommandGroup("core", "Core admin Commands")]
    [SlashRequireGuild]
    [RequireUserRole]
    internal sealed class CoreCommandGroup : ApplicationCommandModule
    {
        [SlashCommand("info", "Shows basic information about Azzy")]
        internal static async Task CoreInfoCommandAsync(InteractionContext ctx)
        {
            LoggerBase.LogInfo(LoggerBase.GetLogger, "CoreInfoCommand requsted", null);
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            string userName = ctx.Client.CurrentUser.Username;
            string avatarUrl = ctx.Client.CurrentUser.AvatarUrl;
            DiscordEmbed embed = await CoreEmbedBuilder.BuildInfoAzzyEmbedAsync(userName, avatarUrl);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("ping", "Pings Azzy and returns general information")]
        internal static async Task CorePingCommandAsync(InteractionContext ctx)
        {
            LoggerBase.LogInfo(LoggerBase.GetLogger, "CorePingCommand requested", null);
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            string userName = ctx.Client.CurrentUser.Username;
            string avatarUrl = ctx.Client.CurrentUser.AvatarUrl;
            DiscordEmbed embed = await CoreEmbedBuilder.BuildAzzyStatsEmbedAsync(userName, avatarUrl, ctx.Client.Ping);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
    }
}
