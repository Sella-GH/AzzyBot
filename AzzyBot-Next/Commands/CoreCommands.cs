using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;

namespace AzzyBot.Commands;

internal sealed class CoreCommands
{
    [Command("core")]
    [RequireGuild]
    internal sealed class Core
    {
        //[Command("info")]
        //public static async ValueTask CoreInfoAsync(CommandContext context)
        //{
        //    await context.DeferResponseAsync();
        //}

        [Command("ping")]
        public static ValueTask CorePingAsync(CommandContext context) => context.RespondAsync($"Pong! {context.Client.Ping}ms");
    }
}
