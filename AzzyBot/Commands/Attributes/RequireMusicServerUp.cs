using System;
using System.Threading.Tasks;
using AzzyBot.Modules.AzuraCast;
using DSharpPlus.SlashCommands;

namespace AzzyBot.Commands.Attributes;

/// <summary>
/// Checks if the music server is online or not.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class RequireMusicServerUp : SlashCheckBaseAttribute
{
    public override Task<bool> ExecuteChecksAsync(InteractionContext ctx) => AzuraCastModule.CheckIfMusicServerIsOnlineAsync();
}
