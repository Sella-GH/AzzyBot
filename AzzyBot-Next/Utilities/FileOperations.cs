using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace AzzyBot.Utilities;

public static class FileOperations
{
    public static async Task<string> CreateTempFileAsync(string content, string? fileName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

        string tempFilePath = (!string.IsNullOrWhiteSpace(fileName)) ? Path.Combine(Path.GetTempPath(), fileName) : Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, content);

        return tempFilePath;
    }

    public static void CreateZipFile(string zipFileName, string zipFileDir, string filesDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipFileName, nameof(zipFileName));
        ArgumentException.ThrowIfNullOrWhiteSpace(zipFileDir, nameof(zipFileDir));
        ArgumentException.ThrowIfNullOrWhiteSpace(filesDir, nameof(filesDir));

        string dir = Path.Combine(zipFileDir, zipFileName);
        ZipFile.CreateFromDirectory(filesDir, dir, CompressionLevel.NoCompression, false, Encoding.UTF8);
        if (!File.Exists(dir))
            throw new FileNotFoundException($"The zip file {dir} was not created.");
    }

    public static void DeleteFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        File.Delete(path);
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
}
