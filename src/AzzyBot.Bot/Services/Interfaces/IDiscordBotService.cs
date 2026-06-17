using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AzzyBot.Data.Entities;

using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace AzzyBot.Bot.Services.Interfaces;

public interface IDiscordBotService
{
    Task<bool> CheckChannelPermissionsAsync(DiscordMember member, ulong channelId, DiscordPermissions permissions);
    Task CheckPermissionsAsync(DiscordGuild guild, ulong[] channelIds);
    Task CheckPermissionsAsync(GuildEntity guildEntity);
    Task CheckPermissionsAsync(IReadOnlyList<GuildEntity> guilds);
    Task<DiscordChannel?> GetDiscordChannelAsync(ulong channelId);
    DiscordGuild? GetDiscordGuild(ulong guildId = 0);
    IReadOnlyDictionary<ulong, DiscordGuild> GetDiscordGuilds { get; }
    Task<DiscordMember?> GetDiscordMemberAsync(ulong guildId, ulong userId = 0);
    Task LogExceptionAsync(Exception ex, DateTimeOffset timestamp, SlashCommandContext? ctx = null, string? jsonMessage = null);
    Task RespondToChecksExceptionAsync(ChecksFailedException ex, SlashCommandContext context);
    Task<bool> SendMessageAsync(ulong channelId, string? content = null, IReadOnlyList<DiscordEmbed>? embeds = null, IReadOnlyList<string>? filePaths = null, IMention[]? mentions = null);
    Task<bool> SendMessageToOwnerAsync(ulong guildId, string? content = null, IReadOnlyList<DiscordEmbed>? embeds = null, IReadOnlyList<string>? filePaths = null);
    Task SetBotStatusAsync(int status, int type, string doing, Uri? url = null, bool reset = false);
    DiscordActivity SetBotStatusActivity(int type, string doing, Uri? url);
    DiscordUserStatus SetBotStatusUserStatus(int status);
}
