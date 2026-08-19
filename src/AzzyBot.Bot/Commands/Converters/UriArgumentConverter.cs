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

    public async Task<Optional<Uri>> ConvertAsync(ConverterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? value = context.Argument?.ToString();

        if (string.IsNullOrWhiteSpace(value))
            return Optional.FromNoValue<Uri>();

        if (!value.Contains("https://", StringComparison.OrdinalIgnoreCase) && !value.Contains("http://", StringComparison.OrdinalIgnoreCase))
            value = $"https://{value}";

        return (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri? uri))
            ? Optional.FromValue(uri)
            : Optional.FromNoValue<Uri>();
    }
}
