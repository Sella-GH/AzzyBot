using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

using AzzyBot.Bot.Models;
using AzzyBot.Bot.Models.AzuraCast;
using AzzyBot.Bot.Settings;
using AzzyBot.Core.Models;

namespace AzzyBot.Bot.Utilities;

[SuppressMessage("Roslynator", "RCS1251:Remove unnecessary braces from record declaration", Justification = "Class has to be empty.")]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default, WriteIndented = true)]
// Serialization
[JsonSerializable(typeof(AppSettingsModel))]
[JsonSerializable(typeof(AzuraAdminStationConfigModel))]
[JsonSerializable(typeof(AzuraFileUploadModel))]
[JsonSerializable(typeof(IEnumerable<AzuraFilesModel>))]
[JsonSerializable(typeof(AzuraInternalRequestModel))]
[JsonSerializable(typeof(SerializableExceptionsModel))]
// Deserialization
[JsonSerializable(typeof(AzuraAdminStationConfigModel))]
[JsonSerializable(typeof(IEnumerable<AzuraAdminStationConfigModel>))]
[JsonSerializable(typeof(AzuraErrorModel))]
[JsonSerializable(typeof(AzuraFilesModel))]
[JsonSerializable(typeof(AzuraFilesDetailedModel))]
[JsonSerializable(typeof(IEnumerable<AzuraFilesDetailedModel>))]
[JsonSerializable(typeof(AzuraHardwareStatsModel))]
[JsonSerializable(typeof(AzuraHlsMountModel))]
[JsonSerializable(typeof(IEnumerable<AzuraHlsMountModel>))]
[JsonSerializable(typeof(AzuraNowPlayingDataModel))]
[JsonSerializable(typeof(AzuraPlaylistModel))]
[JsonSerializable(typeof(IEnumerable<AzuraPlaylistModel>))]
[JsonSerializable(typeof(AzuraRequestModel))]
[JsonSerializable(typeof(IEnumerable<AzuraRequestModel>))]
[JsonSerializable(typeof(AzuraRequestQueueItemModel))]
[JsonSerializable(typeof(IEnumerable<AzuraRequestQueueItemModel>))]
[JsonSerializable(typeof(IEnumerable<AzuraMediaItemModel>))]
[JsonSerializable(typeof(AzuraSongDataModel))]
[JsonSerializable(typeof(AzuraStationHistoryItemModel))]
[JsonSerializable(typeof(IEnumerable<AzuraStationHistoryItemModel>))]
[JsonSerializable(typeof(AzuraStationListenerModel))]
[JsonSerializable(typeof(IEnumerable<AzuraStationListenerModel>))]
[JsonSerializable(typeof(AzuraStationQueueItemDetailedModel))]
[JsonSerializable(typeof(IEnumerable<AzuraStationQueueItemDetailedModel>))]
[JsonSerializable(typeof(AzuraStationModel))]
[JsonSerializable(typeof(AzuraStationStatusModel))]
[JsonSerializable(typeof(AzuraStatusModel))]
[JsonSerializable(typeof(AzuraSystemLogsModel))]
[JsonSerializable(typeof(AzuraSystemLogModel))]
[JsonSerializable(typeof(IEnumerable<AzuraSystemLogEntryModel>))]
[JsonSerializable(typeof(AzuraUpdateModel))]
[JsonSerializable(typeof(AzuraUpdateErrorModel))]
[JsonSerializable(typeof(List<AzzyUpdateModel>))]
public sealed partial class JsonSourceGen : JsonSerializerContext;
