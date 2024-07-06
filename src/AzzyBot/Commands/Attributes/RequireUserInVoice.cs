using System;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;

namespace AzzyBot.Commands.Attributes;

/// <summary>
/// Checks if the User is in a voice channel.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class RequireUserInVoice : SlashCheckBaseAttribute
{
    public override Task<bool> ExecuteChecksAsync(InteractionContext ctx) => Task.FromResult(ctx.Member.VoiceState is not null);
}
