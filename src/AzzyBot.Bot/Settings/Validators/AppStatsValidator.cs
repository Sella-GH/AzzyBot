using AzzyBot.Core.Utilities.Records;
using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Settings.Validators;

[OptionsValidator]
public sealed partial class AppStatsValidator : IValidateOptions<AppStats>
{
}
