using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using Lavalink4NET.Rest.Entities.Tracks;

namespace AzzyBot.Bot.Commands.Choices;

public sealed class MusicStreamingPlatformProvider : IChoiceProvider
{
    private readonly IReadOnlyDictionary<string, object> _platformProvider = new Dictionary<string, object>(1)
    {
        [nameof(TrackSearchMode.SoundCloud)] = TrackSearchMode.SoundCloud.Prefix ?? string.Empty
    };

    public ValueTask<IReadOnlyDictionary<string, object>> ProvideAsync(CommandParameter parameter)
        => ValueTask.FromResult(_platformProvider);
}
