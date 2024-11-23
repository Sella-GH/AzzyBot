using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace AzzyBot.Core.Utilities;

public static class FileOperations
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static async Task<string> CreateCsvFileAsync<T>(IEnumerable<T> content, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(content);

        string filePath = (!string.IsNullOrEmpty(path)) ? Path.Combine(Path.GetTempPath(), path) : Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
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
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        string tempFilePath = (!string.IsNullOrEmpty(fileName)) ? Path.Combine(Path.GetTempPath(), fileName) : Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        await File.WriteAllTextAsync(tempFilePath, content);

        return tempFilePath;
    }

    public static async Task CreateZipFileAsync(string zipFileName, string zipFileDir, string filesDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(zipFileDir);
        ArgumentException.ThrowIfNullOrWhiteSpace(filesDir);

        string zipPath = Path.Combine(zipFileDir, zipFileName);
        await using FileStream stream = new(zipPath, FileMode.Create);
        using ZipArchive zipFile = new(stream, ZipArchiveMode.Create, false, Encoding.UTF8);
        foreach (string file in Directory.EnumerateFiles(filesDir))
        {
            string fileName = Path.GetFileName(file);
            zipFile.CreateEntryFromFile(file, fileName, CompressionLevel.SmallestSize);
        }

        if (!File.Exists(zipPath))
            throw new FileNotFoundException($"The zip file {zipPath} was not created.");
    }

    public static void DeleteFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        File.Delete(path);
    }

    public static void DeleteFiles(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        foreach (string path in paths)
        {
            File.Delete(path);
        }
    }

    public static void DeleteFiles(string path, string startingName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(startingName);

        foreach (string file in Directory.EnumerateFiles(path, $"{startingName}*"))
        {
            File.Delete(file);
        }
    }

    public static Task<byte[]> GetBase64BytesFromFileAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return File.ReadAllBytesAsync(path);
    }

    public static Task<string> GetFileContentAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return File.ReadAllTextAsync(path);
    }

    public static IEnumerable<string> GetFilesInDirectory(string path, bool latest = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return (!latest) ? Directory.EnumerateFiles(path) : Directory.EnumerateFiles(path).OrderDescending();
    }

    public static async Task WriteToFileAsync(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        await File.WriteAllTextAsync(path, content);
    }

    public static async Task WriteToFilesAsync(string directoryPath, IReadOnlyDictionary<string, string> files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        ArgumentNullException.ThrowIfNull(files);

        foreach (KeyValuePair<string, string> file in files)
        {
            string filePath = Path.Combine(directoryPath, file.Key);
            await File.WriteAllTextAsync(filePath, file.Value);
        }
    }

    public static async Task WriteToJsonFileAsync<T>(string path, T content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);

        string json = JsonSerializer.Serialize(content, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }
}
