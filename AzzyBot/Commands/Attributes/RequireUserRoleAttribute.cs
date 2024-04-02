using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzzyBot.Commands.Enums;
using AzzyBot.ExceptionHandling;
using AzzyBot.Modules.ClubManagement;
using AzzyBot.Modules.Core;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands.Attributes;

//
// EXTEND WITH FEATURES TO
// CHECK FOR DIFFERENT COMMANDS
//

/// <summary>
/// Attribute to require a user to have a specific role to execute a command.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class RequireUserRoleAttribute : SlashCheckBaseAttribute
{
    /// <summary>
    /// Executes checks to see if the user has the required role to execute a command.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Throws when the command name does not exist in <seealso cref="CommandsEnum"/>.</exception>
    /// <exception cref="InvalidOperationException">Throws when there is no role found which matches the required role.</exception>
    public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
    {
        if (ctx.Member is null)
            return Task.FromResult(false);

        List<ulong> allowedRoles = [];

        switch (ctx.CommandName)
        {
            case string value1 when value1.Equals(nameof(CommandsEnum.azuracast), StringComparison.OrdinalIgnoreCase):
            case string value2 when value2.Equals(nameof(CommandsEnum.core), StringComparison.OrdinalIgnoreCase):
                allowedRoles.Add(CoreSettings.AdminRoleId);
                break;

            case string value when value.Equals(nameof(CommandsEnum.staff), StringComparison.OrdinalIgnoreCase):
                allowedRoles.Add(CoreSettings.AdminRoleId);
                allowedRoles.Add(ClubManagementSettings.StaffRoleId);
                allowedRoles.Add(ClubManagementSettings.CloserRoleId);
                break;

            default:
                throw new ArgumentOutOfRangeException(ctx.CommandName);
        }

        if (allowedRoles.Count == 0)
            throw new InvalidOperationException($"{nameof(allowedRoles)} is null!");

        foreach (DiscordRole role in ctx.Member.Roles)
        {
            if (allowedRoles.Contains(role.Id))
                return Task.FromResult(true);
        }

        ExceptionHandler.LogMessage(LogLevel.Warning, $"User **{ctx.User.Username}** is not allowed to use the command **{ctx.QualifiedName}** !");
        return Task.FromResult(false);
    }
}
