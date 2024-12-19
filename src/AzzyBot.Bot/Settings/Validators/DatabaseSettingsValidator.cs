using AzzyBot.Data.Settings;
using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Settings.Validators;

[OptionsValidator]
public sealed partial class DatabaseSettingsValidator : IValidateOptions<AppDatabaseSettings>
{
}
