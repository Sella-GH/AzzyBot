using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;
using Lavalink4NET.Rest.Entities.Tracks;

namespace AzzyBot.Bot.Commands.Choices;

public sealed class MusicStreamingPlatformProvider : IChoiceProvider
{
    private static readonly IEnumerable<DiscordApplicationCommandOptionChoice> Platforms =
    [
        new(nameof(TrackSearchMode.SoundCloud), TrackSearchMode.SoundCloud.Prefix ?? string.Empty)
    ];

    public ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
    => ValueTask.FromResult(Platforms);
}
