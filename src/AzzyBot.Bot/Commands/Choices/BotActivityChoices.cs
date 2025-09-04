using AzzyBot.Bot.Utilities.Attributes;

namespace AzzyBot.Bot.Commands.Choices;

[DiscordChoiceProvider]
public static class BotActivityChoices
{
    public static readonly (string Name, int Value)[] Choices =
    [
        ("Playing", 0),
        ("Streaming", 1),
        ("Listening To", 2),
        ("Watching", 3),
        ("Competing", 5)
    ];
}