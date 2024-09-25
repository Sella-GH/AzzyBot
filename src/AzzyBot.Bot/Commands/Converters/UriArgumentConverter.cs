using System;
using System.Threading.Tasks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace AzzyBot.Bot.Commands.Converters;

public sealed class UriArgumentConverter : ISlashArgumentConverter<Uri>
{
    public DiscordApplicationCommandOptionType ParameterType
        => DiscordApplicationCommandOptionType.String;

    public string ReadableName
        => "Url";

    public Task<Optional<Uri>> ConvertAsync(InteractionConverterContext context, InteractionCreatedEventArgs eventArgs)
        => ConvertAsync(context?.Argument?.RawValue);

    public static Task<Optional<Uri>> ConvertAsync(string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        if (!value.Contains("https://", StringComparison.OrdinalIgnoreCase) && !value.Contains("http://", StringComparison.OrdinalIgnoreCase))
            value = $"http://{value}";

        return (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri? uri))
            ? Task.FromResult(Optional.FromValue(uri))
            : Task.FromResult(Optional.FromNoValue<Uri>());
    }
}
