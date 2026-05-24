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
[JsonSerializable(typeof(AzuraAdminStationConfigRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraAdminStationConfigRecord>))]
[JsonSerializable(typeof(AzuraErrorRecord))]
[JsonSerializable(typeof(AzuraFilesRecord))]
[JsonSerializable(typeof(AzuraFilesDetailedRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraFilesDetailedRecord>))]
[JsonSerializable(typeof(AzuraHardwareStatsRecord))]
[JsonSerializable(typeof(AzuraHlsMountRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraHlsMountRecord>))]
[JsonSerializable(typeof(AzuraNowPlayingDataRecord))]
[JsonSerializable(typeof(AzuraPlaylistRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraPlaylistRecord>))]
[JsonSerializable(typeof(AzuraRequestRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraRequestRecord>))]
[JsonSerializable(typeof(AzuraRequestQueueItemRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraRequestQueueItemRecord>))]
[JsonSerializable(typeof(AzuraMediaItemRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraMediaItemRecord>))]
[JsonSerializable(typeof(AzuraSongDataRecord))]
[JsonSerializable(typeof(AzuraStationHistoryItemRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraStationHistoryItemRecord>))]
[JsonSerializable(typeof(AzuraStationListenerRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraStationListenerRecord>))]
[JsonSerializable(typeof(AzuraStationQueueItemDetailedRecord))]
[JsonSerializable(typeof(IEnumerable<AzuraStationQueueItemDetailedRecord>))]
[JsonSerializable(typeof(AzuraStationRecord))]
[JsonSerializable(typeof(AzuraStationStatusRecord))]
[JsonSerializable(typeof(AzuraStatusRecord))]
[JsonSerializable(typeof(AzuraSystemLogsRecord))]
[JsonSerializable(typeof(AzuraSystemLogRecord))]
[JsonSerializable(typeof(AzuraUpdateRecord))]
[JsonSerializable(typeof(AzuraUpdateErrorRecord))]
[JsonSerializable(typeof(List<AzzyUpdateRecord>))]
public sealed partial class JsonSourceGen : JsonSerializerContext
{
}
