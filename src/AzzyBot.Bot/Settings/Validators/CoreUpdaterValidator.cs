using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Settings.Validators;

[OptionsValidator]
public sealed partial class CoreUpdaterValidator : IValidateOptions<CoreUpdaterSettings>
{
}
