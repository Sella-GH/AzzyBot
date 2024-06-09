using System;
using System.Collections.Generic;
using System.IO;
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
}
