using System.Diagnostics.CodeAnalysis;
using AzzyBot.Data.Settings;
using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Settings.Validators;

[SuppressMessage("Roslynator", "RCS1251:Remove unnecessary braces from record declaration", Justification = "Class has to be empty.")]
[OptionsValidator]
public sealed partial class DatabaseSettingsValidator : IValidateOptions<DatabaseSettings>
{
}
