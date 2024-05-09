using System;
using System.IO;
using System.Threading.Tasks;

namespace AzzyBot.Utilities;

internal static class FileOperations
{
    internal static async Task<string> CreateTempFileAsync(string content, string? fileName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

        string tempFilePath;

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            tempFilePath = Path.Combine(Path.GetTempPath(), fileName);
        }
        else
        {
            tempFilePath = Path.GetTempFileName();
        }

        await File.WriteAllTextAsync(tempFilePath, content);

        return tempFilePath;
    }

    internal static void DeleteTempFilePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        File.Delete(path);
    }
}
