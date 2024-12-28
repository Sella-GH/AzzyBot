using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using AzzyBot.Bot.Utilities.Records.AzuraCast;

namespace AzzyBot.Bot.Utilities;

[SuppressMessage("Roslynator", "RCS1251:Remove unnecessary braces from record declaration", Justification = "Class has to be empty.")]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(IEnumerable<AzuraFilesRecord>))] // Generic
[JsonSerializable(typeof(IEnumerable<AzuraFilesDetailedRecord>))] // Generic
[JsonSerializable(typeof(IEnumerable<AzuraHlsMountRecord>))] // Generic
[JsonSerializable(typeof(IEnumerable<AzuraPlaylistRecord>))] // Generic
[JsonSerializable(typeof(IEnumerable<AzuraRequestRecord>))] // Generic
[JsonSerializable(typeof(IEnumerable<AzuraRequestQueueItemRecord>))] // Generic
[JsonSerializable(typeof(IEnumerable<AzuraMediaItemRecord>))] // Generic
[JsonSerializable(typeof(IEnumerable<AzuraStationHistoryItemRecord>))] // Generic
[JsonSerializable(typeof(IEnumerable<AzuraStationListenerRecord>))] // Generic
[JsonSerializable(typeof(IEnumerable<AzuraStationQueueItemDetailedRecord>))] // Generic
public sealed partial class JsonDeserializationListSourceGen : JsonSerializerContext
{
}
