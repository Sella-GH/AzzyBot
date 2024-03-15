using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace AzzyBot.Modules.Core;

internal static class CoreDiscordCommands
{
    internal static bool CheckIfUserHasRole(DiscordMember member, ulong roleId)
    {
        ArgumentNullException.ThrowIfNull(nameof(member));

        foreach (DiscordRole role in member.Roles)
        {
            if (role.Id == roleId)
                return true;
        }

        return false;
    }

    internal static async Task RemoveUserRoleAsync(DiscordMember user, ulong roleId)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        foreach (DiscordRole role in user.Roles)
        {
            if (role.Id == roleId)
                await user.RevokeRoleAsync(role);
        }
    }

    internal static string GetBestUsername(string discordName, string serverName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(discordName, nameof(discordName));

        // Return discordName if the serverName is not given
        // otherwise return serverName
        return (string.IsNullOrWhiteSpace(serverName)) ? discordName : serverName;
    }

    internal static bool CheckUserId(ulong checkUserId, ulong againstUserId) => checkUserId == againstUserId;
    internal static bool CheckIfUserIsInVoiceChannel(DiscordMember member) => member.VoiceState is not null;

    internal static bool CheckIfBotIsInVoiceChannel(DiscordMember member, ulong botId)
    {
        foreach (DiscordMember channelMember in (IReadOnlyList<DiscordMember>)member.VoiceState.Channel.Users)
        {
            if (channelMember.Id == botId)
                return true;
        }

        return false;
    }

    internal static Task<DiscordMember> GetMemberAsync(ulong userId, DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));

        return guild.GetMemberAsync(userId);
    }

    internal static DiscordRole GetRole(ulong roleId, DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));

        return guild.GetRole(roleId);
    }
}
