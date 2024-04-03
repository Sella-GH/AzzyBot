using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AzzyBot.ExceptionHandling;
using AzzyBot.Strings.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzzyBot.Modules.Core.Updater;

internal static class Updates
{
    internal static async Task CheckForUpdatesAsync()
    {
        const string gitHubUrl = "https://api.github.com/repos/Sella-GH/AzzyBot/releases/latest";
        Version localVersion = new(CoreAzzyStatsGeneral.GetBotVersion);

        Dictionary<string, string> headers = new()
        {
            ["User-Agent"] = CoreAzzyStatsGeneral.GetBotName
        };
        string body = await CoreWebRequests.GetWebAsync(gitHubUrl, headers);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("GitHub release version body is empty");

        UpdaterModel? updaterModel = JsonConvert.DeserializeObject<UpdaterModel>(body) ?? throw new InvalidOperationException("UpdaterModel is null");

        Version updateVersion = new(updaterModel.name);
        if (updateVersion == localVersion)
        {
            await AzzyBot.SendMessageAsync(CoreSettings.ErrorChannelId, "No update neccessary");
            return;
        }

        if (!DateTime.TryParse(updaterModel.createdAt, out DateTime releaseDate))
            releaseDate = DateTime.Now;

        await AzzyBot.SendMessageAsync(CoreSettings.ErrorChannelId, string.Empty, CoreEmbedBuilder.BuildUpdatesAvailableEmbed(updateVersion, releaseDate));
        await AzzyBot.SendMessageAsync(CoreSettings.ErrorChannelId, string.Empty, CoreEmbedBuilder.BuildUpdatesAvailableChangelogEmbed(updaterModel.body));
    }

    private static readonly HttpClient Client = new()
    {
        DefaultRequestVersion = new(2, 0),
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static void AddHeaders(Dictionary<string, string> headers, HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(headers, nameof(headers));

        client.DefaultRequestHeaders.Clear();

        foreach (KeyValuePair<string, string> header in headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }

    internal static async Task CheckForUpdaterUpdatesAsync()
    {
        string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater");
        string filePath = Path.Combine(basePath, "Updater.dll");
        string settingsPath = Path.Combine(basePath, "appsettings.json");
        string text;

        if (!File.Exists(filePath))
            throw new InvalidOperationException("Updater is not available!");

        Version localVersion = new(FileVersionInfo.GetVersionInfo(filePath).FileVersion ?? string.Empty);

        if (!File.Exists(settingsPath))
            throw new IOException($"File not found {settingsPath}");

        try
        {
            text = await File.ReadAllTextAsync(settingsPath);
        }
        catch (DirectoryNotFoundException)
        {
            ExceptionHandler.LogMessage(LogLevel.Warning, $"Directory not found: {basePath}");
            throw;
        }
        catch (FileNotFoundException)
        {
            ExceptionHandler.LogMessage(LogLevel.Warning, $"File not found: {settingsPath}");
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            ExceptionHandler.LogMessage(LogLevel.Warning, $"Can't access file: {settingsPath} - invalid permissions");
            throw;
        }

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Settings can not be read");

        UpdaterModel? model = JsonConvert.DeserializeObject<UpdaterModel>(text);

        if (model is null)
            throw new InvalidOperationException($"{nameof(model)} is null");

        Dictionary<string, string> headers = new()
        {
            ["X-GitHub-Api-Version"] = "2022-11-28",
            ["Authorization"] = $"Bearer model.Updater.PersonalAccessToken",
            ["User-Agent"] = "AzzyBot-Dev"
        };

        DeleteOldFiles();

        string url = string.Join("/", $"model.Updater.ApiUrl-Updater", "tags");
        AddHeaders(headers, Client);
        HttpResponseMessage? reponse = await Client.GetAsync(new Uri(url));
        reponse.EnsureSuccessStatusCode();

        string content = await reponse.Content.ReadAsStringAsync();
        reponse.Dispose();

        using JsonDocument doc = JsonDocument.Parse(content);

        // Check if commit is already the newest
        Version onlineVersion = new(doc.RootElement[0].GetProperty("name").GetString() ?? "0.0.0");

        if (onlineVersion.ToString() == "0.0.0")
            throw new InvalidOperationException("Version can not be found");

        if (onlineVersion > localVersion)
            await InstallUpdaterUpdateAsync(onlineVersion, "model.Updater.ApiUrl", "model.Updater.PersonalAccessToken", "model.Updater.Permissions");
    }

    private static async Task InstallUpdaterUpdateAsync(Version version, string apiUrl, string pat, string permissions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiUrl, nameof(apiUrl));
        ArgumentException.ThrowIfNullOrWhiteSpace(pat, nameof(pat));
        ArgumentException.ThrowIfNullOrWhiteSpace(permissions, nameof(permissions));

        Dictionary<string, string> headers = new()
        {
            ["X-GitHub-Api-Version"] = "2022-11-28",
            ["Authorization"] = $"Bearer {pat}",
            ["User-Agent"] = "AzzyBot-Dev"
        };

        string url = string.Join("/", $"{apiUrl}-Updater", "releases", "tags", version);
        AddHeaders(headers, Client);
        HttpResponseMessage? response = await Client.GetAsync(new Uri(url));
        response.EnsureSuccessStatusCode();

        string content = await response.Content.ReadAsStringAsync();
        response.Dispose();

        using JsonDocument doc = JsonDocument.Parse(content);

        string downloadUrl = doc.RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(downloadUrl))
            throw new InvalidOperationException("Download URL is null");

        //DeleteOldFiles();

        Console.WriteLine(downloadUrl);

        // Build a new HttpClient to retrieve the file
        using HttpClient httpClient = new()
        {
            DefaultRequestVersion = new(1, 1),
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };

        // Add URL headers again
        AddHeaders(headers, httpClient);

        response = await httpClient.GetAsync(new Uri(downloadUrl));
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException)
        {
            return;
        }

        string tempZipPath = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater", "zip.zip"));

        await using (Stream stream = await response.Content.ReadAsStreamAsync())
        {
            await using FileStream fs = new(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fs);
            ExceptionHandler.LogMessage(LogLevel.Debug, "Updater files downloaded");
        }

        response.Dispose();

        using ZipArchive archive = ZipFile.OpenRead(tempZipPath);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string fullPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater", entry.Name));

            if (!fullPath.StartsWith(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater"), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Extracting Path isn't correct");

            // If it's a directory, create it
            if (string.IsNullOrEmpty(entry.Name))
            {
                if (!Directory.Exists(fullPath))
                    Directory.CreateDirectory(fullPath);
            }
            else
            {
                string? directoryPath = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath!);

                if (File.Exists(fullPath) || fullPath.Contains("appsettings.json", StringComparison.OrdinalIgnoreCase))
                    continue;

                entry.ExtractToFile(fullPath);
            }
        }

        File.Delete(tempZipPath);

        ExceptionHandler.LogMessage(LogLevel.Information, "New Updater extracted");
    }

    private static void DeleteOldFiles()
    {
        string[] existingFiles = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater"));
        if (existingFiles.Length > 1)
        {
            foreach (string file in existingFiles)
            {
                string fileName = Path.GetFileName(file);
                if (fileName is not "appsettings.json")
                    File.Delete(file);
            }
        }

        ExceptionHandler.LogMessage(LogLevel.Debug, "Old Updater files deleted");
    }
}
