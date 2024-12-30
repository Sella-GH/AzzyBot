using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Utilities.Records;

namespace AzzyBot.Bot.Utilities;

[SuppressMessage("Roslynator", "RCS1251:Remove unnecessary braces from record declaration", Justification = "Class has to be empty.")]
[JsonSourceGenerationOptions(WriteIndented = true, GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(AppSettingsRecord))]
[JsonSerializable(typeof(AzuraAdminStationConfigRecord))]
[JsonSerializable(typeof(AzuraFileUploadRecord))]
[JsonSerializable(typeof(AzuraInternalRequestRecord))]
[JsonSerializable(typeof(SerializableExceptionsRecord))]
public sealed partial class JsonSerializationSourceGen : JsonSerializerContext
{
}
