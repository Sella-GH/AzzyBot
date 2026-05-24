using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AzzyBot.Bot.Models;
#if DEBUG || DOCKER_DEBUG
using AzzyBot.Bot.Structs;
#endif

namespace AzzyBot.Bot.Services.Interfaces;

public interface IWebRequestService
{
    Task<IReadOnlyList<bool>> CheckForApiPermissionsAsync(IReadOnlyList<Uri> urls, IReadOnlyDictionary<string, string> headers);
    Task DeleteAsync(Uri uri, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true);
    Task<string> DownloadAsync(Uri url, string downloadPath, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool acceptImage = false, bool noCache = true);
    Task<AzzyIpAddressModel> GetIpAddressesAsync();
    Task<long> GetPingAsync(Uri uri);
    Task<string?> GetWebAsync(Uri url, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true, bool noLogging = false);
#if DEBUG || DOCKER_DEBUG
    Task<AzzyDebugWebRequestStruct> DebugGetWebAsync(Uri url);
#endif
    Task PostWebAsync(Uri url, string? content = null, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true);
    Task PutWebAsync(Uri url, string? content = null, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true);
    Task<string?> UploadAsync(Uri url, string file, string fileName, string filePath, IReadOnlyDictionary<string, string>? headers = null, bool acceptJson = false, bool noCache = true);
}
