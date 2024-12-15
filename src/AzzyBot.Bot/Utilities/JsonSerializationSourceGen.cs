using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using AzzyBot.Bot.Utilities.Records;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Utilities.Records;

namespace AzzyBot.Bot.Utilities;

[SuppressMessage("Roslynator", "RCS1251:Remove unnecessary braces from record declaration", Justification = "Class has to be empty.")]
[JsonSourceGenerationOptions(WriteIndented = true, GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(AzuraAdminStationConfigRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraFilesRecord>))]
[JsonSerializable(typeof(AzuraFileUploadRecord))]
[JsonSerializable(typeof(AzuraInternalRequestRecord))]
[JsonSerializable(typeof(AzzyUpdateRecord))]
[JsonSerializable(typeof(SerializableExceptionsRecord))]
public sealed partial class JsonSerializationSourceGen : JsonSerializerContext
{
}
