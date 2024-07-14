using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace AzzyBot.Utilities;

public static class FileOperations
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static async Task<string> CreateCsvFileAsync<T>(IReadOnlyList<T> content, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(content, nameof(content));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(content.Count, nameof(content));

        string filePath = (!string.IsNullOrWhiteSpace(path)) ? Path.Combine(Path.GetTempPath(), path) : Path.GetTempFileName();

        await using StreamWriter writer = new(filePath);
        CsvConfiguration config = new(CultureInfo.InvariantCulture)
        {
            Encoding = Encoding.UTF8,
            InjectionOptions = InjectionOptions.Escape
        };
        await using CsvWriter csv = new(writer, config);
        await csv.WriteRecordsAsync(content);

        return filePath;
    }

    public static async Task<string> CreateTempFileAsync(string content, string? fileName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

        string tempFilePath = (!string.IsNullOrWhiteSpace(fileName)) ? Path.Combine(Path.GetTempPath(), fileName) : Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, content);

        return tempFilePath;
    }

    public static async Task CreateZipFileAsync(string zipFileName, string zipFileDir, string filesDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipFileName, nameof(zipFileName));
        ArgumentException.ThrowIfNullOrWhiteSpace(zipFileDir, nameof(zipFileDir));
        ArgumentException.ThrowIfNullOrWhiteSpace(filesDir, nameof(filesDir));

        string zipPath = Path.Combine(zipFileDir, zipFileName);
        await using FileStream stream = new(zipPath, FileMode.Create);
        using ZipArchive zipFile = new(stream, ZipArchiveMode.Create, false, Encoding.UTF8);
        foreach (string file in Directory.GetFiles(filesDir))
        {
            string fileName = Path.GetFileName(file);
            zipFile.CreateEntryFromFile(file, fileName, CompressionLevel.SmallestSize);
        }

        if (!File.Exists(zipPath))
            throw new FileNotFoundException($"The zip file {zipPath} was not created.");
    }

    public static void DeleteFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        File.Delete(path);
    }

    public static void DeleteFiles(IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths, nameof(paths));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(paths.Count, nameof(paths));

        foreach (string path in paths)
        {
            File.Delete(path);
        }
    }

    public static void DeleteFiles(string path, string startingName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));
        ArgumentException.ThrowIfNullOrWhiteSpace(startingName, nameof(startingName));

        foreach (string file in Directory.GetFiles(path, $"{startingName}*"))
        {
            File.Delete(file);
        }
    }

    public static Task<string> GetFileContentAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        return File.ReadAllTextAsync(path);
    }

    public static IReadOnlyList<string> GetFilesInDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        return Directory.GetFiles(path);
    }

    public static async Task WriteToFileAsync(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

        await File.WriteAllTextAsync(path, content);
    }

    public static async Task WriteToFilesAsync(string directoryPath, IReadOnlyDictionary<string, string> files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath, nameof(directoryPath));
        ArgumentNullException.ThrowIfNull(files, nameof(files));

        foreach (KeyValuePair<string, string> file in files)
        {
            string filePath = Path.Combine(directoryPath, file.Key);
            await File.WriteAllTextAsync(filePath, file.Value);
        }
    }

    public static async Task WriteToJsonFileAsync<T>(string path, T content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));
        ArgumentNullException.ThrowIfNull(content, nameof(content));

        string json = JsonSerializer.Serialize(content, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }
}
