using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities.Records;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Utilities.Records;

namespace AzzyBot.Bot.Utilities;

[SuppressMessage("Roslynator", "RCS1251:Remove unnecessary braces from record declaration", Justification = "Class has to be empty.")]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default, WriteIndented = true)]
// Serialization
[JsonSerializable(typeof(AppSettingsRecord))]
[JsonSerializable(typeof(AzuraAdminStationConfigRecord))]
[JsonSerializable(typeof(AzuraFileUploadRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraFilesRecord>))]
[JsonSerializable(typeof(AzuraInternalRequestRecord))]
[JsonSerializable(typeof(SerializableExceptionsRecord))]
// Deserialization
[JsonSerializable(typeof(AzuraAdminStationConfigRecord))] // Generic
[JsonSerializable(typeof(AzuraErrorRecord))] // Generic
[JsonSerializable(typeof(AzuraFilesRecord))]
[JsonSerializable(typeof(AzuraFilesDetailedRecord))] // Generic
[JsonSerializable(typeof(IEnumerable<AzuraFilesDetailedRecord>))] // Generic
[JsonSerializable(typeof(AzuraHardwareStatsRecord))] // Generic
[JsonSerializable(typeof(AzuraHlsMountRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraHlsMountRecord>))] // Generic
[JsonSerializable(typeof(AzuraNowPlayingDataRecord))] // Generic
[JsonSerializable(typeof(AzuraPlaylistRecord))] // Generic
[JsonSerializable(typeof(IEnumerable<AzuraPlaylistRecord>))] // Generic
[JsonSerializable(typeof(AzuraRequestRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraRequestRecord>))] // Generic
[JsonSerializable(typeof(AzuraRequestQueueItemRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraRequestQueueItemRecord>))] // Generic
[JsonSerializable(typeof(AzuraMediaItemRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraMediaItemRecord>))] // Generic
[JsonSerializable(typeof(AzuraSongDataRecord))]
[JsonSerializable(typeof(AzuraStationHistoryItemRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraStationHistoryItemRecord>))] // Generic
[JsonSerializable(typeof(AzuraStationListenerRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraStationListenerRecord>))] // Generic
[JsonSerializable(typeof(AzuraStationQueueItemDetailedRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraStationQueueItemDetailedRecord>))] // Generic
[JsonSerializable(typeof(AzuraStationRecord))] // Generic
[JsonSerializable(typeof(AzuraStationStatusRecord))] // Generic
[JsonSerializable(typeof(AzuraStatusRecord))] // Generic
[JsonSerializable(typeof(AzuraSystemLogsRecord))] // Generic
[JsonSerializable(typeof(AzuraSystemLogRecord))] // Generic
[JsonSerializable(typeof(AzuraUpdateRecord))]
[JsonSerializable(typeof(AzuraUpdateErrorRecord))]
[JsonSerializable(typeof(List<AzzyUpdateRecord>))]
public sealed partial class JsonSourceGen : JsonSerializerContext
{
}
