using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Settings.Validators;

[OptionsValidator]
public sealed partial class DiscordStatusSettingsValidator : IValidateOptions<DiscordStatusSettings>
{
}
