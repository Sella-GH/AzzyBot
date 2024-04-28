using System;
using System.Threading.Tasks;
using AzzyBot.Modules.AzuraCast.Settings;
using DSharpPlus.SlashCommands;

namespace AzzyBot.Commands.Attributes;

/// <summary>
/// Checks if the AzuraCast API key is valid or not.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class RequireAzuraApiKeyValid : SlashCheckBaseAttribute
{
    public override Task<bool> ExecuteChecksAsync(InteractionContext ctx) => Task.FromResult(AcSettings.AzuraCastApiKeyIsValid);
}
