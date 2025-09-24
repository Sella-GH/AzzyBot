using System;
using System.Threading.Tasks;

using DSharpPlus.Commands.Converters;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace AzzyBot.Bot.Commands.Converters;

public sealed class UriArgumentConverter : ISlashArgumentConverter<Uri>
{
    public DiscordApplicationCommandOptionType ParameterType
        => DiscordApplicationCommandOptionType.String;

    public string ReadableName
        => "Url";

    public Task<Optional<Uri>> ConvertAsync(ConverterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? value = context.Argument?.ToString();

        if (string.IsNullOrWhiteSpace(value))
            return Task.FromResult(Optional.FromNoValue<Uri>());

        if (!value.Contains("https://", StringComparison.OrdinalIgnoreCase) && !value.Contains("http://", StringComparison.OrdinalIgnoreCase))
            value = $"https://{value}";

        return (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri? uri))
            ? Task.FromResult(Optional.FromValue(uri))
            : Task.FromResult(Optional.FromNoValue<Uri>());
    }
}
