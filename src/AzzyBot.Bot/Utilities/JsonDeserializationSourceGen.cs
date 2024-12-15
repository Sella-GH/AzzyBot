using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using AzzyBot.Bot.Utilities.Records;
using AzzyBot.Bot.Utilities.Records.AzuraCast;

namespace AzzyBot.Bot.Utilities;

[SuppressMessage("Roslynator", "RCS1251:Remove unnecessary braces from record declaration", Justification = "Class has to be empty.")]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(AzuraAdminStationConfigRecord))] // Generic
[JsonSerializable(typeof(AzuraFilesRecord))]
[JsonSerializable(typeof(AzuraFilesDetailedRecord))] // Generic
[JsonSerializable(typeof(AzuraHardwareStatsRecord))] // Generic
[JsonSerializable(typeof(AzuraHlsMountRecord))]
[JsonSerializable(typeof(AzuraNowPlayingDataRecord))] // Generic
[JsonSerializable(typeof(AzuraPlaylistRecord))] // Generic
[JsonSerializable(typeof(AzuraRequestRecord))]
[JsonSerializable(typeof(AzuraRequestQueueItemRecord))]
[JsonSerializable(typeof(AzuraSongDataRecord))]
[JsonSerializable(typeof(AzuraMediaItemRecord))]
[JsonSerializable(typeof(AzuraStationHistoryItemRecord))]
[JsonSerializable(typeof(AzuraStationListenerRecord))]
[JsonSerializable(typeof(AzuraStationQueueItemDetailedRecord))]
[JsonSerializable(typeof(AzuraStationRecord))] // Generic
[JsonSerializable(typeof(AzuraStationStatusRecord))] // Generic
[JsonSerializable(typeof(AzuraStatusRecord))] // Generic
[JsonSerializable(typeof(AzuraSystemLogsRecord))] // Generic
[JsonSerializable(typeof(AzuraSystemLogRecord))] // Generic
[JsonSerializable(typeof(AzuraUpdateRecord))]
[JsonSerializable(typeof(AzuraUpdateErrorRecord))]
[JsonSerializable(typeof(List<AzzyUpdateRecord>))]
public sealed partial class JsonDeserializationSourceGen : JsonSerializerContext
{
}
