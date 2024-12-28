using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using AzzyBot.Bot.Utilities.Records.AzuraCast;

namespace AzzyBot.Bot.Utilities;

[SuppressMessage("Roslynator", "RCS1251:Remove unnecessary braces from record declaration", Justification = "Class has to be empty.")]
[JsonSourceGenerationOptions(WriteIndented = true, GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(IEnumerable<AzuraFilesRecord>))]
public sealed partial class JsonSerializationListSourceGen : JsonSerializerContext
{
}
